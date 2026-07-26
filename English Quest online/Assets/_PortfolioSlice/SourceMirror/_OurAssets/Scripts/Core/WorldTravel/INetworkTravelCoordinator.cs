/// <summary>
/// Coordinates networked travel broadcasts for party members.
/// </summary>
public interface INetworkTravelCoordinator
{
    void NotifyTravelStarted(
        string mapId,
        string fromNodeId,
        string toNodeId,
        PlayerInteraction initiator,
        int presentationEpoch,
        string sessionName,
        WorldTravelSessionMode sessionMode);
}
