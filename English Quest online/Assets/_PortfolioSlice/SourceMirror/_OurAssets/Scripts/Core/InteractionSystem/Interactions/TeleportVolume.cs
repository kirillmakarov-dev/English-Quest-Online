using Fusion;
using UnityEngine;

namespace Interactions
{
    /// <summary>
    /// Teleports a player to a specific destination when they enter the trigger volume.
    /// Useful for hazards (like water/lakes) or shortcuts.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TeleportVolume : MonoBehaviour
    {
        [Header("Teleport Settings")]
        [Tooltip("The transform where the player will be teleported to.")]
        [SerializeField] private Transform _destinationPoint;

        [Tooltip("Offset applied to the destination position (e.g., specific height adjustment).")]
        [SerializeField] private Vector3 _padding;

        [Tooltip("If true, resets the player's velocity upon teleporting.")]
        [SerializeField] private bool _resetVelocity = true;

        private void OnTriggerEnter(Collider other)
        {
            // Try to get the NetworkCharacterController from the colliding object
            var characterController = other.GetComponent<NetworkCharacterController>();
            
            // If not found on the collider object, check parent
            if (characterController == null)
            {
                characterController = other.GetComponentInParent<NetworkCharacterController>();
            }

            // Validations
            if (characterController == null) return;
            if (_destinationPoint == null)
            {
                AppLog.Warning($"[TeleportVolume] No destination point assigned on {gameObject.name}!");
                return;
            }

            // In Fusion Shared Mode, we typically want the client with State Authority over the object
            // to perform the movement/teleport to ensure it syncs correctly.
            if (characterController.Object != null && characterController.Object.HasStateAuthority)
            {
                TeleportPlayer(characterController);
            }
        }

        private void TeleportPlayer(NetworkCharacterController controller)
        {
            // Teleport the character controller to the destination with padding
            controller.Teleport(_destinationPoint.position + _padding);

            // Optionally reset velocity if we want a clean state (e.g., falling into water)
            if (_resetVelocity)
            {
                controller.Velocity = Vector3.zero;
            }
            
            AppLog.Info($"[TeleportVolume] Teleported {controller.gameObject.name} to {_destinationPoint.name}");
        }
        
        private void OnDrawGizmos()
        {
            if (_destinationPoint != null)
            {
                Gizmos.color = Color.green;
                Vector3 targetPos = _destinationPoint.position + _padding;
                Gizmos.DrawWireSphere(targetPos, 0.5f);
                Gizmos.DrawLine(transform.position, targetPos);
            }
        }
    }
}
