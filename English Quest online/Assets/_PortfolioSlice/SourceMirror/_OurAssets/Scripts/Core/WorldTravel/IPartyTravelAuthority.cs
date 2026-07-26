using Fusion;

/// <summary>
/// Determines who may initiate world-map travel.
/// MVP: local player with state authority. Party phase will gate on party leader.
/// </summary>
public interface IPartyTravelAuthority
{
    bool CanInitiateTravel(PlayerInteraction interactor);
}
