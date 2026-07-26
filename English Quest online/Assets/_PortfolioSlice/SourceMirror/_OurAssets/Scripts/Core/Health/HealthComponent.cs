using System;
using UnityEngine;

/// <summary>
/// Self-contained health system. Works on any GameObject — networked or fully local.
/// Zero Fusion imports; add <see cref="NetworkedHealthSync"/> alongside this component
/// to replicate state over Photon Fusion when needed.
/// </summary>
public class HealthComponent : MonoBehaviour
{
    [SerializeField] private HealthStatsSO _stats;

    private float _currentHP;
    private float _invincibilityTimer;
    private bool _initialized;

    // ── Read-only state ───────────────────────────────────────────────────────

    public float CurrentHP  => _currentHP;
    public float MaxHP      => _stats != null ? _stats.maxHP : 1f;
    public bool  IsAlive    => _currentHP > 0f;
    public bool  IsInvincible => _invincibilityTimer > 0f;
    public float NormalizedHP => MaxHP > 0f ? _currentHP / MaxHP : 0f;

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired whenever HP changes. Parameters: (currentHP, maxHP).</summary>
    public event Action<float, float> OnHealthChanged;

    /// <summary>Fired when a hit lands and was not blocked by invincibility frames. Parameters: (amount, info).</summary>
    public event Action<float, DamageInfo> OnDamageTaken;

    /// <summary>Fired once when HP reaches zero. Killer context is supplied by the damage authority.</summary>
    public event Action<DeathContext> OnDied;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _currentHP = MaxHP;
        _initialized = true;
    }

    private void OnEnable()
    {
        // Broadcast current state to any late-subscribing listeners (e.g. HealthBarView).
        if (_initialized)
            NotifyChanged();
    }

    private void Update()
    {
        TickInvincibility();
        TickRegen();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies damage. Respects invincibility frames and alive check.
    /// On networked entities, call this only from the state authority.
    /// </summary>
    public void TakeDamage(float amount, DamageInfo info)
    {
        if (!IsAlive || IsInvincible || amount <= 0f) return;

        _currentHP = Mathf.Clamp(_currentHP - amount, 0f, MaxHP);

        if (_stats != null && _stats.invincibilityDurationOnHit > 0f)
            _invincibilityTimer = _stats.invincibilityDurationOnHit;

        NotifyChanged();
        OnDamageTaken?.Invoke(amount, info);
    }

    /// <summary>
    /// Raises <see cref="OnDied"/> after HP has reached zero.
    /// Call from the damage authority once lethal damage has been applied.
    /// </summary>
    public void NotifyDied(DeathContext context)
        => OnDied?.Invoke(context);

    /// <summary>Restores HP. Clamped to MaxHP. Ignores dead state — use <see cref="Revive"/> to resurrect.</summary>
    public void Heal(float amount)
    {
        if (!IsAlive || amount <= 0f) return;

        float previous = _currentHP;
        _currentHP = Mathf.Min(_currentHP + amount, MaxHP);

        if (_currentHP != previous)
            NotifyChanged();
    }

    /// <summary>Restores the entity to life. Optionally specify HP on revive; defaults to full HP.</summary>
    public void Revive(float hpOnRevive = -1f)
    {
        _currentHP = hpOnRevive > 0f ? Mathf.Min(hpOnRevive, MaxHP) : MaxHP;
        _invincibilityTimer = 0f;
        GetComponent<DamageAttributionComponent>()?.Clear();
        NotifyChanged();
    }

    /// <summary>Grants temporary invincibility. Extends any remaining i-frames.</summary>
    public void GrantInvincibility(float duration)
    {
        if (duration <= 0f) return;
        _invincibilityTimer = Mathf.Max(_invincibilityTimer, duration);
    }

    /// <summary>
    /// Sets HP directly without applying damage logic, invincibility, or one-shot events.
    /// Called by <see cref="NetworkedHealthSync"/> on proxy clients to push the replicated value.
    /// </summary>
    public void SetHP(float value)
    {
        _currentHP = Mathf.Clamp(value, 0f, MaxHP);
        NotifyChanged();
    }

    /// <summary>
    /// Raises <see cref="OnDamageTaken"/> without modifying HP.
    /// Called by <see cref="NetworkedHealthSync"/> on proxy clients to replicate the hit event.
    /// </summary>
    public void NetworkInvokeDamageTaken(float amount, DamageInfo info)
        => OnDamageTaken?.Invoke(amount, info);

    /// <summary>
    /// Raises <see cref="OnDied"/> without modifying HP.
    /// Called by <see cref="NetworkedHealthSync"/> on proxy clients to replicate the death event.
    /// </summary>
    public void NetworkInvokeDied(DeathContext context)
        => OnDied?.Invoke(context);

    // ── Private ───────────────────────────────────────────────────────────────

    private void TickInvincibility()
    {
        if (_invincibilityTimer > 0f)
            _invincibilityTimer = Mathf.Max(0f, _invincibilityTimer - Time.deltaTime);
    }

    private void TickRegen()
    {
        if (_stats == null || _stats.regenPerSecond <= 0f) return;
        if (!IsAlive || _currentHP >= MaxHP) return;

        Heal(_stats.regenPerSecond * Time.deltaTime);
    }

    private void NotifyChanged()
        => OnHealthChanged?.Invoke(_currentHP, MaxHP);
}
