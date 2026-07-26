using Fusion;
using UnityEngine;

/// <summary>
/// Resolves whether the local player should receive kill credit (stats, loot, etc.).
/// </summary>
public static class CombatKillCredit
{
    public static bool IsLocalPlayerKill(DeathContext context, NetworkRunner runner)
    {
        if (!context.HasKiller)
            return false;

        if (!NPCTargetScanner.IsPlayerTarget(context.Killer.transform))
            return false;

        if (runner == null || !runner.IsRunning)
            return true;

        var killerNetworkObject = context.Killer.GetComponentInParent<NetworkObject>();
        if (killerNetworkObject == null || !killerNetworkObject.IsValid)
            return true;

        return killerNetworkObject.InputAuthority == runner.LocalPlayer;
    }
}
