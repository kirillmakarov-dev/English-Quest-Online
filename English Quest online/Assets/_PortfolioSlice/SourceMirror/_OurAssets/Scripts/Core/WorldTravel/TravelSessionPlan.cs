public readonly struct TravelSessionPlan
{
    public string SessionName { get; }
    public WorldTravelSessionMode Mode { get; }
    public int MaxPlayers { get; }
    public bool CreatesSession { get; }

    public TravelSessionPlan(
        string sessionName,
        WorldTravelSessionMode mode,
        int maxPlayers,
        bool createsSession)
    {
        SessionName = sessionName;
        Mode = mode;
        MaxPlayers = maxPlayers;
        CreatesSession = createsSession;
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(SessionName);
}
