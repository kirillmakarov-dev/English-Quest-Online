using Fusion;

public class PartyTravelAuthority : IPartyTravelAuthority
{
    public bool CanInitiateTravel(PlayerInteraction interactor)
    {
        if (interactor == null)
            return false;

        if (interactor.Object == null)
            return true;

        NetworkRunner runner = interactor.Object.Runner;
        if (runner == null || !runner.IsRunning)
            return false;

        PlayerPartyMembership membership =
            PartyMembershipUtility.GetMembership(runner, interactor.Object.InputAuthority);

        return membership != null && membership.IsPartyLeader;
    }
}
