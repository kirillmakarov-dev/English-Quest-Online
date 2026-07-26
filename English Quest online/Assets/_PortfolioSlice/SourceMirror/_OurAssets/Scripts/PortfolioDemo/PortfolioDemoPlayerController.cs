using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace EnglishKingdom.PortfolioDemo
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInteraction))]
    public sealed class PortfolioDemoPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float turnSpeed = 12f;

        [Header("Interaction")]
        [SerializeField] private float interactionRadius = 2.4f;
        [SerializeField] private Key interactionKey = Key.E;
        [SerializeField] private PortfolioDemoHud hud;

        [Header("Camera")]
        [SerializeField] private Camera followCamera;
        [SerializeField] private Vector3 cameraOffset = new(0f, 8f, -9f);
        [SerializeField] private Vector3 cameraLookOffset = new(0f, 1f, 0f);

        private readonly Collider[] nearbyColliders = new Collider[32];
        private readonly HashSet<IInteractable> candidates = new();
        private CharacterController characterController;
        private PlayerInteraction playerInteraction;
        private bool movementLocked;
        private bool interactionLocked;

        public void SetMovementLocked(bool value) => movementLocked = value;
        public void SetInteractionLocked(bool value) => interactionLocked = value;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerInteraction = GetComponent<PlayerInteraction>();
        }

        private void Update()
        {
            if (!movementLocked)
                UpdateMovement();

            UpdateInteractionTarget();

            if (!interactionLocked
                && Keyboard.current != null
                && Keyboard.current[interactionKey].wasPressedThisFrame)
                TryInteract();
        }

        private void LateUpdate()
        {
            if (followCamera == null)
                return;

            followCamera.transform.position = transform.position + cameraOffset;
            followCamera.transform.LookAt(transform.position + cameraLookOffset);
        }

        private void UpdateMovement()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            float horizontal = ReadAxis(keyboard.aKey, keyboard.dKey);
            float vertical = ReadAxis(keyboard.sKey, keyboard.wKey);
            Vector3 input = new(horizontal, 0f, vertical);

            if (input.sqrMagnitude > 1f)
                input.Normalize();

            Vector3 movement = input;
            if (followCamera != null)
            {
                Vector3 forward = followCamera.transform.forward;
                Vector3 right = followCamera.transform.right;
                forward.y = 0f;
                right.y = 0f;
                movement = forward.normalized * input.z + right.normalized * input.x;
            }

            characterController.SimpleMove(movement * moveSpeed);

            if (movement.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movement);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime);
            }
        }

        private static float ReadAxis(KeyControl negative, KeyControl positive)
        {
            return (positive.isPressed ? 1f : 0f) - (negative.isPressed ? 1f : 0f);
        }

        private void UpdateInteractionTarget()
        {
            if (interactionLocked)
            {
                SetCurrentTarget(null);
                return;
            }

            candidates.Clear();
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                interactionRadius,
                nearbyColliders,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = nearbyColliders[i];
                if (hit == null || hit.transform.IsChildOf(transform))
                    continue;

                CollectInteractables(hit.GetComponents<MonoBehaviour>());
                CollectInteractables(hit.GetComponentsInParent<MonoBehaviour>(true));
            }

            IInteractable closest = null;
            float closestDistance = float.MaxValue;
            foreach (IInteractable candidate in candidates)
            {
                if (!IsAvailable(candidate))
                    continue;

                Component component = candidate as Component;
                float distance = (component.transform.position - transform.position).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = candidate;
                }
            }

            SetCurrentTarget(closest);
        }

        private void CollectInteractables(MonoBehaviour[] behaviours)
        {
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IInteractable interactable)
                    candidates.Add(interactable);
            }
        }

        private void SetCurrentTarget(IInteractable target)
        {
            playerInteraction.SetCurrentInteractable(target);
            hud?.SetInteractionPrompt(
                target != null ? $"E  {target.InteractionPrompt}" : string.Empty);
        }

        private void TryInteract()
        {
            IInteractable target = playerInteraction.CurrentInteractable;
            if (!IsAvailable(target))
                return;

            target.Interact(playerInteraction);
        }

        private static bool IsAvailable(IInteractable interactable)
        {
            if (interactable == null || !interactable.CanInteract)
                return false;

            return interactable is not Object unityObject || unityObject != null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.85f, 0.65f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
