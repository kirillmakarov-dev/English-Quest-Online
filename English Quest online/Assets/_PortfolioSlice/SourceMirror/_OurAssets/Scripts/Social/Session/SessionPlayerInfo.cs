using Fusion;

public readonly struct SessionPlayerInfo
{
    public PlayerRef PlayerRef { get; }
    public string DisplayName { get; }
    public bool IsLocal { get; }
    public bool IsInMyParty { get; }
    public bool IsPartyLeader { get; }

    public SessionPlayerInfo(
        PlayerRef playerRef,
        string displayName,
        bool isLocal,
        bool isInMyParty,
        bool isPartyLeader)
    {
        PlayerRef = playerRef;
        DisplayName = displayName;
        IsLocal = isLocal;
        IsInMyParty = isInMyParty;
        IsPartyLeader = isPartyLeader;
    }
}
