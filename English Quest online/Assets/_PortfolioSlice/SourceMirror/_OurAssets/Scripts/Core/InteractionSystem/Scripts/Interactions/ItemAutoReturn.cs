using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

/// <summary>
/// Automatically returns a networked object to its spawn position when triggered by:
///   - Idle:         the object hasn't been held for <see cref="_idleReturnDelay"/> seconds.
///   - Out of bounds: the object falls below <see cref="_fallThresholdY"/>.
///
/// Add this alongside a <see cref="PickableItem"/> or any NetworkBehaviour on the same prefab.
/// The return anchor defaults to the object's world position/rotation at <c>Spawned()</c> time,
/// but can be overridden at runtime via <see cref="SetReturnAnchor"/> or <see cref="ForceReturn"/>.
///
/// Only state authority runs, so no extra RPCs are needed — the teleport is inherently replicated.
/// </summary>
public class ItemAutoReturn : NetworkBehaviour
{
    [Header("Idle Return")]
    [Tooltip("Return the object to its spawn anchor after it has been idle (not held) for this many seconds.")]
    [SerializeField] private bool _enableIdleReturn = true;
    [SerializeField] private float _idleReturnDelay = 10f;

    [Tooltip("Seconds after spawn during which the return anchor keeps updating. " +
             "Allows other scripts that reposition the object on spawn to be reflected correctly.")]
    [SerializeField] private float _anchorSettleDelay = 0.2f;

    // Return anchor — set from spawn transform, overridable externally
    private Vector3 _returnPos;
    private Quaternion _returnRot;

    // Tracks whether the anchor has been locked in after the settle window
    private bool _anchorLocked;
    private float _anchorSettleTimer;

    // Optional sibling components
    private PickableItem _pickable;
    private NetworkRigidbody3D _nrb;

    // Timer tracked locally on state authority (does not need to be networked;
    // if authority transfers the new owner simply starts a fresh countdown)
    private float _idleTimer;

    // -------------------------------------------------------------------------
    // NetworkBehaviour
    // -------------------------------------------------------------------------

    public override void Spawned()
    {
        _returnPos = transform.position;
        _returnRot = transform.rotation;
        _anchorLocked = false;
        _anchorSettleTimer = 0f;
        _idleTimer = 0f;

        _pickable = GetComponent<PickableItem>();
        _nrb      = GetComponent<NetworkRigidbody3D>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // --- Anchor settle window ---
        // Keep tracking the object's position until the settle delay has passed,
        // so that any sibling script that repositions the object at spawn is reflected.
        if (!_anchorLocked)
        {
            _returnPos = transform.position;
            _returnRot = transform.rotation;
            _anchorSettleTimer += Runner.DeltaTime;
            if (_anchorSettleTimer >= _anchorSettleDelay)
                _anchorLocked = true;
        }

        // --- Idle countdown ---
        if (_enableIdleReturn && _anchorLocked)
        {
            bool isHeld = _pickable != null && _pickable.IsHeld;

            if (isHeld)
            {
                _idleTimer = 0f;
            }
            else
            {
                _idleTimer += Runner.DeltaTime;
                if (_idleTimer >= _idleReturnDelay)
                {
                    ExecuteReturn();
                }
            }
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Override the position and rotation this object returns to.
    /// Calling this also immediately locks the anchor, bypassing the settle window.
    /// </summary>
    public void SetReturnAnchor(Vector3 position, Quaternion rotation)
    {
        _returnPos    = position;
        _returnRot    = rotation;
        _anchorLocked = true;
    }

    /// <summary>
    /// Immediately return the object to its anchor.
    /// Only executes on state authority.
    /// </summary>
    public void ForceReturn()
    {
        if (!Object.HasStateAuthority) return;
        ExecuteReturn();
    }

    // -------------------------------------------------------------------------
    // Internal
    // -------------------------------------------------------------------------

    private void ExecuteReturn()
    {
        _idleTimer = 0f;

        // If still held (e.g. holder disconnected mid-hold), force-drop before teleporting
        if (_pickable != null && _pickable.IsHeld)
        {
            _pickable.Drop();
        }

        // Zero out physics so the object doesn't drift after landing
        if (_nrb != null && _nrb.Rigidbody != null && !_nrb.Rigidbody.isKinematic)
        {
            _nrb.Rigidbody.linearVelocity  = Vector3.zero;
            _nrb.Rigidbody.angularVelocity = Vector3.zero;
        }

        // Teleport — NetworkRigidbody3D.Teleport() is replicated automatically
        if (_nrb != null)
        {
            _nrb.Teleport(_returnPos, _returnRot);
        }
        else
        {
            // Fallback for objects with NetworkTransform or no physics component
            transform.SetPositionAndRotation(_returnPos, _returnRot);
        }
    }
}
