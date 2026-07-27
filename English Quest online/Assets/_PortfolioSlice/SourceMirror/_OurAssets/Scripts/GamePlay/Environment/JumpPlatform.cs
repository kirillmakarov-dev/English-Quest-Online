using Fusion;
using UnityEngine;

public class JumpPlatform : NetworkBehaviour
{
    [Header("Settings")]
    [Tooltip("The upward force applied to the player when touching the platform.")]
    [SerializeField] private float _jumpForce = 20f;
    
    [Header("Feedbacks")]
    [Tooltip("Optional feedback to play when a jump is triggered (sound, particles, etc.).")]
    [SerializeField] private MonoBehaviour _feedback;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering is a player with a NetworkCharacterController
        var ncc = other.GetComponent<NetworkCharacterController>();
        if (ncc == null)
            ncc = other.GetComponentInParent<NetworkCharacterController>();

        if (ncc != null)
        {
            // 1. Direction Check: Ensure the player is falling or moving horizontally (not jumping UP through the platform)
            if (ncc.Velocity.y > 0.1f) return;

            // 2. Position Check: Ensure the player hit the TOP of the platform
            // We compare the player's feet (bottom of bounds) to the platform's center.
            var myCollider = GetComponent<Collider>();
            if (myCollider != null)
            {
                // Method A: Raycast to check surface normal (Most accurate for MeshColliders)
                // We cast a ray from player center downwards to find the hit point on THIS collider.
                Ray ray = new Ray(other.bounds.center, Vector3.down);
                float castDist = other.bounds.extents.y + 1.0f; // Cast slightly past feet
                
                if (myCollider.Raycast(ray, out RaycastHit hit, castDist))
                {
                    // If we hit the surface, check if it's flat enough (pointing up)
                    // > 0.5f roughly means within 60 degrees of Up
                    if (Vector3.Dot(hit.normal, Vector3.up) < 0.5f) return;
                }
                else
                {
                    // Method B: Fallback Bounds Check (If Raycast fails, e.g. player inside trigger)
                    // If player's bottom is below the platform's center, they likely hit the side/bottom
                    if (other.bounds.min.y < myCollider.bounds.center.y) return;
                }
            }

            // We apply the jump if we have authority over the player object (State Authority or Input Authority).
            // This ensures the movement is predicted instantly for the local player and authoritative on the server/host.
            if (ncc.Object != null && (ncc.Object.HasStateAuthority || ncc.Object.HasInputAuthority))
            {
                // Set the vertical velocity directly to ensure consistent jump height
                // regardless of how fast the player was falling when they hit the platform.
                var velocity = ncc.Velocity;
                velocity.y = _jumpForce;
                ncc.Velocity = velocity;
                ncc.Grounded = false;

                // Trigger the jump animation on the platform itself
                // Changing search logic to scan parent first as Animator is likely on the main transform or parent.
                var platformAnimator = GetComponentInParent<Animator>(); 
                
                // Fallback: Check siblings if not found directly in parent chain
                if (platformAnimator == null && transform.parent != null)
                {
                     platformAnimator = transform.parent.GetComponentInChildren<Animator>();
                }

                if (platformAnimator != null)
                {
                    // AppLog.Info($"JumpPlatform: Found animator on {platformAnimator.gameObject.name}, searching for 'isJumping'...");
                    bool paramFound = false;
                    foreach (var param in platformAnimator.parameters)
                    {
                        // Check for 'isJumping' (case-insensitive to support IsJumping or isJumping)
                        if (param.name.Equals("isJumping", System.StringComparison.OrdinalIgnoreCase))
                        {
                            if (param.type == AnimatorControllerParameterType.Trigger)
                            {
                                platformAnimator.SetTrigger(param.nameHash);
                                // AppLog.Info($"JumpPlatform: Set Trigger '{param.name}'");
                            }
                            else if (param.type == AnimatorControllerParameterType.Bool)
                            {
                                StartCoroutine(ResetJumpBoolRoutine(platformAnimator, param.nameHash));
                                // AppLog.Info($"JumpPlatform: Started Coroutine for Bool '{param.name}'");
                            }
                            paramFound = true;
                            break;
                        }
                    }

                    if (!paramFound)
                    {
                        AppLog.Warning($"JumpPlatform: Could not find Animator parameter 'IsJumping/isJumping' on {platformAnimator.gameObject.name}. Available parameters: " + string.Join(", ", System.Array.ConvertAll(platformAnimator.parameters, p => p.name)));
                    }
                }
                else
                {
                     AppLog.Warning("JumpPlatform: No Animator found in children!");
                }
                
                // Play feedback locally
                if (_feedback != null)
                {
                    OptionalFeedbackPlayer.Play(_feedback);
                }
            }
        }
    }

    private System.Collections.IEnumerator ResetJumpBoolRoutine(Animator anim, int paramHash)
    {
        anim.SetBool(paramHash, true);
        yield return new WaitForSeconds(0.5f);
        anim.SetBool(paramHash, false);
    }
}

