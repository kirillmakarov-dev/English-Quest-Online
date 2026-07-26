using System.Collections;
using Fusion;
using UnityEngine;

/// <summary>
/// Manages the rider's visual representation on the bike for ALL clients.
///
/// Subscribes to <see cref="NetworkBikeMount.OnRiderChanged"/> and, whenever the
/// networked rider changes, hides the rider's world-space animal rig and shows a
/// small seat mascot parented to the bike's seat transform instead.
///
/// Separated from <see cref="BikeEntrySystem"/> to follow single-purpose guidelines.
/// </summary>
public class BikeRiderVisualsController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private NetworkBikeMount _bikeMount;
    [SerializeField] private Transform _seatTransform;
    [SerializeField] private RuntimeAnimatorController _rideAnimatorController;

    // Runtime reference typed as IBikeMount so tests can inject a stub.
    // Wired from the serialized field in Spawned(); override with SetBikeMountForTest().
    private IBikeMount _mount;

    /// <summary>Inject a stub implementation — for testing only.</summary>
    public void SetBikeMountForTest(IBikeMount mount) => _mount = mount;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private PlayerFormSwitcher _trackedFormSwitcher;
    private GameObject         _seatMascotInstance;
    private Coroutine          _refreshRoutine;

    // -------------------------------------------------------------------------
    // Fusion lifecycle
    // -------------------------------------------------------------------------

    public override void Spawned()
    {
        _mount ??= _bikeMount;  // wire from Inspector if not injected by a test

        if (_mount == null) return;

        _mount.OnRiderChanged += HandleRiderChanged;

        // Handle the case where this NetworkObject is spawned after a rider has
        // already mounted (late-join or host re-simulation after scene load).
        ApplyRiderVisuals(_mount.RidingPlayer);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_mount != null)
            _mount.OnRiderChanged -= HandleRiderChanged;

        CancelRefresh();
        ClearSeatMascot();
        RestoreTrackedVisuals();
    }

    // -------------------------------------------------------------------------
    // Rider change handler
    // -------------------------------------------------------------------------

    private void HandleRiderChanged(PlayerRef previousRider, PlayerRef currentRider)
        => ApplyRiderVisuals(currentRider);

    private void ApplyRiderVisuals(PlayerRef rider)
    {
        CancelRefresh();
        ClearSeatMascot();
        RestoreTrackedVisuals();

        if (rider == PlayerRef.None) return;

        if (TryGetFormSwitcher(rider, out PlayerFormSwitcher switcher))
        {
            BeginTracking(switcher);
            return;
        }

        // Rider's NetworkObject may not have been instantiated on this client yet —
        // retry for up to 30 frames before giving up.
        _refreshRoutine = StartCoroutine(WaitAndTrack(rider));
    }

    private IEnumerator WaitAndTrack(PlayerRef rider)
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            yield return null;

            if (_mount == null || _mount.RidingPlayer != rider)
            {
                _refreshRoutine = null;
                yield break;
            }

            if (TryGetFormSwitcher(rider, out PlayerFormSwitcher switcher))
            {
                _refreshRoutine = null;
                BeginTracking(switcher);
                yield break;
            }
        }

        _refreshRoutine = null;
    }

    // -------------------------------------------------------------------------
    // Tracking helpers
    // -------------------------------------------------------------------------

    private void BeginTracking(PlayerFormSwitcher switcher)
    {
        _trackedFormSwitcher = switcher;
        if (_trackedFormSwitcher == null) return;

        _trackedFormSwitcher.SetVisualsVisible(false);
        _trackedFormSwitcher.OnFormChanged += OnTrackedFormChanged;
        RefreshSeatMascot();
    }

    private void RestoreTrackedVisuals()
    {
        if (_trackedFormSwitcher == null) return;

        _trackedFormSwitcher.OnFormChanged -= OnTrackedFormChanged;
        _trackedFormSwitcher.SetVisualsVisible(true);
        _trackedFormSwitcher = null;
    }

    private void OnTrackedFormChanged(AnimalFormDefinitionSO form) => RefreshSeatMascot();

    // -------------------------------------------------------------------------
    // Seat mascot
    // -------------------------------------------------------------------------

    private void RefreshSeatMascot()
    {
        ClearSeatMascot();

        if (_seatTransform == null || _trackedFormSwitcher == null) return;

        AnimalFormDefinitionSO riderForm = _trackedFormSwitcher.GetCurrentFormDefinition();
        if (riderForm?.rigPrefab == null) return;

        _seatMascotInstance = Instantiate(riderForm.rigPrefab, _seatTransform);
        _seatMascotInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        Animator animator = _seatMascotInstance.GetComponent<Animator>();
        // Swap the rig's animator controller for the bike-riding version, if assigned.
        if (animator != null && _rideAnimatorController != null)
        {
            animator.runtimeAnimatorController = _rideAnimatorController;
        }
    }

    private void ClearSeatMascot()
    {
        if (_seatMascotInstance == null) return;
        Destroy(_seatMascotInstance);
        _seatMascotInstance = null;
    }

    // -------------------------------------------------------------------------
    // Utilities
    // -------------------------------------------------------------------------

    private bool TryGetFormSwitcher(PlayerRef player, out PlayerFormSwitcher switcher)
    {
        switcher = null;
        if (Runner == null || player == PlayerRef.None) return false;
        NetworkObject playerObject = Runner.GetPlayerObject(player);
        if (playerObject == null) return false;
        return playerObject.TryGetComponent(out switcher);
    }

    private void CancelRefresh()
    {
        if (_refreshRoutine == null) return;
        StopCoroutine(_refreshRoutine);
        _refreshRoutine = null;
    }
}
