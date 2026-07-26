using Fusion;
using UnityEngine;

public class InteractionZoneTrigger : MonoBehaviour
{
    [SerializeField] private PlayerInteraction playerInteractionScript;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (playerInteractionScript == null) 
        {
             playerInteractionScript = GetComponentInParent<PlayerInteraction>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        IInteractable interactableItem = ResolveInteractable(other);
        if (interactableItem != null)
        {
            playerInteractionScript.SetCurrentInteractable(interactableItem);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IInteractable interactableItem = ResolveInteractable(other);
        if (interactableItem != null)
        {
            playerInteractionScript.SetCurrentInteractable(null);
        }
    }

    // Resolves the best IInteractable on a collider's object.
    // Prefers NetworkBehaviour implementors so that a networked component takes
    // priority over a local one when both live on the same GameObject.
    private IInteractable ResolveInteractable(Collider other)
    {
        MonoBehaviour[] candidates = other.GetComponents<MonoBehaviour>();
        if (candidates == null || candidates.Length == 0)
            candidates = other.GetComponentsInParent<MonoBehaviour>();

        foreach (MonoBehaviour candidate in candidates)
            if (candidate is NetworkBehaviour interactable && interactable is IInteractable canInteract && canInteract.CanInteract)
                return canInteract;

        foreach (MonoBehaviour candidate in candidates)
            if (candidate is IInteractable interactable && interactable.CanInteract)
                return interactable;

        foreach (MonoBehaviour candidate in candidates)
            if (candidate is IInteractable interactable && candidate is NetworkBehaviour)
                return interactable;

        foreach (MonoBehaviour candidate in candidates)
            if (candidate is IInteractable interactable)
                return interactable;

        return null;
    }
}
