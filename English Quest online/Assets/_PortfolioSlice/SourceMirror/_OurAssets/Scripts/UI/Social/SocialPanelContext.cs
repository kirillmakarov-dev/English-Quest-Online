public sealed class SocialPanelContext
{
    public ISessionPlayerRegistry SessionPlayers { get; }
    public IPartyService Party { get; }
    public IPartyInviteService Invites { get; }

    public SocialPanelContext(
        ISessionPlayerRegistry sessionPlayers,
        IPartyService party,
        IPartyInviteService invites)
    {
        SessionPlayers = sessionPlayers;
        Party = party;
        Invites = invites;
    }
}
