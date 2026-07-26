using System;
using UnityEngine;

public class GroundDetector : MonoBehaviour
{
    public event Action<bool> OnGroundedStatusChanged;

    [SerializeField] private Transform _groundCheck;
    [SerializeField] private float _groundRadius = 0.2f;
    [SerializeField] private LayerMask _groundLayer;

    public bool IsGrounded { get; private set; }
    public Transform GroundCheck => _groundCheck;
    public float GroundRadius => _groundRadius;
    public LayerMask GroundLayer => _groundLayer;

    /// <summary>
    /// Performs the ground check and invokes OnGroundedStatusChanged if the state changes.
    /// Should be called from FixedUpdateNetwork() in the controlling NetworkBehaviour.
    /// </summary>
    public void Detect(Vector3 fallbackPosition, bool currentGroundedState)
    {
        bool newGroundedState;

        if (_groundCheck != null)
        {
            // Start 0.1f above the check point so rays don't begin inside the floor (skinWidth).
            // Ray length of 0.45f covers the CharacterController stepOffset (~0.3f) so stair
            // transitions never push feet beyond ray reach.
            Vector3 center = _groundCheck.position + Vector3.up * 0.1f;
            newGroundedState = false;
            foreach (Vector3 offset in _rayOffsets)
            {
                if (RayHitsGround(center + offset * _groundRadius))
                {
                    newGroundedState = true;
                    break;
                }
            }
        }
        else
        {
            // Fallback ground check from the center of the player
            newGroundedState = PhysicsSceneQueries.Raycast(
                this,
                fallbackPosition + Vector3.up * 0.1f,
                Vector3.down,
                0.2f,
                _groundLayer,
                QueryTriggerInteraction.Ignore);
        }

        // Compare against our own internal state, not the externally passed networked state.
        // Using external state caused a stuck-false loop: once both were false they always matched
        // and the event never fired again to restore grounded.
        if (IsGrounded != newGroundedState)
        {
            IsGrounded = newGroundedState;
            OnGroundedStatusChanged?.Invoke(IsGrounded);
        }
    }

    private static readonly Vector3[] _rayOffsets =
    {
        Vector3.zero,
        Vector3.right,
        Vector3.left,
        Vector3.forward,
        Vector3.back
    };

    private bool RayHitsGround(Vector3 origin)
    {
        // 0.45f = start offset (0.1f) + stepOffset (0.3f) + margin (0.05f)
        return PhysicsSceneQueries.Raycast(this, origin, Vector3.down, 0.45f, _groundLayer, QueryTriggerInteraction.Ignore);
    }

    /// <summary>
    /// Updates the position and size of the ground check sphere to match the character's form.
    /// </summary>
    /// <param name="localPosition">The local position (usually at the feet of the collider).</param>
    /// <param name="radius">The radius of the ground check sphere.</param>
    public void UpdateDetectionSettings(Vector3 localPosition, float radius)
    {
        if (_groundCheck != null)
        {
            _groundCheck.localPosition = localPosition;
        }
        
        // Ensure radius is reasonable (e.g. not larger than collider preventing wall-climb)
        _groundRadius = radius;
    }

    private void OnDrawGizmosSelected()
    {
        if (_groundCheck != null)
        {
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(_groundCheck.position, _groundRadius);
        }
    }
}
