using Cysharp.Threading.Tasks;
using UnityEngine;

internal static class WorldTravelPresentationRunner
{
    private const float MinimumTravelDuration = 2.5f;

    public readonly struct PresentationRequest
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

        public PresentationRequest(
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

    public readonly struct PresentationResult
    {
        public int PresentationEpoch { get; }
        public TravelSessionPlan SessionPlan { get; }

        public PresentationResult(int presentationEpoch, in TravelSessionPlan sessionPlan)
        {
            PresentationEpoch = presentationEpoch;
            SessionPlan = sessionPlan;
        }
    }

    public static async UniTask<PresentationResult> RunPresentationPhaseAsync(
        WorldMapUI mapUi,
        PresentationRequest request,
        INetworkTravelCoordinator networkCoordinator)
    {
        if (mapUi == null)
        {
            AppLog.Error("[WorldTravelService] WorldMapUI not found.");
            return new PresentationResult(request.PresentationEpoch, request.SessionPlan);
        }

        float travelDuration = Mathf.Max(request.Route.DurationOrDefault, MinimumTravelDuration);
        bool sharedPresentationActive = mapUi.IsInTravelMode;
        bool runFullPresentation = !request.IsPartyFollow || !sharedPresentationActive;

        if (runFullPresentation)
            return await RunLeaderPresentationAsync(mapUi, request, networkCoordinator, travelDuration);

        await RunSharedPresentationFollowAsync(mapUi, request);
        return new PresentationResult(request.PresentationEpoch, request.SessionPlan);
    }

    public static void CloseAfterTravel(WorldMapUI mapUi, PlayerInteraction interactor, bool isPartyFollow)
    {
        if (mapUi == null)
            return;

        if (isPartyFollow)
            mapUi.ReleaseFollowTravelLocks(interactor);

        mapUi.CloseAfterTravel();
    }

    public static void ReleaseInteraction(WorldMapUI mapUi, PlayerInteraction interactor, bool isPartyFollow)
    {
        if (isPartyFollow)
            return;

        mapUi?.PrepareForSceneTransition();
        interactor?.ReleaseAllInteractableBindings();
    }

    private static async UniTask<PresentationResult> RunLeaderPresentationAsync(
        WorldMapUI mapUi,
        PresentationRequest request,
        INetworkTravelCoordinator networkCoordinator,
        float travelDuration)
    {
        int presentationEpoch = PartyTravelPresentationGate.BeginPresentation();
        TravelSessionPlan sessionPlan = request.SessionPlan;

        if (!request.IsPartyFollow && request.ToNode.destinationType == WorldMapDestinationType.LoadScene)
        {
            sessionPlan = WorldTravelSessionResolver.Resolve(
                request.ToNode,
                request.Interactor,
                presentationEpoch,
                createsSession: true);
        }

        if (request.IsPartyFollow)
            mapUi.OpenForPartyFollowTravel(request.Map, request.FromNodeId, request.Interactor);
        else
            mapUi.EnterTravelMode(request.FromNode.NormalizedPosition);

        if (request.BroadcastPartyTravel && sessionPlan.IsValid)
        {
            networkCoordinator?.NotifyTravelStarted(
                request.Map.MapId,
                request.FromNodeId,
                request.ToNodeId,
                request.Interactor,
                presentationEpoch,
                sessionPlan.SessionName,
                sessionPlan.Mode);
        }

        UniTask animationTask = mapUi.PlayTravelAnimationAsync(
            request.Route,
            request.FromNode.NormalizedPosition,
            request.ToNode.NormalizedPosition,
            travelDuration);

        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

        await animationTask;
        PartyTravelPresentationGate.SignalPresentationComplete(presentationEpoch);
        return new PresentationResult(presentationEpoch, sessionPlan);
    }

    private static async UniTask RunSharedPresentationFollowAsync(
        WorldMapUI mapUi,
        PresentationRequest request)
    {
        mapUi.OpenForPartyFollowTravel(request.Map, request.FromNodeId, request.Interactor);
        await PartyTravelPresentationGate.WaitForPresentationCompleteAsync(request.PresentationEpoch);
    }
}
