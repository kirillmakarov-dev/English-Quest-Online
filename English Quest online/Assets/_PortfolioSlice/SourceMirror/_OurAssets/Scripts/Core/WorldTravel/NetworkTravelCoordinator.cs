using Fusion;
using UnityEngine;

public class NetworkTravelCoordinator : MonoBehaviour, INetworkTravelCoordinator
{
    public void NotifyTravelStarted(
        string mapId,
        string fromNodeId,
        string toNodeId,
        PlayerInteraction initiator,
        int presentationEpoch,
        string sessionName,
        WorldTravelSessionMode sessionMode)
    {
        if (sessionMode == WorldTravelSessionMode.PartyIsolated)
            AppLog.Info($"[NetworkTravelCoordinator] Travel started: {mapId} {fromNodeId} -> {toNodeId}");
        else
            AppLog.Info(
                $"[NetworkTravelCoordinator] Travel started: {mapId} {fromNodeId} -> {toNodeId} " +
                $"(session={sessionName}, mode={sessionMode})");

        if (initiator == null || initiator.Object == null)
            return;

        PlayerPartyMembership membership = initiator.GetComponent<PlayerPartyMembership>()
            ?? initiator.GetComponentInParent<PlayerPartyMembership>();

        if (membership == null)
        {
            AppLog.Warning("[NetworkTravelCoordinator] Initiator has no PlayerPartyMembership.");
            return;
        }

        membership.BroadcastPartyTravelStarted(
            mapId,
            fromNodeId,
            toNodeId,
            presentationEpoch,
            sessionName,
            sessionMode);
    }
}
