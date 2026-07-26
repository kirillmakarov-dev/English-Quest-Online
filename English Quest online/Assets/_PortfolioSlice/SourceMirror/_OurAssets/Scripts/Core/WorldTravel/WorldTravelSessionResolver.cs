using Fusion;
using UnityEngine;

public static class WorldTravelSessionResolver
{
    public static TravelSessionPlan Resolve(
        in WorldMapNodeData destination,
        PlayerInteraction interactor,
        int presentationEpoch,
        bool createsSession)
    {
        if (destination.id == WorldTravelSessionNaming.OpenWorldNodeId)
        {
            return new TravelSessionPlan(
                WorldTravelSessionNaming.ResolveOpenWorldSessionName(),
                WorldTravelSessionMode.SharedPool,
                WorldTravelSessionNaming.DefaultSharedMaxPlayers,
                createsSession);
        }

        if (destination.sessionMode == WorldTravelSessionMode.SharedPool)
        {
            return new TravelSessionPlan(
                WorldTravelSessionNaming.ResolveSharedPoolName(in destination),
                WorldTravelSessionMode.SharedPool,
                WorldTravelSessionNaming.ResolveSharedMaxPlayers(in destination),
                createsSession);
        }

        PlayerRef leader = ResolvePartyLeader(interactor);
        int partySize = ResolvePartySize(interactor, leader);
        string sessionName = WorldTravelSessionNaming.CreatePartyIsolatedSessionName(
            leader,
            destination.id,
            presentationEpoch);

        return new TravelSessionPlan(
            sessionName,
            WorldTravelSessionMode.PartyIsolated,
            Mathf.Max(1, partySize),
            createsSession);
    }

    private static PlayerRef ResolvePartyLeader(PlayerInteraction interactor)
    {
        NetworkRunner runner = TravelInteractorResolver.GetRunner(interactor);
        if (runner == null || !runner.IsRunning)
            return PlayerRef.None;

        PlayerRef localPlayer = runner.LocalPlayer;
        PlayerPartyMembership membership = PartyMembershipUtility.GetMembership(runner, localPlayer);
        if (membership == null)
            return localPlayer;

        return membership.PartyLeader.IsRealPlayer ? membership.PartyLeader : localPlayer;
    }

    private static int ResolvePartySize(PlayerInteraction interactor, PlayerRef leaderRef)
    {
        if (!leaderRef.IsRealPlayer)
            return 1;

        NetworkRunner runner = TravelInteractorResolver.GetRunner(interactor);
        if (runner == null || !runner.IsRunning)
            return 1;

        int count = 0;
        foreach (PlayerRef player in FusionCoSessionRunners.EnumeratePlayers(runner))
        {
            PlayerPartyMembership membership = PartyMembershipUtility.GetMembership(runner, player);
            if (membership != null && membership.PartyLeader == leaderRef)
                count++;
        }

        return Mathf.Max(1, count);
    }
}
