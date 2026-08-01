using UnityEngine;
using UnityServiceLocator;
using System;

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

/// <summary>
/// Shared runtime lifecycle for quest-owned mini-game bootstraps.
/// Centralizes open/close/completion callbacks so every mini-game behaves the same way.
/// </summary>
public abstract class QuestMiniGameRuntimeBase : GameplayUIBase
{
    private Action _onCompleted;
    private Action _onClosed;
    private bool _completionTriggered;

    public abstract string RuntimeTypeId { get; }
    public virtual bool SupportsManualClose => true;
    public bool IsMiniGameOpen { get; private set; }

    public event Action Opened;
    public event Action Closed;
    public event Action Completed;

    protected void BeginMiniGameSession(PlayerInteraction interactor, Action onCompleted, Action onClosed)
    {
        ResetMiniGameSessionState();

        _onCompleted = onCompleted;
        _onClosed = onClosed;
        _completionTriggered = false;
        IsMiniGameOpen = true;

        BeginInteraction(interactor);
        Opened?.Invoke();
    }

    protected void NotifyMiniGameCompleted()
    {
        if (!IsMiniGameOpen || _completionTriggered)
            return;

        _completionTriggered = true;
        IsMiniGameOpen = false;

        Action completed = _onCompleted;
        _onCompleted = null;
        _onClosed = null;

        EndInteraction();
        Completed?.Invoke();
        completed?.Invoke();
    }

    protected void NotifyMiniGameClosed()
    {
        if (_completionTriggered)
        {
            EndInteraction();
            _onCompleted = null;
            _onClosed = null;
            IsMiniGameOpen = false;
            return;
        }

        if (!IsMiniGameOpen)
            return;

        IsMiniGameOpen = false;

        Action closed = _onClosed;
        _onCompleted = null;
        _onClosed = null;

        EndInteraction();
        Closed?.Invoke();
        closed?.Invoke();
    }

    protected void ResetMiniGameSessionState()
    {
        _onCompleted = null;
        _onClosed = null;
        _completionTriggered = false;

        if (IsMiniGameOpen)
        {
            IsMiniGameOpen = false;
            EndInteraction();
        }
    }

    protected virtual void OnDestroy()
    {
        ResetMiniGameSessionState();
    }
}
