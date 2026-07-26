using System.Text;
using Fusion;

public static class WorldTravelSessionNaming
{
    public const string OpenWorldSessionName = "OpenWorld";
    public const string OpenWorldDevelopmentSessionName = "OpenWorld_Dev";
    public const string OpenWorldNodeId = "open_world";
    public const int DefaultSharedMaxPlayers = 50;
    private const int MaxSessionNameLength = 48;

    /// <summary>
    /// Shared Open World Fusion room. Non-production UGS environments
    /// (e.g. local <c>development</c>) use a separate room from production players.
    /// </summary>
    public static string ResolveOpenWorldSessionName()
    {
        return IsNonProductionEnvironment()
            ? OpenWorldDevelopmentSessionName
            : OpenWorldSessionName;
    }

    public static bool IsNonProductionEnvironment() => UgsEnvironment.IsNonProduction;

    public static string CreatePartyIsolatedSessionName(PlayerRef leader, string toNodeId, int presentationEpoch)
    {
        string safeNode = SanitizeSessionToken(toNodeId);
        return Truncate($"Travel_{leader.PlayerId}_{safeNode}_{presentationEpoch}");
    }

    public static string ResolveSharedPoolName(in WorldMapNodeData node)
    {
        if (!string.IsNullOrWhiteSpace(node.sharedSessionName))
            return Truncate(SanitizeSessionToken(node.sharedSessionName));

        return Truncate(SanitizeSessionToken(node.id));
    }

    public static int ResolveSharedMaxPlayers(in WorldMapNodeData node) =>
        node.sharedSessionMaxPlayers > 0
            ? node.sharedSessionMaxPlayers
            : DefaultSharedMaxPlayers;

    private static string SanitizeSessionToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "dest";

        var builder = new StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsLetterOrDigit(c) || c == '_')
                builder.Append(c);
            else if (c == '-' || c == ' ')
                builder.Append('_');
        }

        return builder.Length > 0 ? builder.ToString() : "dest";
    }

    private static string Truncate(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= MaxSessionNameLength)
            return value;

        return value.Substring(0, MaxSessionNameLength);
    }
}
