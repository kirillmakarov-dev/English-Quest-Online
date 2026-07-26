using EnglishKingdom.QuestSystem;

public static class ActiveQuestDisplayHelper
{
    const string TurnInPrefix = "חזרו למסור";

    public static bool IsActiveJournalState(QuestState state) =>
        state == QuestState.IN_PROGRESS || state == QuestState.CAN_FINISH;

    public static ActiveQuestJournalEntry BuildEntry(
        QuestInfo quest,
        IQuestService questService,
        IQuestWorldResolver worldResolver = null)
    {
        if (quest == null)
            return null;

        string objectiveText = BuildObjectiveText(quest, questService, worldResolver);
        string progressText = BuildProgressText(quest, questService);

        return new ActiveQuestJournalEntry(
            quest.id,
            quest.displayName,
            quest.state,
            objectiveText,
            progressText);
    }

    public static string BuildObjectiveText(
        QuestInfo quest,
        IQuestService questService,
        IQuestWorldResolver worldResolver = null)
    {
        if (quest == null)
            return string.Empty;

        if (quest.state == QuestState.CAN_FINISH)
            return BuildTurnInText(quest, worldResolver);

        if (!quest.CurrentStepExists())
            return string.Empty;

        int stepIndex = quest.currentStepIndex;

        if (quest.UsesObjectives() && quest.TryGetObjectiveDefinition(stepIndex, out QuestObjectiveDefinition definition))
        {
            if (!string.IsNullOrEmpty(definition.displayText))
                return definition.displayText;

            string catalogName = ResolveCatalogDisplayName(definition, worldResolver);
            if (!string.IsNullOrEmpty(catalogName))
                return catalogName;
        }

        if (quest.UsesLegacySteps() &&
            quest.questSteps != null &&
            stepIndex >= 0 &&
            stepIndex < quest.questSteps.Count &&
            quest.questSteps[stepIndex] != null &&
            !string.IsNullOrEmpty(quest.questSteps[stepIndex].stepDescription))
        {
            return quest.questSteps[stepIndex].stepDescription;
        }

        return "המשיכו במשימה";
    }

    public static string BuildProgressText(QuestInfo quest, IQuestService questService)
    {
        if (quest == null || questService == null || quest.state != QuestState.IN_PROGRESS || !quest.CurrentStepExists())
            return string.Empty;

        ObjectiveProgress progress = questService.GetObjectiveProgress(quest, quest.currentStepIndex);
        if (progress.Target <= 1)
            return string.Empty;

        return $"{progress.Current}/{progress.Target}";
    }

    static string BuildTurnInText(QuestInfo quest, IQuestWorldResolver worldResolver)
    {
        if (!quest.TryGetDefinition(out QuestDefinitionSO definition) || string.IsNullOrEmpty(definition.giverNpcId))
            return TurnInPrefix;

        if (worldResolver != null &&
            worldResolver.TryGetNpc(definition.giverNpcId, out NpcCatalogEntry npc) &&
            !string.IsNullOrEmpty(npc.displayName))
        {
            return $"{TurnInPrefix} ({npc.displayName})";
        }

        return TurnInPrefix;
    }

    static string ResolveCatalogDisplayName(QuestObjectiveDefinition definition, IQuestWorldResolver worldResolver)
    {
        if (definition == null || worldResolver == null || string.IsNullOrEmpty(definition.targetId))
            return null;

        switch (definition.type)
        {
            case QuestObjectiveType.TalkToNpc:
            case QuestObjectiveType.DeliverItem:
                return worldResolver.TryGetNpc(definition.targetId, out NpcCatalogEntry npc)
                    ? npc.displayName
                    : null;
            case QuestObjectiveType.EnterArea:
                return worldResolver.TryGetArea(definition.targetId, out AreaCatalogEntry area)
                    ? area.displayName
                    : null;
            case QuestObjectiveType.Collect:
            case QuestObjectiveType.CompleteMiniGame:
                return worldResolver.TryGetInteractable(definition.targetId, out InteractableCatalogEntry interactable)
                    ? interactable.displayName
                    : null;
            default:
                return null;
        }
    }
}
