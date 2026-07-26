using UnityEngine;
using UnityServiceLocator;

public class NpcDialogueTrigger : MonoBehaviour, IInteractable
{
    [Header("Dialogue Configuration")]
    public DialogueNode startingNode;

    [Header("Interaction Settings")]
    [SerializeField] private string _interactionPrompt = "Talk";
    public string InteractionPrompt => _interactionPrompt;
    public bool CanInteract => this.enabled && startingNode != null;
    public bool Interact(PlayerInteraction interactor)
    {
        if (!CanInteract) return false;
        StartDialogue(interactor != null ? interactor.transform : null);
        return true;
    }

    public void Interact()
    {
        StartDialogue(null);
    }

    private void StartDialogue(Transform localPlayer)
    {
        if (startingNode != null && ServiceLocator.For(this).TryGet<IDialogueService>(out var dialogue))
        {
            dialogue.StartDialogue(startingNode, transform, localPlayer);
        }
        else
        {
            AppLog.Warning("Dialogue Manager or Starting Node is missing!");
        }
    }
}
