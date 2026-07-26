using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Reclaims orphaned state authority when a player leaves Shared Mode sessions.
/// Dispatch is centralized in <see cref="NetworkRunnerCallbackHub"/>.
/// </summary>
public class NetworkAuthorityService : MonoBehaviour
{
    private static NetworkAuthorityService s_instance;

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(this);
            return;
        }

        s_instance = this;
    }

    private void OnDestroy()
    {
        if (s_instance == this)
            s_instance = null;
    }

    /// <summary>
    /// Transfers state authority for overridable objects owned by a departed player to the master client.
    /// </summary>
    public static void ReclaimOrphanedAuthority(NetworkRunner runner, PlayerRef departedPlayer)
    {
        if (runner == null || !runner.IsRunning || !runner.IsSharedModeMasterClient)
            return;

        IList<NetworkObject> objects = runner.GetAllNetworkObjects();
        int reclaimed = 0;

        for (int i = 0; i < objects.Count; i++)
        {
            NetworkObject obj = objects[i];
            if (obj == null || !obj.IsValid)
                continue;

            if (!CanReclaimAuthority(obj))
                continue;

            if (obj.StateAuthority != departedPlayer)
                continue;

            obj.RequestStateAuthority();
            reclaimed++;
        }

        if (reclaimed > 0)
        {
            AppLog.Info(
                $"[NetworkAuthorityService] Reclaimed state authority for {reclaimed} object(s) " +
                $"after player {departedPlayer} left on '{runner.name}'.");
        }
    }

    internal static bool CanReclaimAuthority(NetworkObject obj)
    {
        return obj != null && CanReclaimAuthority(obj.Flags);
    }

    public static bool CanReclaimAuthority(NetworkObjectFlags flags)
    {
        if ((flags & NetworkObjectFlags.AllowStateAuthorityOverride) != NetworkObjectFlags.AllowStateAuthorityOverride)
            return false;

        if ((flags & NetworkObjectFlags.MasterClientObject) == NetworkObjectFlags.MasterClientObject)
            return false;

        if ((flags & NetworkObjectFlags.DestroyWhenStateAuthorityLeaves) == NetworkObjectFlags.DestroyWhenStateAuthorityLeaves)
            return false;

        return true;
    }
}
