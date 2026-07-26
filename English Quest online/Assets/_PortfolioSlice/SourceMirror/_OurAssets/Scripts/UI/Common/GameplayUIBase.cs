using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Base class for any gameplay UI panel that requires mouse control.
/// Handles locking and unlocking the local player automatically so callers
/// never need to touch PlayerLockSystem directly.
///
/// Usage:
///   1. Inherit from GameplayUIBase instead of MonoBehaviour.
///   2. Call BeginInteraction(interactor) at the top of your Open() method when you have a PlayerInteraction.
///   3. Call EndInteraction() at the top of your Close() method.
/// </summary>
public abstract class GameplayUIBase : MonoBehaviour
{
    protected virtual PlayerLockSystem.LockType[] LocksToApply => new[]
    {
        PlayerLockSystem.LockType.Movement,
        PlayerLockSystem.LockType.Camera,
        PlayerLockSystem.LockType.Interaction,
        PlayerLockSystem.LockType.Cursor,
        PlayerLockSystem.LockType.GameplayInput
    };

    /// <summary>
    /// MonoBehaviour used to resolve scene-scoped services such as <see cref="IPlayerLockSystem"/>.
    /// Override when this UI lives outside the gameplay scene (e.g. DontDestroyOnLoad canvases).
    /// </summary>
    protected virtual MonoBehaviour ServiceLocatorContext =>
        _activeInteractor != null ? _activeInteractor : this;

    private PlayerInteraction _activeInteractor;
    private IPlayerLockSystem _locker;

    protected void BeginInteraction(PlayerInteraction interactor = null)
    {
        _activeInteractor = interactor;
        if (!TryResolvePlayerLocker(out _locker))
            return;

        _locker.Lock(this, LocksToApply);
    }

    protected void EndInteraction()
    {
        _locker?.Unlock(this, LocksToApply);
        _locker = null;
        _activeInteractor = null;
    }

    protected bool TryResolvePlayerLocker(out IPlayerLockSystem locker)
    {
        if (_activeInteractor != null)
            return ServiceLocator.For(_activeInteractor).TryGet(out locker);

        return ServiceLocator.For(this).TryGet(out locker);
    }
}
