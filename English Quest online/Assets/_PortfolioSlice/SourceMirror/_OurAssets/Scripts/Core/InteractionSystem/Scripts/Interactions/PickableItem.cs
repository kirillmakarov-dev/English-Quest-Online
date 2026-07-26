using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

public class PickableItem : NetworkBehaviour, IInteractable
{
    private NetworkRigidbody3D rb;
    private Rigidbody _directRb; // Fallback for objects without NetworkRigidbody3D (e.g. animal variants using NetworkTransform)
    [Networked] public bool IsHeld { get; set; } = false;
    [Networked] public PlayerInteraction CurrentHolder { get; set; }

    // Events
    public event System.Action<bool> OnHeldStateChanged;
    private ChangeDetector _changes;

    // Telekinesis Settings
    public float smoothTime = 0.2f; // Controls how "loose" the holding feels (higher = looser)
    public float rotateSpeed = 10f;
    public float holdDistance = 3f;
    public float holdHeightOffset = 0.5f;

    // Floating Animation
    public float hoverAmount = 0.15f; // Vertical bobbing distance
    public float hoverSpeed = 1.5f; // Vertical bobbing speed

    [Networked] private Vector3 CurrentVelocity { get; set; } // Networked velocity for SmoothDamp stability

    private bool grab;
    private Quaternion rotationOffset;
    private PlayerInteraction _pendingInteractor; // Cached while waiting for state authority

    public string InteractionPrompt => IsHeld ? "Drop" : "Pick Up";
    public bool CanInteract => this.enabled &&
                               (!IsHeld ||
                                CurrentHolder == null ||
                                (CurrentHolder.Object != null && CurrentHolder.Object.HasInputAuthority));

    // Resolves the active Rigidbody regardless of whether NetworkRigidbody3D is present.
    private Rigidbody ActiveRb => rb != null ? rb.Rigidbody : _directRb;

    private void Awake()
    {
        rb = GetComponent<NetworkRigidbody3D>();
        _directRb = GetComponent<Rigidbody>();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;

        var networkObject = GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            // Ensure AllowStateAuthorityOverride is set for Shared Mode interaction
            if ((networkObject.Flags & NetworkObjectFlags.AllowStateAuthorityOverride) == 0)
            {
                AppLog.Info($"[PickableItem] Enabling 'Allow State Authority Override' on {name} to allow pickup in Shared Mode.", this);
                networkObject.Flags |= NetworkObjectFlags.AllowStateAuthorityOverride;
                UnityEditor.EditorUtility.SetDirty(networkObject);
            }
        }
    }
#endif

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        // Fire initial state to ensure visuals sync when joining late
        OnHeldStateChanged?.Invoke(IsHeld);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // If this item is despawned while still being held (e.g. auto-validated by MuseumAudioStation),
        // immediately release both ActiveInteraction and CurrentInteractable on the holder so the
        // interaction UI clears without waiting for Unity's deferred OnDestroy.
        if (hasState && IsHeld && CurrentHolder != null)
        {
            CurrentHolder.ForceReleaseInteractable(this);
        }
    }

    public override void Render()
    {
        foreach (var change in _changes.DetectChanges(this))
        {
            if (change == nameof(IsHeld))
            {
                OnHeldStateChanged?.Invoke(IsHeld);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // Auto-pickup once authority is granted from a pending request
        if (_pendingInteractor != null)
        {
            var pending = _pendingInteractor;
            _pendingInteractor = null;
            if (!IsHeld && pending.ActiveInteraction == null)
            {
                PickUp(pending);
            }
        }

        if (IsHeld && CurrentHolder != null)
        {
            FollowHolder();
        }
    }

    // Move towards the holder's position (Smooth follow)
    private void FollowHolder()
    {
        if (CurrentHolder.HoldPoint == null || ActiveRb == null) return;

        // Calculate target position in front of the hold point
        Vector3 targetPos = CurrentHolder.HoldPoint.position + (CurrentHolder.HoldPoint.forward * holdDistance);
        targetPos.y += holdHeightOffset;

        // Add floating/bobbing effect independent of player look
        // This adds a gentle up-and-down motion like it's hovering
        targetPos.y += Mathf.Sin(Runner.SimulationTime * hoverSpeed) * hoverAmount;

        // Use SmoothDamp for "fluid" movement instead of stiff Lerp
        // This creates that "drag" or "weight" feeling of telekinesis
        // We use a local variable to pass by ref, then store it back in the Networked property
        Vector3 velocity = CurrentVelocity;
        Vector3 nextPos = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime, Mathf.Infinity, Runner.DeltaTime);
        CurrentVelocity = velocity;

        // Move using Rigidbody for better physics interaction and interpolation
        ActiveRb.MovePosition(nextPos);

        // Smooth rotation
        // Quaternion nextRot = Quaternion.Lerp(transform.rotation, CurrentHolder.HoldPoint.rotation, rotateSpeed * Runner.DeltaTime);

        // ActiveRb.MoveRotation(nextRot);


        Quaternion targetRotation = CurrentHolder.HoldPoint.rotation * rotationOffset;
        Quaternion nextRot = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            rotateSpeed * Runner.DeltaTime);

        ActiveRb.MoveRotation(nextRot);

    }


    public bool Interact(PlayerInteraction interactor)
    {
        if (!CanInteract) return false;

        // 1. Check Authority
        if (!Object.HasStateAuthority)
        {
            _pendingInteractor = interactor;
            Object.RequestStateAuthority();
            return true; // Accept the intent; pickup will execute once authority arrives
        }

        // 2. Perform Interaction (only if we have authority)
        if (IsHeld)
        {
            // Only the current holder can drop.
            Drop();
            return true;
        }
        else
        {
            PickUp(interactor);
            return true;
        }
    }

    private void PickUp(PlayerInteraction interactor)
    {
        // Safety check: Don't pick up if already busy
        if (interactor.ActiveInteraction != null) return;

        IsHeld = true;
        CurrentHolder = interactor;

        // Lock the player interaction
        CurrentHolder.LockInteraction(this);

        CurrentVelocity = Vector3.zero; // Reset damp velocity to prevent jumping

        if (ActiveRb != null)
        {
            if (!ActiveRb.isKinematic)
            {
                ActiveRb.linearVelocity = Vector3.zero;
                ActiveRb.angularVelocity = Vector3.zero;
            }
            ActiveRb.isKinematic = true;
        }

        if (!grab)
        {
            rotationOffset = Quaternion.Inverse(CurrentHolder.HoldPoint.rotation) * transform.rotation;
            grab = true;
        }
    }

    public void Drop()
    {
        // Unlock the player interaction
        if (CurrentHolder != null)
        {
            CurrentHolder.UnlockInteraction(this);
        }

        IsHeld = false;
        CurrentHolder = null;
        grab = false;

        if (ActiveRb != null)
        {
            ActiveRb.isKinematic = false;
        }
    }
}
