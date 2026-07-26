using Fusion;

public readonly struct PartyMemberInfo
{
    public PlayerRef PlayerRef { get; }
    public string DisplayName { get; }
    public bool IsLeader { get; }
    public bool IsLocal { get; }

    public PartyMemberInfo(PlayerRef playerRef, string displayName, bool isLeader, bool isLocal)
    {
        PlayerRef = playerRef;
        DisplayName = displayName;
        IsLeader = isLeader;
        IsLocal = isLocal;
    }
}
