using System.Collections;
using Fusion;
using UnityEngine;

/// <summary>
/// Scene-level controller that applies knockback impulses to the local player. Lives as a
/// scene-bound NetworkObject inside the gameplay scene (alongside PhonicsMonsterController
/// and the other scene NetworkBehaviours) — NOT on the player prefab — so the player
/// gameobject stays free of any knockback-specific component.
///
/// How it works:
/// - Adapters (KnockbackOnWrongInteraction etc.) call <see cref="Apply"/> with the picker's
///   PlayerInteraction. Apply runs on the picker's own client, which is the player's State
///   Authority in Shared Mode, so the writes to NCC.Velocity / NCC.braking succeed locally
///   and replicate to proxies through NCC's built-in NetworkTRSP sync.
/// - Movement is not locked — <see cref="PlayerMovement"/> keeps running on the owning
///   client and integrates the impulse via NCC each simulation tick. Locking movement from
///   this scene object left clients permanently stuck because unlock depended on scene-object
///   FixedUpdateNetwork, which does not reliably complete on non-host clients.
/// - NCC.braking is temporarily overridden for sharper decay, then restored via a local
///   coroutine after an estimated knockback duration.
/// </summary>
public class KnockbackController : NetworkBehaviour
{
    private const float BrakingRestoreBufferSeconds = 0.25f;

    public static KnockbackController Instance { get; private set; }

    private Coroutine _restoreBrakingCoroutine;
    private NetworkCharacterController _activeNcc;
    private float _originalBraking;

    public override void Spawned()
    {
        Instance = this;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this) Instance = null;

        CancelBrakingRestore(restoreNow: true);
    }

    /// <summary>
    /// Apply a knockback impulse to the player who initiated the interaction. Must be
    /// called from that player's own client (which is the State Authority in Shared Mode).
    /// Calls from non-authoritative clients are silently ignored.
    /// </summary>
    /// <param name="picker">PlayerInteraction of the player who triggered the wrong action.</param>
    /// <param name="horizontalDirection">Push direction. Normalized internally; typically -player.forward.</param>
    /// <param name="horizontalForce">Initial horizontal speed (units/second).</param>
    /// <param name="verticalForce">Initial upward lift (units/second).</param>
    /// <param name="decay">How quickly the horizontal impulse fades. Temporarily overrides NCC.braking; restored at the end.</param>
    public void Apply(PlayerInteraction picker, Vector3 horizontalDirection, float horizontalForce, float verticalForce, float decay)
    {
        if (picker == null || picker.Object == null) return;
        if (!picker.Object.HasStateAuthority) return;

        var ncc = PlayerRoot.Resolve<NetworkCharacterController>(picker);
        if (ncc == null) return;

        // Chained traps: cancel the pending restore and put braking back before re-applying.
        CancelBrakingRestore(restoreNow: true);

        _originalBraking = ncc.braking;
        _activeNcc = ncc;

        Vector3 flat = horizontalDirection;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0f) flat.Normalize();

        Vector3 impulse = flat * horizontalForce;
        impulse.y = verticalForce;

        ncc.Velocity = impulse;
        ncc.braking = Mathf.Max(0f, decay);

        float duration = horizontalForce / Mathf.Max(decay, 0.01f) + BrakingRestoreBufferSeconds;
        _restoreBrakingCoroutine = StartCoroutine(RestoreBrakingAfter(ncc, _originalBraking, duration));
    }

    private void CancelBrakingRestore(bool restoreNow)
    {
        if (_restoreBrakingCoroutine != null)
        {
            StopCoroutine(_restoreBrakingCoroutine);
            _restoreBrakingCoroutine = null;
        }

        if (restoreNow && _activeNcc != null)
        {
            _activeNcc.braking = _originalBraking;
            _activeNcc = null;
        }
    }

    private IEnumerator RestoreBrakingAfter(NetworkCharacterController ncc, float originalBraking, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (ncc != null)
            ncc.braking = originalBraking;

        _restoreBrakingCoroutine = null;
        if (_activeNcc == ncc)
            _activeNcc = null;
    }
}
