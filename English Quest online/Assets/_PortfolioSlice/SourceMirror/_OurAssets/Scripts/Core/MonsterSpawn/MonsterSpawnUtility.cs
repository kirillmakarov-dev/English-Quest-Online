using Fusion;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Shared spawn helpers for <see cref="MonsterSpawner"/> and <see cref="MonsterSpawnDirector"/>.
/// </summary>
public static class MonsterSpawnUtility
{
    public static NetworkObject SpawnNetworked(
        NetworkRunner runner,
        MonsterDefinitionSO definition,
        Vector3 position,
        Quaternion rotation)
    {
        if (runner == null)
        {
            AppLog.Error("[MonsterSpawnUtility] NetworkRunner is null.");
            return null;
        }

        if (definition == null || !definition.HasNetworkedPrefab)
        {
            AppLog.Error("[MonsterSpawnUtility] Monster definition or networked prefab is not set.");
            return null;
        }

        if (!definition.NetworkedPrefab.TryGetComponent(out NetworkObject _))
        {
            AppLog.Error("[MonsterSpawnUtility] Networked prefab is missing a NetworkObject component.");
            return null;
        }

        return runner.Spawn(
            definition.NetworkedPrefab,
            position,
            rotation,
            onBeforeSpawned: (_, spawnedObject) => AnchorNetworkSpawn(spawnedObject, position, rotation));
    }

    private static void AnchorNetworkSpawn(NetworkObject networkObject, Vector3 position, Quaternion rotation)
    {
        if (networkObject == null)
            return;

        if (networkObject.TryGetComponent(out NavMeshAgent navAgent))
            navAgent.enabled = false;

        networkObject.transform.SetPositionAndRotation(position, rotation);

        if (networkObject.TryGetComponent(out NetworkTransform networkTransform))
            networkTransform.Teleport(position, rotation);
    }

    public static GameObject SpawnLocal(
        MonsterDefinitionSO definition,
        Vector3 position,
        Quaternion rotation)
    {
        if (definition == null || !definition.HasLocalPrefab)
        {
            AppLog.Error("[MonsterSpawnUtility] Monster definition or local prefab is not set.");
            return null;
        }

        return Object.Instantiate(definition.LocalPrefab, position, rotation);
    }
}
