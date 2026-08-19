using UnityEngine;
using UnityServiceLocator;
using System;
using System.Collections;

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
    [SerializeField, Min(0f)] private float completionAutoCloseDelay = 2f;

    private Action _onCompleted;
    private Action _onClosed;
    private bool _completionTriggered;
    private bool _completionPending;
    private bool _panelCloseFeedbackPlayed;
    private Coroutine _completionRoutine;

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
        _panelCloseFeedbackPlayed = false;
        IsMiniGameOpen = true;

        BeginInteraction(interactor);
        Opened?.Invoke();
    }

    protected void NotifyMiniGameCompleted()
    {
        CancelPendingCompletionRoutine();

        if (!IsMiniGameOpen || _completionTriggered)
            return;

        _completionPending = false;
        _completionTriggered = true;
        IsMiniGameOpen = false;

        Action completed = _onCompleted;
        _onCompleted = null;
        _onClosed = null;

        EndInteraction();
        Completed?.Invoke();
        completed?.Invoke();
    }

    protected void NotifyMiniGameCompletedDelayed(Action onBeforeCompleteClose)
    {
        if (!IsMiniGameOpen || _completionTriggered || _completionPending)
            return;

        _completionPending = true;
        CancelPendingCompletionRoutine();
        _completionRoutine = StartCoroutine(CompleteAfterDelayRoutine(onBeforeCompleteClose));
    }

    protected void NotifyMiniGameClosed()
    {
        bool belongsToActiveSession = IsMiniGameOpen || _completionTriggered || _completionPending;
        if (belongsToActiveSession && !_panelCloseFeedbackPlayed)
        {
            _panelCloseFeedbackPlayed = true;
            GameplayAudioAtmosphere.PlayMiniGamePanelClosed();
        }

        if (_completionPending)
        {
            CancelPendingCompletionRoutine();
            NotifyMiniGameCompleted();
            return;
        }

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
        CancelPendingCompletionRoutine();
        _onCompleted = null;
        _onClosed = null;
        _completionTriggered = false;
        _completionPending = false;
        _panelCloseFeedbackPlayed = false;

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

    private IEnumerator CompleteAfterDelayRoutine(Action onBeforeCompleteClose)
    {
        if (completionAutoCloseDelay > 0f)
            yield return new WaitForSecondsRealtime(completionAutoCloseDelay);

        _completionRoutine = null;
        NotifyMiniGameCompleted();
        onBeforeCompleteClose?.Invoke();
    }

    private void CancelPendingCompletionRoutine()
    {
        if (_completionRoutine == null)
            return;

        StopCoroutine(_completionRoutine);
        _completionRoutine = null;
    }
}
