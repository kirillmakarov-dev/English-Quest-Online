using System.Collections;
using Fusion;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Handles enter/exit interaction for the bike via the <see cref="IInteractable"/> system.
/// Manages local player hide/show and teleport on mount/dismount.
///
/// Visual tracking (seat mascot, remote rider rig toggle) is handled by
/// <see cref="BikeRiderVisualsController"/> on the same object.
/// Collision suppression between the bike body and player CharacterControllers
/// is handled here because it is tied to the object's lifetime.
/// </summary>
public class BikeEntrySystem : NetworkBehaviour, IInteractable, IPlayerJoined, IPlayerLeft
{
    [Header("References")]
    [SerializeField] private NetworkBikeMount _bikeMount;
    [SerializeField] private Transform _exitTransform;

    // Assign the bike's CapsuleCollider here so PhysX contacts between
    // player CharacterControllers and the bike body are suppressed.
    // Without this, two players walking up to the bike push it via PhysX
    // contact resolution even though no OnControllerColliderHit is implemented.
    [SerializeField] private Collider _bikeBodyCollider;

    // Runtime reference typed as IBikeMount so tests can inject a stub.
    // Wired from the serialized field in Spawned(); override with SetBikeMountForTest().
    private IBikeMount _mount;

    /// <summary>Inject a stub implementation — for testing only.</summary>
    public void SetBikeMountForTest(IBikeMount mount) => _mount = mount;

    // -------------------------------------------------------------------------
    // Fusion lifecycle
    // -------------------------------------------------------------------------

    public override void Spawned()
    {
        _mount ??= _bikeMount;  // wire from Inspector if not injected by a test

        // Ignore collision between the bike body and all CharacterControllers
        // already present in the scene (handles the host and players who joined
        // before this NetworkObject was spawned).
        SetCollisionWithAllCharacterControllers(ignore: true);

        if (_mount == null)
        {
            return;
        }

        _mount.OnLocalMounted    += HandleLocalMounted;
        _mount.OnLocalDismounted += HandleLocalDismounted;

        if (_mount.IsLocalPlayerRiding)
        {
            HandleLocalMounted();
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_mount != null)
        {
            _mount.OnLocalMounted    -= HandleLocalMounted;
            _mount.OnLocalDismounted -= HandleLocalDismounted;
        }

        HandleLocalDismounted(teleportToExit: false);
        SetCollisionWithAllCharacterControllers(ignore: false);
    }

    public void PlayerJoined(PlayerRef player)
    {
        // A newly joined player's NetworkObject may not exist yet on this client.
        // Wait one frame so Fusion has time to instantiate it.
        StartCoroutine(IgnoreNewPlayerNextFrame(player));
    }

    public void PlayerLeft(PlayerRef player)
    {
        // Nothing to restore — when the player leaves their NetworkObject is
        // destroyed, so the IgnoreCollision pair is automatically cleaned up.
    }

    private IEnumerator IgnoreNewPlayerNextFrame(PlayerRef player)
    {
        yield return null;
        if (_bikeBodyCollider == null || Runner == null) yield break;
        var playerObj = Runner.GetPlayerObject(player);
        if (playerObj == null) yield break;
        foreach (var cc in playerObj.GetComponentsInChildren<CharacterController>(true))
            Physics.IgnoreCollision(_bikeBodyCollider, cc, true);
    }

    private void SetCollisionWithAllCharacterControllers(bool ignore)
    {
        if (_bikeBodyCollider == null) return;
        foreach (var cc in FindObjectsByType<CharacterController>(FindObjectsSortMode.None))
            Physics.IgnoreCollision(_bikeBodyCollider, cc, ignore);
    }

    // -------------------------------------------------------------------------
    // IInteractable
    // -------------------------------------------------------------------------

    public string InteractionPrompt =>
        _mount != null && _mount.IsLocalPlayerRiding ? "Exit bike" : "Ride";

    // Object == null means there is no Fusion context (e.g. Edit Mode tests).
    // In that case we skip the IsValid guard so mount-state logic is still testable.
    // In production, Spawned() guarantees Object is non-null, so IsValid is always checked.
    public bool CanInteract =>
        _mount != null &&
        (Object == null || Object.IsValid) &&
        (!_mount.IsOccupied || _mount.IsLocalPlayerRiding);

    public bool Interact(PlayerInteraction interactor)
    {
        if (!CanInteract) return false;

        if (_mount.IsLocalPlayerRiding)
            DismountBike();
        else
            MountBike(interactor);

        return true;
    }

    // -------------------------------------------------------------------------
    // Local riding state
    // -------------------------------------------------------------------------

    private bool                       _isRiding;
    private GameObject                 _riderObject;
    private CinemachineCamera          _playerFollowCamera;
    private NetworkCharacterController _riderNCC;
    private PlayerFormSwitcher         _riderFormSwitcher;

    // -------------------------------------------------------------------------
    // Mount
    // -------------------------------------------------------------------------

    private void MountBike(PlayerInteraction interactor)
    {
        CacheRiderReferences(interactor);
        _mount.Mount();
    }

    // -------------------------------------------------------------------------
    // Dismount
    // -------------------------------------------------------------------------

    private void DismountBike()
    {
        _mount.Dismount();

        HandleLocalDismounted();
    }

    private void HandleLocalMounted()
    {
        if (_isRiding)
        {
            return;
        }

        EnsureLocalRiderReferences();

        if (LocalPlayerReadiness.TryGet(Runner, gameObject.scene, out ILocalPlayerReadiness readiness))
            _playerFollowCamera = readiness.TryGetFollowCamera();
        if (_playerFollowCamera != null)
            _playerFollowCamera.Priority = PlayerSceneCamera.InactivePriority;

        if (_riderFormSwitcher != null)
            _riderFormSwitcher.SetVisualsVisible(false);

        // Deactivate the entire player only after the networked rider state says
        // this local player actually owns the seat.
        if (_riderObject != null)
            _riderObject.SetActive(false);

        _isRiding = true;
    }

    private void HandleLocalDismounted()
    {
        HandleLocalDismounted(teleportToExit: true);
    }

    private void HandleLocalDismounted(bool teleportToExit)
    {
        if (!_isRiding && _riderObject == null)
        {
            return;
        }

        EnsureLocalRiderReferences();

        if (_riderFormSwitcher != null)
            _riderFormSwitcher.SetVisualsVisible(true);

        if (_riderObject != null && !_riderObject.activeSelf)
            _riderObject.SetActive(true);

        // Clear the stale bike interactable reference so the interaction UI hides
        // correctly after the player is teleported away from the bike.
        // OnTriggerExit is not reliably fired on Teleport, so we clear it manually.
        _riderObject?.GetPlayerComponent<PlayerInteraction>()?.ForceReleaseInteractable(this);

        if (teleportToExit && _riderNCC != null && _exitTransform != null)
            _riderNCC.Teleport(_exitTransform.position);
        else if (teleportToExit && _riderObject != null && _exitTransform != null)
            _riderObject.transform.position = _exitTransform.position;

        LocalPlayerReadiness.TryGet(Runner, gameObject.scene, out ILocalPlayerReadiness readiness);
        if (readiness != null)
            _playerFollowCamera ??= readiness.TryGetFollowCamera();
        if (_playerFollowCamera != null)
            _playerFollowCamera.Priority = PlayerSceneCamera.ActivePriority;

        if (_riderObject != null && readiness != null)
        {
            CinemachineCamera followCamera = readiness.TryGetFollowCamera();
            PlayerSceneCamera.AssignFollow(followCamera, _riderObject.transform);
        }

        _isRiding             = false;
        _riderObject          = null;
        _playerFollowCamera   = null;
        _riderNCC             = null;
        _riderFormSwitcher    = null;
    }

    private void EnsureLocalRiderReferences()
    {
        if (_riderObject != null)
        {
            return;
        }

        if (Runner == null)
        {
            return;
        }

        NetworkObject localPlayerObject = Runner.GetPlayerObject(Runner.LocalPlayer);
        if (localPlayerObject == null)
        {
            return;
        }

        CacheRiderReferences(localPlayerObject);
    }

    private void CacheRiderReferences(Component riderComponent)
    {
        if (riderComponent == null)
        {
            return;
        }

        PlayerRoot root = PlayerRoot.Get(riderComponent);
        _riderObject = root != null ? root.gameObject : riderComponent.gameObject;
        _riderNCC = PlayerRoot.Resolve<NetworkCharacterController>(riderComponent);
        _riderFormSwitcher = PlayerRoot.Resolve<PlayerFormSwitcher>(riderComponent);

        if (_riderNCC == null)
        {
            Debug.LogError("NetworkCharacterController not found on rider object", _riderObject);
        }
        if (_riderFormSwitcher == null)
        {
            Debug.LogError("PlayerFormSwitcher not found on rider object", _riderObject);
        }
    }

    // -------------------------------------------------------------------------
    // Dismount input — runs on the Bike (always active)
    // -------------------------------------------------------------------------

    private void Update()
    {
        if (!_isRiding || !Object.HasStateAuthority) return;
        if (Input.GetKeyDown(KeyCode.E))
            DismountBike();
    }
}

