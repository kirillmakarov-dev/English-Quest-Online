using UnityEngine;

/// <summary>
/// Entry point for combat damage on any entity with <see cref="HealthComponent"/>.
/// Routes networked hits through <see cref="NetworkedHealthSync"/> when present.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(DamageAttributionComponent))]
public class DamageReceiver : MonoBehaviour, IDamageable
{
    [SerializeField] private HealthComponent _health;

    private NetworkedHealthSync _networkSync;
    private DamageAttributionComponent _attribution;

    private void Awake()
    {
        if (_health == null)
            _health = GetComponent<HealthComponent>();

        _networkSync = GetComponent<NetworkedHealthSync>();
        _attribution = GetComponent<DamageAttributionComponent>();
    }

    public void ApplyDamage(float amount, DamageInfo info, GameObject source)
    {
        if (_health == null)
            _health = GetComponent<HealthComponent>();

        if (_networkSync == null)
            _networkSync = GetComponent<NetworkedHealthSync>();

        if (_attribution == null)
            _attribution = GetComponent<DamageAttributionComponent>();

        if (_health == null || amount <= 0f) return;

        info = DamageInfo.WithHitDirection(info, source, transform.position);

        if (_networkSync != null && _networkSync.Object != null && _networkSync.Object.IsValid)
        {
            _networkSync.RequestDamage(amount, info, source);
            return;
        }

        _attribution?.RecordInstigator(source);
        _health.TakeDamage(amount, info);

        if (!_health.IsAlive)
            _health.NotifyDied(_attribution != null ? _attribution.BuildDeathContext() : DeathContext.None);
    }
}
