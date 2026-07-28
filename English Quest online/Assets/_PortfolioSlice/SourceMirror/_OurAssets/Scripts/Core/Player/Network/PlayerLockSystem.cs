using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Fusion;
using Unity.Cinemachine;
using Fusion.Addons.Physics;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityServiceLocator;

/// <summary>
/// Manages locking and unlocking of player capabilities (Movement, Camera, Interaction).
/// Allows multiple systems to lock capabilities independently.
/// </summary>
public class PlayerLockSystem : NetworkBehaviour, IPlayerLockSystem
{
    public enum LockType
    {
        Movement,
        Camera,
        Interaction,
        Cursor,
        GameplayInput
    }

    // Stores the set of objects that are currently locking each capability
    private Dictionary<LockType, HashSet<object>> _locks = new Dictionary<LockType, HashSet<object>>();

    // Component references to control
    private CinemachineInputAxisController _cameraInput;
    private PlayerInteraction _playerInteraction;
    private NetworkRigidbody3D _networkRigidbody;

    private void Awake()
    {
        // Initialize dictionary
        foreach (LockType type in System.Enum.GetValues(typeof(LockType)))
        {
            _locks[type] = new HashSet<object>();
        }

        // Get components on the player
        _playerInteraction = PlayerRoot.Resolve<PlayerInteraction>(this);
        _networkRigidbody = PlayerRoot.Resolve<NetworkRigidbody3D>(this);
        
        // Only the local player needs to control the camera input (Shared Mode or Client)
        // We delay finding camera input until Spawned or ensure we check IsLocalPlayer if possible.
        // But Awake runs before Spawned. We can find it, but only modify it if we are local player later.
    }

    public override void Spawned()
    {
        // Hide cursor by default for the local player when they spawn
        if (NetworkPlayerOwnership.IsLocal(this))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            ServiceLocator.ForSceneOf(this).Register<IPlayerLockSystem>(this);
            BindCameraInputAsync().Forget();
        }
    }

    private async UniTaskVoid BindCameraInputAsync()
    {
        NetworkRunner runner = Object.Runner;
        if (runner == null || !runner.IsRunning)
            return;

        Scene scene = gameObject.scene;
        if (!LocalPlayerReadiness.TryGet(runner, scene, out ILocalPlayerReadiness readiness))
            return;

        LocalPlayerReadyArgs ready = await readiness.WaitReadyAsync();
        if (ready.FollowCamera != null)
            _cameraInput = ready.FollowCamera.GetComponent<CinemachineInputAxisController>();
    }

    /// <summary>
    /// Locks a specific capability.
    /// </summary>
    /// <param name="type">The capability to lock.</param>
    /// <param name="source">The object requesting the lock (e.g., specific script instance).</param>
    public void Lock(LockType type, object source)
    {
        if (source == null)
        {
            AppLog.Warning("PlayerLockSystem: Attempted to lock with null source.");
            return;
        }

        if (_locks[type].Add(source))
        {
            UpdateCapabilityState(type);
        }
    }

    /// <summary>
    /// Unlocks a specific capability.
    /// </summary>
    /// <param name="type">The capability to unlock.</param>
    /// <param name="source">The object requesting the unlock (must match the locker).</param>
    public void Unlock(LockType type, object source)
    {
        if (_locks[type].Remove(source))
        {
            UpdateCapabilityState(type);
        }
    }

    /// <summary>
    /// Helper to lock multiple capabilities at once.
    /// </summary>
    public void Lock(object source, params LockType[] types)
    {
        foreach (var type in types) Lock(type, source);
    }

    /// <summary>
    /// Helper to unlock multiple capabilities at once.
    /// </summary>
    public void Unlock(object source, params LockType[] types)
    {
        foreach (var type in types) Unlock(type, source);
    }

    /// <summary>
    /// Updates the actual component state based on lock count.
    /// </summary>
    private void UpdateCapabilityState(LockType type)
    {
        bool isLocked = _locks[type].Count > 0;

        switch (type)
        {
            case LockType.Movement:
                if (isLocked) StopPhysicsMovement();
                break;

            case LockType.Camera:
                if (_cameraInput != null)
                {
                    _cameraInput.enabled = !isLocked;
                }
                break;
                
            case LockType.Interaction:
                if (_playerInteraction != null)
                {
                    _playerInteraction.enabled = !isLocked;
                }
                break;

            case LockType.Cursor:
                // Cursor should only be controlled for the local player.
                if (NetworkPlayerOwnership.IsLocal(this))
                {
                    if (isLocked)
                    {
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                    else
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
                break;
        }
    }

    private void StopPhysicsMovement()
    {
        // Stop any residual velocity when locking movement
        if (_networkRigidbody != null && _networkRigidbody.Rigidbody != null && !_networkRigidbody.Rigidbody.isKinematic)
        {
            Vector3 vel = _networkRigidbody.Rigidbody.linearVelocity;
            vel.x = 0;
            vel.z = 0; // Keep Y for gravity? Or just stop horizontal?
            _networkRigidbody.Rigidbody.linearVelocity = vel;
            _networkRigidbody.Rigidbody.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Returns true if the capability is currently locked.
    /// </summary>
    public bool IsLocked(LockType type)
    {
        return _locks.ContainsKey(type) && _locks[type].Count > 0;
    }

    /// <summary>
    /// Returns true if the lock is held by any source other than <paramref name="self"/>.
    /// </summary>
    public bool IsLockedByOther(LockType type, object self)
    {
        return _locks[type].Count > 0 && !_locks[type].Contains(self);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (NetworkPlayerOwnership.IsLocal(this))
        {
            ServiceLocator.ForSceneOf(this).DeregisterIfRegistered<IPlayerLockSystem>();
        }

        UnlockCursor();
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
