using Fusion;
using UnityEngine;

/// <summary>
/// Spawns and tracks the session-level <see cref="NetworkSessionBridge"/> on the master client.
/// </summary>
public static class NetworkSessionBridgeSpawner
{
    public static void TrySpawn(NetworkRunner runner, NetworkPrefabRef bridgePrefab)
    {
        if (runner == null || !runner.IsRunning || !bridgePrefab.IsValid)
            return;

        if (!runner.IsSharedModeMasterClient)
            return;

        if (NetworkSessionBridge.TryGetInstance(out _))
            return;

        runner.Spawn(bridgePrefab);
    }
}
