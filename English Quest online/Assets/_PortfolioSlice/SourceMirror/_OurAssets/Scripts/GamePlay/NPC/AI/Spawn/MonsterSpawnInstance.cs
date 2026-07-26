using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Tracks a spawned monster's owning spawn point and notifies the spawn director on death.
/// </summary>
[DisallowMultipleComponent]
public class MonsterSpawnInstance : MonoBehaviour
{
    private MonsterSpawnPoint _spawnPoint;
    private MonsterDefinitionSO _definition;
    private HealthComponent _health;
    private bool _notifiedDeath;

    public MonsterSpawnPoint SpawnPoint => _spawnPoint;
    public MonsterDefinitionSO Definition => _definition;
    public bool IsLocalSpawn { get; private set; }

    public void Initialize(MonsterSpawnPoint spawnPoint, MonsterDefinitionSO definition, bool isLocalSpawn)
    {
        _spawnPoint = spawnPoint;
        _definition = definition;
        IsLocalSpawn = isLocalSpawn;
    }

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
    }

    private void OnEnable()
    {
        if (_health != null)
            _health.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        if (_health != null)
            _health.OnDied -= HandleDied;
    }

    private void HandleDied(DeathContext _)
    {
        if (_notifiedDeath || _spawnPoint == null)
            return;

        _notifiedDeath = true;

        if (UnityServiceLocator.ServiceLocator.For(this).TryGet<IMonsterSpawnService>(out IMonsterSpawnService service))
            service.NotifyMobDied(_spawnPoint, this);
    }

    public bool IsOrphanedLocalSpawn()
    {
        if (!IsLocalSpawn)
            return false;

        NetworkObject networkObject = GetComponent<NetworkObject>();
        return networkObject == null || !networkObject.IsValid;
    }
}
