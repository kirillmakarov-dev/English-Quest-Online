using System.Collections;
using UnityEngine;

/// <summary>
/// Applies brief movement locks for instant attack abilities.
/// Uses a single lock owner (<see cref="AttackCastLockService"/>) so rapid overlapping
/// casts always release movement when the latest window ends.
/// </summary>
[DisallowMultipleComponent]
public class AttackCastLockService : MonoBehaviour
{
    [SerializeField] private PlayerLockSystem _lockSystem;

    private Coroutine _activeLock;

    private void Awake()
    {
        if (_lockSystem == null)
            _lockSystem = PlayerRoot.Resolve<PlayerLockSystem>(this);
    }

    public void LockMovementBriefly(object source, float duration)
    {
        if (duration <= 0f || _lockSystem == null) return;

        if (source != null)
            _lockSystem.Unlock(PlayerLockSystem.LockType.Movement, source);

        CancelActiveLockInternal();
        _activeLock = StartCoroutine(BriefLock(duration));
    }

    /// <summary>Stops an in-progress brief lock when the player is interrupted mid-attack.</summary>
    public void CancelActiveLock() => CancelActiveLockInternal();

    private void CancelActiveLockInternal()
    {
        if (_activeLock != null)
        {
            StopCoroutine(_activeLock);
            _activeLock = null;
        }

        if (_lockSystem != null)
            _lockSystem.Unlock(PlayerLockSystem.LockType.Movement, this);
    }

    private IEnumerator BriefLock(float duration)
    {
        _lockSystem.Lock(PlayerLockSystem.LockType.Movement, this);
        yield return new WaitForSeconds(duration);
        _lockSystem.Unlock(PlayerLockSystem.LockType.Movement, this);
        _activeLock = null;
    }

    private void OnDisable()
    {
        CancelActiveLockInternal();
    }
}
