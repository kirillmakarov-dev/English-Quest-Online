internal static class TravelRouteValidator
{
    public static bool TryResolve(
        WorldMapDefinitionSO map,
        string fromNodeId,
        string toNodeId,
        out WorldMapNodeData fromNode,
        out WorldMapNodeData toNode,
        out WorldMapRouteData route)
    {
        fromNode = default;
        toNode = default;
        route = default;

        if (!map.TryGetNode(fromNodeId, out fromNode) || !map.TryGetNode(toNodeId, out toNode))
        {
            AppLog.Error("[WorldTravelService] Invalid travel nodes.");
            return false;
        }

        if (!map.TryGetRoute(fromNodeId, toNodeId, out route))
        {
            AppLog.Error($"[WorldTravelService] No route from '{fromNodeId}' to '{toNodeId}'.");
            return false;
        }

        if (!toNode.unlockedByDefault)
        {
            AppLog.Warning($"[WorldTravelService] Destination '{toNodeId}' is locked.");
            return false;
        }

        return true;
    }
}
