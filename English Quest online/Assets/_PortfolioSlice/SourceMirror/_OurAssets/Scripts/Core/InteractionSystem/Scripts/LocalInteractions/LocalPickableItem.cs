using UnityEngine;
using System;

/// <summary>
/// Local version of PickableItem.
///
/// The differnce between the 2 scripts:
/// 
/// PickableItem is networked:
/// - It inherits from NetworkBehaviour.
/// - It uses NetworkObject state authority.
/// - It stores held state in networked fields.
/// - It moves the item through Fusion's network simulation.
/// - This is useful when all players should share the same pickup object.
///
/// LocalPickableItem is not networked:
/// - It inherits from MonoBehaviour.
/// - It uses the existing IInteractable system.
/// - It only responds to the local player's interaction input.
/// - It moves the item locally using a normal Rigidbody.
/// - It does not use NetworkObject, NetworkTransform, state authority, or RPCs.
///
/// Use this for daily quest items that should exist independently for each player,
/// so one player's pickup/drop does not affect the same item for other players.
/// </summary>

[RequireComponent(typeof(Rigidbody))] // Makes sure the object has a Rigidbody.
public class LocalPickableItem : MonoBehaviour, IInteractable
{
    // Control how the item behaves while held.
    [Header("Hold Settings")]
    [SerializeField] private float smoothTime = 0.2f; // How softly it follows the hold point.
    [SerializeField] private float rotateSpeed = 10f; // How quickly it rotates toward the hold point rotation.
    [SerializeField] private float holdDistance = 3f; // How far in front of the player the item floats.
    [SerializeField] private float holdHeightOffset = 2f; // How high the item floats.
    
    // Small floating animation while held.
    [SerializeField] private float hoverAmount = 0.15f; 
    [SerializeField] private float hoverSpeed = 1.5f;

    private Rigidbody _rb;
    private PlayerInteraction _currentHolder;
    private Vector3 _velocity;
    private Quaternion _rotationOffset;
    private bool _isHeld;

    // Decides what the player sees in the interaction UI.
    public string InteractionPrompt => _isHeld ? "Drop" : "Pick Up";
    public bool CanInteract => enabled && (!_isHeld || _currentHolder != null);

    public event Action<bool> OnHeldStateChanged;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // If the item is being held, it moves toward the player’s hold point.
        if (_isHeld && _currentHolder != null)
            FollowHolder();
    }

    // Called when the player presses the interaction key
    public bool Interact(PlayerInteraction interactor)
    {
        if (!CanInteract) return false;

        if (_isHeld)
            Drop();
        else
            PickUp(interactor);

        return true;
    }

    // Starts holding the item.
    private void PickUp(PlayerInteraction interactor)
    {
        if (interactor == null || interactor.ActiveInteraction != null) return;

        _isHeld = true;
        OnHeldStateChanged?.Invoke(true);

        _currentHolder = interactor;
        _velocity = Vector3.zero;

        _currentHolder.LockInteraction(this);

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
        }

        if (_currentHolder.HoldPoint != null)
            _rotationOffset = Quaternion.Inverse(_currentHolder.HoldPoint.rotation) * transform.rotation;
    }

    // Releases the item.
    public void Drop()
    {
        if (_currentHolder != null)
            _currentHolder.UnlockInteraction(this);

        _isHeld = false;
        OnHeldStateChanged?.Invoke(false);

        _currentHolder = null;

        if (_rb != null)
            _rb.isKinematic = false;
    }

    public void ForceDrop()
    {
        Drop();
    }

    // Moves the item while the player is holding it
    private void FollowHolder()
    {
        if (_currentHolder.HoldPoint == null || _rb == null) return;

        Transform holdPoint = _currentHolder.HoldPoint;

        Vector3 targetPos = holdPoint.position + holdPoint.forward * holdDistance;
        targetPos.y += holdHeightOffset;
        targetPos.y += Mathf.Sin(Time.time * hoverSpeed) * hoverAmount;

        Vector3 nextPos = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref _velocity,
            smoothTime);

        Quaternion targetRot = holdPoint.rotation * _rotationOffset;
        Quaternion nextRot = Quaternion.Lerp(
            transform.rotation,
            targetRot,
            rotateSpeed * Time.deltaTime);

        _rb.MovePosition(nextPos);
        _rb.MoveRotation(nextRot);
    }
}