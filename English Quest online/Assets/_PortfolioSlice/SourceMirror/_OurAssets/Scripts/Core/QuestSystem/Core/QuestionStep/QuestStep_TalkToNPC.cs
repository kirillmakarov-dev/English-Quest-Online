using UnityEngine;
using UnityServiceLocator;

[AddComponentMenu(QuestSystemComponentMenuPaths.Steps + "/Talk To NPC")]
public class QuestStep_TalkToNPC : QuestStep, IInteractable
{
    [SerializeField] private DialogueNode dialogueToTrigger;
    [SerializeField] private DialogueNode dialogueAfterFinished;

    public string InteractionPrompt => "Talk to NPC";

    // Talkable whenever the step is loaded — action guard is inside Interact()
    public bool CanInteract => stepIsActive;

    public override void InitializeStep()
    {
        base.InitializeStep();
    }

    public bool Interact(PlayerInteraction interactor)
    {
        if (!CanInteract) return false;
        if (!ServiceLocator.For(this).TryGet<IDialogueService>(out var dialogue)) return false;

        var localPlayer = interactor != null ? interactor.transform : null;

        if (!isFinished)
        {
            dialogue.StartDialogue(dialogueToTrigger, transform, localPlayer);
            FinishStep();
            return true;
        }

        // Step already done — play a repeat dialogue without re-triggering the step action
        if (dialogueAfterFinished != null)
            dialogue.StartDialogue(dialogueAfterFinished, transform, localPlayer);
        else if (dialogueToTrigger != null)
            dialogue.StartDialogue(dialogueToTrigger, transform, localPlayer);

        return false;
    }
}
