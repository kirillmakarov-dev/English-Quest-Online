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
        IInteractable[] candidates = other.GetComponents<IInteractable>();
        if (candidates.Length == 0)
            candidates = other.GetComponentsInParent<IInteractable>();

        foreach (IInteractable candidate in candidates)
            if (candidate is NetworkBehaviour && candidate.CanInteract) return candidate;

        foreach (IInteractable candidate in candidates)
            if (candidate.CanInteract) return candidate;

        foreach (IInteractable candidate in candidates)
            if (candidate is NetworkBehaviour) return candidate;

        return candidates.Length > 0 ? candidates[0] : null;
    }
}
