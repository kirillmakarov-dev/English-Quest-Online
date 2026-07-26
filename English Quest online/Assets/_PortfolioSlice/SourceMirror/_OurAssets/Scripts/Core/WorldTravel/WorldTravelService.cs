using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityServiceLocator;

public class WorldTravelService : MonoBehaviour, IWorldTravelService
{
    [SerializeField] private WorldMapUI _worldMapUI;

    private IPartyTravelAuthority _partyAuthority;
    private INetworkTravelCoordinator _networkCoordinator;
    private readonly TravelStateTracker _travelState = new();
    private WorldTravelPipeline _pipeline;

    public bool IsTraveling => _travelState.IsAnyoneTraveling;

    private WorldMapUI MapUi
    {
        get
        {
            if (_worldMapUI == null)
                _worldMapUI = FindFirstObjectByType<WorldMapUI>(FindObjectsInactive.Include);
            return _worldMapUI;
        }
    }

    private void Awake()
    {
        _partyAuthority = new PartyTravelAuthority();
        _networkCoordinator = GetComponent<NetworkTravelCoordinator>();
        _pipeline = new WorldTravelPipeline(this, () => MapUi, _networkCoordinator);

        ServiceLocator.For(this).Register<IWorldTravelService>(this);
        ServiceLocator.For(this).Register<IPartyTravelAuthority>(_partyAuthority);

        if (_networkCoordinator != null)
            ServiceLocator.For(this).Register<INetworkTravelCoordinator>(_networkCoordinator);
    }

    private void OnDestroy()
    {
        ServiceLocator.DeregisterFor<IWorldTravelService>(this);
        ServiceLocator.DeregisterFor<IPartyTravelAuthority>(this);

        if (_networkCoordinator != null)
            ServiceLocator.DeregisterFor<INetworkTravelCoordinator>(this);
    }

    public bool IsTravelingFor(PlayerInteraction interactor) =>
        _travelState.IsTraveling(interactor);

    public bool CanTravel(PlayerInteraction interactor) =>
        !_travelState.IsTraveling(interactor) && _partyAuthority.CanInitiateTravel(interactor);

    public async UniTask TravelAsync(
        WorldMapDefinitionSO map,
        string fromNodeId,
        string toNodeId,
        PlayerInteraction interactor)
    {
        if (map == null || interactor == null || _travelState.IsTraveling(interactor))
            return;

        if (!_partyAuthority.CanInitiateTravel(interactor))
        {
            AppLog.Warning("[WorldTravelService] Only the party leader may initiate travel.");
            return;
        }

        if (!TravelRouteValidator.TryResolve(map, fromNodeId, toNodeId, out WorldMapNodeData fromNode, out WorldMapNodeData toNode, out WorldMapRouteData route))
            return;

        if (!_travelState.TryBegin(interactor))
            return;

        try
        {
            await _pipeline.ExecuteAsync(new TravelExecutionRequest(
                map,
                fromNodeId,
                toNodeId,
                fromNode,
                toNode,
                route,
                interactor,
                isPartyFollow: false,
                broadcastPartyTravel: true));
        }
        finally
        {
            _travelState.End(interactor);
            PartyTravelPresentationGate.ClearAll();
        }
    }

    public async UniTask FollowPartyTravelAsync(
        string mapId,
        string fromNodeId,
        string toNodeId,
        PlayerInteraction interactor,
        string sessionName,
        WorldTravelSessionMode sessionMode,
        int presentationEpoch = 0)
    {
        if (interactor == null || _travelState.IsTraveling(interactor))
            return;

        WorldMapDefinitionSO map = WorldMapDefinitionLoader.ResolveByMapId(mapId);
        if (map == null)
        {
            AppLog.Error($"[WorldTravelService] Unknown map id '{mapId}'.");
            return;
        }

        if (!TravelRouteValidator.TryResolve(map, fromNodeId, toNodeId, out WorldMapNodeData fromNode, out WorldMapNodeData toNode, out WorldMapRouteData route))
            return;

        if (!_travelState.TryBegin(interactor))
            return;

        var sessionPlan = new TravelSessionPlan(
            sessionName,
            sessionMode,
            WorldTravelSessionNaming.ResolveSharedMaxPlayers(in toNode),
            createsSession: false);

        try
        {
            await _pipeline.ExecuteAsync(new TravelExecutionRequest(
                map,
                fromNodeId,
                toNodeId,
                fromNode,
                toNode,
                route,
                interactor,
                isPartyFollow: true,
                broadcastPartyTravel: false,
                presentationEpoch,
                sessionPlan));
        }
        finally
        {
            _travelState.End(interactor);
        }
    }

    public void HandleRunnerDisconnected(NetworkRunner runner)
    {
        _travelState.ClearRunner(runner);
        PlayerTravelArrival.Clear(runner);
    }
}
