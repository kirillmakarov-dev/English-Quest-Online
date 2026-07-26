using System.Collections.Generic;
using EnglishKingdom.QuestSystem;
using UnityEngine;
using UnityServiceLocator;

[AddComponentMenu(QuestSystemComponentMenuPaths.Npc + "/NPC Quest Giver")]
public class NpcQuestGiver : MonoBehaviour, IInteractable
{
    [SerializeField] private string npcId;
    [SerializeField] private QuestSelectionUI questSelectionUI;
    [SerializeField] private DialogueNode fallbackCannotStartDialogue;
    [SerializeField] private DialogueNode fallbackInProgressDialogue;
    [SerializeField] private DialogueNode fallbackFinishedDialogue;

    private IQuestService _questService;
    private IQuestAvailabilityService _availabilityService;
    private IDialogueService _dialogueService;

    public string InteractionPrompt => "Talk";
    public string NpcId => npcId;

    public bool CanInteract => !string.IsNullOrEmpty(npcId) && enabled;

    private void OnEnable()
    {
        QuestWorldTargetRegistration.TryRegister(this, QuestObjectiveType.TalkToNpc, npcId);
        QuestWorldTargetRegistration.TryRegister(this, QuestObjectiveType.DeliverItem, npcId);
    }

    private void OnDisable()
    {
        QuestWorldTargetRegistration.TryUnregister(this, QuestObjectiveType.TalkToNpc, npcId);
        QuestWorldTargetRegistration.TryUnregister(this, QuestObjectiveType.DeliverItem, npcId);
    }

    public bool Interact(PlayerInteraction interactor)
    {
        if (!CanInteract || ResolveQuestService() == null || ResolveAvailabilityService() == null)
            return false;

        if (TryHandleActiveNpcObjective(interactor))
            return true;

        QuestObjectiveEventBus.TryPublish(this, new QuestObjectiveEvents.NpcInteracted(npcId));

        IReadOnlyList<QuestInfo> turnIns = ResolveAvailabilityService().GetReadyToTurnIn(npcId);
        if (turnIns.Count > 0)
            return HandleTurnIn(turnIns[0], interactor);

        IReadOnlyList<QuestInfo> inProgress = ResolveAvailabilityService().GetInProgress(npcId);
        if (inProgress.Count > 0)
            return HandleInProgress(inProgress[0], interactor);

        IReadOnlyList<QuestInfo> available = ResolveAvailabilityService().GetAvailableToStart(npcId);
        if (available.Count == 1)
            return StartQuestWithDialogue(available[0], interactor);

        if (available.Count > 1)
        {
            OpenQuestSelection(available, interactor);
            return true;
        }

        PlayDialogue(ResolveCannotStartDialogue() ?? fallbackCannotStartDialogue, interactor);
        return false;
    }

    /// <summary>
    /// Handles TalkToNpc / DeliverItem for any in-progress quest targeting this NPC
    /// (including non-giver NPCs). Completing the step does not turn in on the same Interact.
    /// </summary>
    private bool TryHandleActiveNpcObjective(PlayerInteraction interactor)
    {
        if (!TryFindActiveNpcObjective(npcId, out QuestInfo quest, out QuestObjectiveDefinition objective, out int stepIndex))
            return false;

        int stepBefore = quest.currentStepIndex;
        QuestState stateBefore = quest.state;

        QuestObjectiveEventBus.TryPublish(this, new QuestObjectiveEvents.NpcInteracted(npcId));

        bool completed = DidNpcObjectiveComplete(quest, stepBefore, stateBefore, stepIndex);

        if (objective.type == QuestObjectiveType.DeliverItem && !completed)
        {
            if (ResolveAvailabilityService().TryGetDefinition(quest, out QuestDefinitionSO questDefinition))
                PlayDialogue(questDefinition.inProgressDialogue ?? questDefinition.startDialogue ?? fallbackInProgressDialogue, interactor);
            else
                PlayDialogue(fallbackInProgressDialogue, interactor);
            return true;
        }

        PlayDialogue(objective.dialogue, interactor);
        return true;
    }

    private static bool DidNpcObjectiveComplete(
        QuestInfo quest,
        int stepBefore,
        QuestState stateBefore,
        int stepIndex)
    {
        if (quest == null)
            return false;

        if (quest.state != stateBefore || quest.currentStepIndex != stepBefore)
            return true;

        ObjectiveProgress progress = quest.GetObjectiveProgress(stepIndex);
        return progress.Status == QuestStepStatus.COMPLETED;
    }

    private bool TryFindActiveNpcObjective(
        string targetNpcId,
        out QuestInfo quest,
        out QuestObjectiveDefinition objective,
        out int stepIndex)
    {
        quest = null;
        objective = null;
        stepIndex = -1;

        IQuestService questService = ResolveQuestService();
        if (questService == null || string.IsNullOrEmpty(targetNpcId))
            return false;

        foreach (QuestInfo candidate in questService.AllQuests)
        {
            if (candidate == null || candidate.state != QuestState.IN_PROGRESS || !candidate.UsesObjectives())
                continue;

            if (!candidate.TryGetObjectiveDefinition(candidate.currentStepIndex, out QuestObjectiveDefinition definition))
                continue;

            if (definition.targetId != targetNpcId)
                continue;

            if (definition.type != QuestObjectiveType.TalkToNpc &&
                definition.type != QuestObjectiveType.DeliverItem)
                continue;

            quest = candidate;
            objective = definition;
            stepIndex = candidate.currentStepIndex;
            return true;
        }

        return false;
    }

    private bool HandleTurnIn(QuestInfo quest, PlayerInteraction interactor)
    {
        if (ResolveAvailabilityService().TryGetDefinition(quest, out QuestDefinitionSO definition))
            PlayDialogue(definition.turnInDialogue ?? definition.alreadyFinishedDialogue, interactor);
        else
            PlayDialogue(fallbackFinishedDialogue, interactor);

        ResolveQuestService().FinishQuest(quest);
        return true;
    }

    private bool HandleInProgress(QuestInfo quest, PlayerInteraction interactor)
    {
        if (TryGetPriorNpcObjectiveDialogue(quest, npcId, out DialogueNode afterFinished))
        {
            PlayDialogue(afterFinished, interactor);
            return false;
        }

        if (ResolveAvailabilityService().TryGetDefinition(quest, out QuestDefinitionSO definition))
            PlayDialogue(definition.inProgressDialogue ?? definition.startDialogue, interactor);
        else
            PlayDialogue(fallbackInProgressDialogue, interactor);

        return false;
    }

    private static bool TryGetPriorNpcObjectiveDialogue(
        QuestInfo quest,
        string targetNpcId,
        out DialogueNode dialogueAfterFinished)
    {
        dialogueAfterFinished = null;
        if (quest == null || !quest.TryGetDefinition(out QuestDefinitionSO definition) ||
            definition.objectives == null)
            return false;

        for (int i = 0; i < quest.currentStepIndex && i < definition.objectives.Count; i++)
        {
            QuestObjectiveDefinition objective = definition.objectives[i];
            if (objective == null || objective.targetId != targetNpcId)
                continue;

            if (objective.type != QuestObjectiveType.TalkToNpc &&
                objective.type != QuestObjectiveType.DeliverItem)
                continue;

            if (objective.dialogueAfterFinished == null)
                continue;

            dialogueAfterFinished = objective.dialogueAfterFinished;
            return true;
        }

        return false;
    }

    private DialogueNode ResolveCannotStartDialogue()
    {
        IQuestService questService = ResolveQuestService();
        if (questService == null)
            return null;

        DialogueNode anyCannotStart = null;
        foreach (QuestInfo quest in questService.AllQuests)
        {
            if (quest == null)
                continue;

            if (!ResolveAvailabilityService().TryGetDefinition(quest, out QuestDefinitionSO definition))
                continue;

            if (definition.giverNpcId != npcId || definition.cannotStartDialogue == null)
                continue;

            if (quest.state == QuestState.REQUIREMENTS_NOT_MET)
                return definition.cannotStartDialogue;

            anyCannotStart ??= definition.cannotStartDialogue;
        }

        return anyCannotStart;
    }

    private bool StartQuestWithDialogue(QuestInfo quest, PlayerInteraction interactor)
    {
        ResolveQuestService().StartQuest(quest);

        if (ResolveAvailabilityService().TryGetDefinition(quest, out QuestDefinitionSO definition))
            PlayDialogue(definition.startDialogue, interactor);

        return true;
    }

    private void OpenQuestSelection(IReadOnlyList<QuestInfo> quests, PlayerInteraction interactor)
    {
        QuestSelectionUI selectionUI = ResolveQuestSelectionUI();
        if (selectionUI == null)
        {
            AppLog.Warning("[NpcQuestGiver] Multiple quests available but QuestSelectionUI was not found.", this);
            StartQuestWithDialogue(quests[0], interactor);
            return;
        }

        selectionUI.Open(
            quests,
            interactor,
            selected =>
            {
                if (selected != null)
                    StartQuestWithDialogue(selected, interactor);
            },
            null);
    }

    private QuestSelectionUI ResolveQuestSelectionUI()
    {
        if (questSelectionUI != null)
            return questSelectionUI;

        return FindFirstObjectByType<QuestSelectionUI>(FindObjectsInactive.Include);
    }

    private IQuestService ResolveQuestService()
    {
        if (_questService == null)
            ServiceLocator.For(this).TryGet(out _questService);
        return _questService;
    }

    private IQuestAvailabilityService ResolveAvailabilityService()
    {
        if (_availabilityService == null)
            ServiceLocator.For(this).TryGet(out _availabilityService);
        return _availabilityService;
    }

    private IDialogueService ResolveDialogueService()
    {
        if (_dialogueService == null)
            ServiceLocator.For(this).TryGet(out _dialogueService);
        return _dialogueService;
    }

    private void PlayDialogue(DialogueNode node, PlayerInteraction interactor)
    {
        if (node == null || ResolveDialogueService() == null)
            return;

        Transform localPlayer = interactor != null ? interactor.transform : null;
        ResolveDialogueService().StartDialogue(node, transform, localPlayer);
    }
}
