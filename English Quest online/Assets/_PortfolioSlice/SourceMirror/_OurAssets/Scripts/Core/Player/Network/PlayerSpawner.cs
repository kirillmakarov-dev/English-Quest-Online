using Fusion;
using UnityEngine;

/// <summary>
/// Scene marker that exposes the player prefab to <see cref="PlayerSpawnCoordinator"/>.
/// Spawn logic lives in the coordinator; this only registers scene context.
/// </summary>
public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkPrefabRef _playerPrefab;

    public NetworkPrefabRef PlayerPrefab => _playerPrefab;

    private void Awake()
    {
        PlayerSceneContext.EnsureRegistered(gameObject, _playerPrefab);
    }

    public override void Spawned()
    {
        PlayerSceneContext.EnsureRegistered(gameObject, _playerPrefab);
    }
}
