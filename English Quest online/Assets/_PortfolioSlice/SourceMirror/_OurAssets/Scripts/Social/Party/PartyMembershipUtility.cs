using Fusion;
using UnityEngine;

public static class PartyMembershipUtility
{
    public static PlayerPartyMembership GetMembership(NetworkRunner runner, PlayerRef player)
    {
        if (runner == null || !player.IsRealPlayer)
            return null;

        PlayerPartyMembership authoritative = GetMembershipFromAuthoritativeRunner(runner, player);
        if (authoritative != null)
            return authoritative;

        foreach (NetworkRunner sessionRunner in FusionCoSessionRunners.Enumerate(runner))
        {
            if (sessionRunner == null || !sessionRunner.IsRunning)
                continue;

            NetworkObject playerObject = sessionRunner.GetPlayerObject(player);
            if (playerObject == null)
                continue;

            PlayerPartyMembership membership = playerObject.GetComponent<PlayerPartyMembership>();
            if (membership != null)
                return membership;
        }

        return null;
    }

    public static string GetDisplayName(NetworkRunner runner, PlayerRef player)
    {
        if (runner == null || !player.IsRealPlayer)
            return string.Empty;

        NetworkObject authoritativeObject = GetPlayerObjectFromAuthoritativeRunner(runner, player);
        if (authoritativeObject != null)
            return ResolveDisplayName(authoritativeObject, player);

        foreach (NetworkRunner sessionRunner in FusionCoSessionRunners.Enumerate(runner))
        {
            if (sessionRunner == null || !sessionRunner.IsRunning)
                continue;

            NetworkObject playerObject = sessionRunner.GetPlayerObject(player);
            if (playerObject == null)
                continue;

            return ResolveDisplayName(playerObject, player);
        }

        return player.ToString();
    }

    private static PlayerPartyMembership GetMembershipFromAuthoritativeRunner(
        NetworkRunner runner,
        PlayerRef player)
    {
        NetworkObject playerObject = GetPlayerObjectFromAuthoritativeRunner(runner, player);
        return playerObject != null
            ? playerObject.GetComponent<PlayerPartyMembership>()
            : null;
    }

    private static NetworkObject GetPlayerObjectFromAuthoritativeRunner(
        NetworkRunner runner,
        PlayerRef player)
    {
        foreach (NetworkRunner sessionRunner in FusionCoSessionRunners.Enumerate(runner))
        {
            if (sessionRunner == null || !sessionRunner.IsRunning || sessionRunner.LocalPlayer != player)
                continue;

            NetworkObject playerObject = sessionRunner.GetPlayerObject(player);
            if (playerObject != null)
                return playerObject;
        }

        return null;
    }

    private static string ResolveDisplayName(NetworkObject playerObject, PlayerRef player)
    {
        PlayerNameSync nameSync = playerObject.GetComponent<PlayerNameSync>();
        if (nameSync == null)
            return player.ToString();

        string name = nameSync.PlayerName.ToString();
        return string.IsNullOrEmpty(name) ? player.ToString() : name;
    }
}
