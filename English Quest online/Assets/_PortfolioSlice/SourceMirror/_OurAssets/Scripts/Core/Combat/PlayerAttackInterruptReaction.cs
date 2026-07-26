using Fusion;
using UnityEngine;

/// <summary>
/// Cancels in-progress player attack casts when the player takes damage.
/// Subscribes to <see cref="HealthComponent.OnDamageTaken"/> on the health module.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(HealthComponent))]
public sealed class PlayerAttackInterruptReaction : MonoBehaviour
{
    [SerializeField] private HealthComponent _health;

    private NetworkObject _networkObject;

    private void Awake()
    {
        if (_health == null)
            _health = GetComponent<HealthComponent>();

        _networkObject = GetComponentInParent<NetworkObject>();
    }

    private void OnEnable()
    {
        if (_health != null)
            _health.OnDamageTaken += HandleDamageTaken;
    }

    private void OnDisable()
    {
        if (_health != null)
            _health.OnDamageTaken -= HandleDamageTaken;
    }

    private void HandleDamageTaken(float amount, DamageInfo info)
    {
        if (_networkObject != null && _networkObject.IsValid)
        {
            if (_networkObject.HasInputAuthority)
                CancelAttackCast();

            if (_networkObject.HasStateAuthority)
                PlayHitReaction();

            return;
        }

        CancelAttackCast();
        PlayHitReaction();
    }

    private void CancelAttackCast()
    {
        PlayerRoot root = PlayerRoot.Get(this);
        if (root == null) return;

        root.GetPlayerComponent<AttackHitScheduler>()?.CancelPendingHit();
        root.GetPlayerComponent<AttackCastLockService>()?.CancelActiveLock();
    }

    private void PlayHitReaction()
    {
        // Intentionally left empty in the portfolio slice.
    }
}
