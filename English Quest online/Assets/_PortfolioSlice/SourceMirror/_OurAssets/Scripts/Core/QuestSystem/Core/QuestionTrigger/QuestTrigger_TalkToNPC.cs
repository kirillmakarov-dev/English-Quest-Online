using Fusion;
using UnityEngine;
using UnityServiceLocator;

[AddComponentMenu(QuestSystemComponentMenuPaths.Triggers + "/Talk To NPC")]
public class QuestTrigger_TalkToNPC : QuestTrigger, IInteractable
{
    [SerializeField] private DialogueNode StartQuestDialogue;
    [SerializeField] private DialogueNode AlreadyActiveDialogue;
    [SerializeField] private DialogueNode CannotStartDialogue;
    public string InteractionPrompt => "Talk to NPC";

    // Always talkable as long as the quest is assigned — dialogue routes are determined inside Interact()
    public bool CanInteract => quest != null && enabled;

    public bool Interact(PlayerInteraction interactor)
    {
        if (!CanInteract) return false;

        if (!ServiceLocator.For(this).TryGet<IDialogueService>(out var dialogue)) return false;

        switch (quest.state)
        {
            case QuestState.CAN_START:
                TriggerStartQuest();
                PlayDialogue(dialogue, StartQuestDialogue, interactor);
                Log.Debug("Quest can be started, talking to NPC — starting quest and playing start dialogue if assigned.");
                return true;

            case QuestState.REQUIREMENTS_NOT_MET:
                Log.Debug("Quest requirements not met, talking to NPC — playing cannot start dialogue if assigned.");
                PlayDialogue(dialogue, CannotStartDialogue, interactor);
                return false;

            case QuestState.IN_PROGRESS:
                Log.Debug("Quest in progress, talking to NPC again — playing repeat dialogue if assigned.");
                PlayDialogue(dialogue, StartQuestDialogue, interactor);
                return false;
            case QuestState.FINISHED:
                Log.Debug("Quest finished, talking to NPC — playing already active dialogue if assigned.");
                PlayDialogue(dialogue, AlreadyActiveDialogue, interactor);
                return false;

            default:
                return false;
        }
    }

    private void PlayDialogue(IDialogueService dialogue, DialogueNode node, PlayerInteraction interactor)
    {
        if (node != null)
        {
            var localPlayer = interactor != null ? interactor.transform : null;
            dialogue.StartDialogue(node, transform, localPlayer);
        }
    }
}
