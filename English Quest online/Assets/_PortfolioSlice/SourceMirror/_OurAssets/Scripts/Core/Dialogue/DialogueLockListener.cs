using Fusion;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Listens to Dialogue events and locks player capabilities using PlayerLockSystem.
/// </summary>
public class DialogueLockListener : NetworkBehaviour
{
    private PlayerLockSystem _lockSystem;
    private IDialogueService _dialogue;
    private bool _subscribed;

    private void Awake()
    {
        _lockSystem = PlayerRoot.Resolve<PlayerLockSystem>(this);
    }

    private void OnEnable() => Subscribe();

    private void OnDisable() => Unsubscribe();

    public override void Despawned(NetworkRunner runner, bool hasState) => Unsubscribe();

    private void Subscribe()
    {
        if (_subscribed) return;
        if (!ServiceLocator.For(this).TryGet(out _dialogue) || !UnityLifetime.IsAlive(_dialogue))
            return;

        _dialogue.OnDialogueStart += HandleDialogueStart;
        _dialogue.OnDialogueEnd += HandleDialogueEnd;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;

        if (UnityLifetime.IsAlive(_dialogue))
        {
            _dialogue.OnDialogueStart -= HandleDialogueStart;
            _dialogue.OnDialogueEnd -= HandleDialogueEnd;
        }

        _dialogue = null;
        _subscribed = false;
    }

    private void HandleDialogueStart()
    {
        if (Object != null && !Object.HasInputAuthority) return;

        if (_lockSystem != null)
        {
            _lockSystem.Lock(PlayerLockSystem.LockType.Movement, this);
            _lockSystem.Lock(PlayerLockSystem.LockType.Camera, this);
            _lockSystem.Lock(PlayerLockSystem.LockType.Interaction, this);
        }
    }

    private void HandleDialogueEnd()
    {
        if (Object != null && !Object.HasInputAuthority) return;

        if (_lockSystem != null)
        {
            _lockSystem.Unlock(PlayerLockSystem.LockType.Movement, this);
            _lockSystem.Unlock(PlayerLockSystem.LockType.Camera, this);
            _lockSystem.Unlock(PlayerLockSystem.LockType.Interaction, this);
        }
    }
}
