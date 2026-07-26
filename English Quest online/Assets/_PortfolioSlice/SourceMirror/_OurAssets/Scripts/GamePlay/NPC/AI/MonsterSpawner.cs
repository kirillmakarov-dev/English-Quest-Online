using Fusion;
using UnityEngine;

/// <summary>
/// Spawns a monster from a <see cref="MonsterDefinitionSO"/> at this transform.
/// Place on spawn-point transforms for runtime encounters; scene-placed monsters can use variant prefabs directly.
/// </summary>
public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] private MonsterDefinitionSO _definition;

    public MonsterDefinitionSO Definition => _definition;

    public NetworkObject Spawn(NetworkRunner runner)
    {
        transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
        return MonsterSpawnUtility.SpawnNetworked(runner, _definition, position, rotation);
    }

    public GameObject SpawnLocal()
    {
        transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
        return MonsterSpawnUtility.SpawnLocal(_definition, position, rotation);
    }
}
