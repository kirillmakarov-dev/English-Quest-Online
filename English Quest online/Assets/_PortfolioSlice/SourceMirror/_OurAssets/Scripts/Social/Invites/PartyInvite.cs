using Fusion;

public readonly struct PartyInvite
{
    public PlayerRef From { get; }
    public string FromDisplayName { get; }

    public PartyInvite(PlayerRef from, string fromDisplayName)
    {
        From = from;
        FromDisplayName = fromDisplayName;
    }
}
