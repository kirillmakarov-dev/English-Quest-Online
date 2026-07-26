using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Helpers for Fusion Multi-Peer editor sessions where multiple local
/// <see cref="NetworkRunner"/> instances share one room name.
/// </summary>
public static class FusionCoSessionRunners
{
    public static bool AreInSameSession(NetworkRunner a, NetworkRunner b)
    {
        if (a == null || b == null || !a.IsRunning || !b.IsRunning)
            return false;

        if (a == b)
            return true;

        if (NetworkProjectConfig.Global.PeerMode != NetworkProjectConfig.PeerModes.Multiple)
            return false;

        if (a.SessionInfo.IsValid && b.SessionInfo.IsValid)
            return a.SessionInfo.Name == b.SessionInfo.Name;

#if UNITY_EDITOR
        // Editor multi-peer play mode tests often run before SessionInfo is populated.
        return true;
#else
        return false;
#endif
    }

    public static IEnumerable<NetworkRunner> Enumerate(NetworkRunner contextRunner)
    {
        if (contextRunner == null || !contextRunner.IsRunning)
            yield break;

        yield return contextRunner;

        if (NetworkProjectConfig.Global.PeerMode != NetworkProjectConfig.PeerModes.Multiple)
            yield break;

        bool hasSessionName = contextRunner.SessionInfo.IsValid
                              && !string.IsNullOrEmpty(contextRunner.SessionInfo.Name);

        foreach (NetworkRunner runner in NetworkRunner.Instances)
        {
            if (runner == null || !runner.IsRunning || runner == contextRunner)
                continue;

            if (hasSessionName)
            {
                if (runner.SessionInfo.IsValid && runner.SessionInfo.Name == contextRunner.SessionInfo.Name)
                    yield return runner;

                continue;
            }

#if UNITY_EDITOR
            yield return runner;
#endif
        }
    }

    public static IEnumerable<PlayerRef> EnumeratePlayers(NetworkRunner contextRunner)
    {
        var seenNetworkIds = new HashSet<NetworkId>();
        var seenPlayerRefs = new HashSet<PlayerRef>();

        foreach (NetworkRunner runner in Enumerate(contextRunner))
        {
            foreach (PlayerRef player in runner.ActivePlayers)
            {
                if (TryAcceptPlayer(runner, player, seenNetworkIds, seenPlayerRefs))
                    yield return player;
            }

            PlayerRef localPlayer = runner.LocalPlayer;
            if (TryAcceptPlayer(runner, localPlayer, seenNetworkIds, seenPlayerRefs))
                yield return localPlayer;
        }
    }

    private static bool TryAcceptPlayer(
        NetworkRunner runner,
        PlayerRef player,
        HashSet<NetworkId> seenNetworkIds,
        HashSet<PlayerRef> seenPlayerRefs)
    {
        if (!player.IsRealPlayer)
            return false;

        NetworkObject playerObject = runner.GetPlayerObject(player);
        if (playerObject != null)
            return seenNetworkIds.Add(playerObject.Id);

        return seenPlayerRefs.Add(player);
    }
}
