using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

internal readonly struct TravelExecutionRequest
{
    public readonly WorldMapDefinitionSO Map;
    public readonly string FromNodeId;
    public readonly string ToNodeId;
    public readonly WorldMapNodeData FromNode;
    public readonly WorldMapNodeData ToNode;
    public readonly WorldMapRouteData Route;
    public readonly PlayerInteraction Interactor;
    public readonly bool IsPartyFollow;
    public readonly bool BroadcastPartyTravel;
    public readonly int PresentationEpoch;
    public readonly TravelSessionPlan SessionPlan;

    public TravelExecutionRequest(
        WorldMapDefinitionSO map,
        string fromNodeId,
        string toNodeId,
        in WorldMapNodeData fromNode,
        in WorldMapNodeData toNode,
        in WorldMapRouteData route,
        PlayerInteraction interactor,
        bool isPartyFollow,
        bool broadcastPartyTravel,
        int presentationEpoch = 0,
        in TravelSessionPlan sessionPlan = default)
    {
        Map = map;
        FromNodeId = fromNodeId;
        ToNodeId = toNodeId;
        FromNode = fromNode;
        ToNode = toNode;
        Route = route;
        Interactor = interactor;
        IsPartyFollow = isPartyFollow;
        BroadcastPartyTravel = broadcastPartyTravel;
        PresentationEpoch = presentationEpoch;
        SessionPlan = sessionPlan;
    }
}

/// <summary>
/// Runs the full world-travel sequence: presentation, session switch, spawn pipeline, arrival.
/// </summary>
internal sealed class WorldTravelPipeline
{
    private readonly MonoBehaviour _serviceContext;
    private readonly System.Func<WorldMapUI> _resolveMapUi;
    private readonly INetworkTravelCoordinator _networkCoordinator;

    public WorldTravelPipeline(
        MonoBehaviour serviceContext,
        System.Func<WorldMapUI> resolveMapUi,
        INetworkTravelCoordinator networkCoordinator)
    {
        _serviceContext = serviceContext;
        _resolveMapUi = resolveMapUi;
        _networkCoordinator = networkCoordinator;
    }

    public async UniTask ExecuteAsync(TravelExecutionRequest request)
    {
        WorldMapUI mapUi = _resolveMapUi();
        if (mapUi == null)
        {
            AppLog.Error("[WorldTravelService] WorldMapUI not found.");
            return;
        }

        NetworkRunner runner = TravelInteractorResolver.GetRunner(request.Interactor);
        bool pendingArrivalSet = false;
        int presentationEpoch = request.PresentationEpoch;
        TravelSessionPlan sessionPlan = request.SessionPlan;

        try
        {
            var presentationRequest = new WorldTravelPresentationRunner.PresentationRequest(
                request.Map,
                request.FromNodeId,
                request.ToNodeId,
                request.FromNode,
                request.ToNode,
                request.Route,
                request.Interactor,
                request.IsPartyFollow,
                request.BroadcastPartyTravel,
                request.PresentationEpoch,
                sessionPlan);

            WorldTravelPresentationRunner.PresentationResult presentationResult =
                await WorldTravelPresentationRunner.RunPresentationPhaseAsync(
                    mapUi,
                    presentationRequest,
                    _networkCoordinator);

            if (presentationResult.PresentationEpoch > 0)
                presentationEpoch = presentationResult.PresentationEpoch;

            if (presentationResult.SessionPlan.IsValid)
                sessionPlan = presentationResult.SessionPlan;

            bool needsSceneLoad = request.ToNode.destinationType == WorldMapDestinationType.LoadScene;

            if (needsSceneLoad)
            {
                SetPendingArrivalIfNeeded(request.ToNode, runner);
                pendingArrivalSet = true;
            }

            mapUi.SetCurrentNode(request.ToNodeId);

            if (needsSceneLoad)
            {
                mapUi.PrepareForSceneTransition();
                request.Interactor?.ReleaseAllInteractableBindings();

                bool switched = await WorldTravelSceneLoader.SwitchToDestinationSessionAsync(
                    sessionPlan,
                    request.ToNode,
                    request.Interactor,
                    _serviceContext);

                if (!switched)
                {
                    AppLog.Error("[WorldTravelService] Travel aborted because session switch failed.");
                    return;
                }

                await TravelArrivalExecutor.TeleportToDestinationAsync(
                    request.Interactor,
                    _serviceContext,
                    request.ToNode);

                pendingArrivalSet = false;
            }
            else
            {
                WorldTravelPresentationRunner.ReleaseInteraction(mapUi, request.Interactor, request.IsPartyFollow);

                await TravelArrivalExecutor.TeleportToDestinationAsync(
                    request.Interactor,
                    _serviceContext,
                    request.ToNode);
            }

        }
        finally
        {
            if (pendingArrivalSet)
                PlayerTravelArrival.Clear(runner);

            if (presentationEpoch > 0)
                PartyTravelPresentationGate.Reset(presentationEpoch);

            if (mapUi != null)
                WorldTravelPresentationRunner.CloseAfterTravel(mapUi, request.Interactor, request.IsPartyFollow);
        }
    }

    private static void SetPendingArrivalIfNeeded(in WorldMapNodeData toNode, NetworkRunner runner)
    {
        if (toNode.destinationType != WorldMapDestinationType.LoadScene)
            return;

        string spawnId = string.IsNullOrEmpty(toNode.spawnPointId) ? toNode.id : toNode.spawnPointId;
        PlayerTravelArrival.SetPending(runner, spawnId, toNode.scene.BuildIndex, toNode);
    }
}
