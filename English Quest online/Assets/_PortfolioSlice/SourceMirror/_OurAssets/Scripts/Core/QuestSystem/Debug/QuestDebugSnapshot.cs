using EnglishQuest.QuestSystem;
using UnityEngine;
using UnityServiceLocator;

public enum QuestDebugMode
{
    Empty = 0,
    Objectives = 1,
    Legacy = 2
}

/// <summary>
/// Dual-mode (objectives + legacy steps) snapshot for local quest debugging UIs.
/// </summary>
public readonly struct QuestDebugSnapshot
{
    public readonly string Id;
    public readonly string DisplayName;
    public readonly QuestState State;
    public readonly QuestDebugMode Mode;
    public readonly int StepIndex;
    public readonly int StepCount;
    public readonly string ObjectiveText;
    public readonly string ProgressText;
    public readonly string ObjectiveTypeName;
    public readonly string TargetId;
    public readonly string StepTypeName;
    public readonly bool StepFinished;
    public readonly bool StepActive;
    public readonly QuestStepStatus StepStatus;

    public QuestDebugSnapshot(
        string id,
        string displayName,
        QuestState state,
        QuestDebugMode mode,
        int stepIndex,
        int stepCount,
        string objectiveText,
        string progressText,
        string objectiveTypeName,
        string targetId,
        string stepTypeName,
        bool stepFinished,
        bool stepActive,
        QuestStepStatus stepStatus)
    {
        Id = id;
        DisplayName = displayName;
        State = state;
        Mode = mode;
        StepIndex = stepIndex;
        StepCount = stepCount;
        ObjectiveText = objectiveText ?? string.Empty;
        ProgressText = progressText ?? string.Empty;
        ObjectiveTypeName = objectiveTypeName ?? string.Empty;
        TargetId = targetId ?? string.Empty;
        StepTypeName = stepTypeName ?? string.Empty;
        StepFinished = stepFinished;
        StepActive = stepActive;
        StepStatus = stepStatus;
    }

    public string ModeLabel => Mode switch
    {
        QuestDebugMode.Objectives => "Obj",
        QuestDebugMode.Legacy => "Legacy",
        _ => "—"
    };

    public string StepLabel
    {
        get
        {
            if (StepCount <= 0)
                return "—";
            return $"{StepIndex}/{Mathf.Max(0, StepCount - 1)}";
        }
    }

    public string Summary
    {
        get
        {
            if (!string.IsNullOrEmpty(ObjectiveText))
                return ObjectiveText;
            if (!string.IsNullOrEmpty(StepTypeName))
                return StepTypeName;
            if (!string.IsNullOrEmpty(ObjectiveTypeName))
            {
                return string.IsNullOrEmpty(TargetId)
                    ? ObjectiveTypeName
                    : $"{ObjectiveTypeName}:{TargetId}";
            }

            return "—";
        }
    }
}

public static class QuestDebugSnapshotBuilder
{
    public static QuestDebugSnapshot Build(
        QuestInfo quest,
        IQuestService questService = null,
        IQuestWorldResolver worldResolver = null)
    {
        if (quest == null)
        {
            return new QuestDebugSnapshot(
                string.Empty,
                string.Empty,
                QuestState.REQUIREMENTS_NOT_MET,
                QuestDebugMode.Empty,
                0,
                0,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                false,
                false,
                QuestStepStatus.NOT_STARTED);
        }

        questService ??= ResolveQuestService(quest);
        worldResolver ??= ResolveWorldResolver(quest);

        QuestDebugMode mode = QuestDebugMode.Empty;
        if (quest.UsesObjectives())
            mode = QuestDebugMode.Objectives;
        else if (quest.UsesLegacySteps())
            mode = QuestDebugMode.Legacy;

        int stepIndex = quest.currentStepIndex;
        int stepCount = quest.StepCount;
        string objectiveText = ActiveQuestDisplayHelper.BuildObjectiveText(quest, questService, worldResolver);
        string progressText = BuildProgressText(quest, questService);

        string objectiveTypeName = string.Empty;
        string targetId = string.Empty;
        string stepTypeName = string.Empty;
        bool stepFinished = false;
        bool stepActive = false;
        QuestStepStatus stepStatus = QuestStepStatus.NOT_STARTED;

        if (mode == QuestDebugMode.Objectives &&
            quest.TryGetObjectiveDefinition(stepIndex, out QuestObjectiveDefinition definition))
        {
            objectiveTypeName = definition.type.ToString();
            targetId = definition.targetId ?? string.Empty;
            if (questService != null)
            {
                ObjectiveProgress progress = questService.GetObjectiveProgress(quest, stepIndex);
                stepStatus = progress.Status;
            }
            else
            {
                stepStatus = quest.GetObjectiveProgress(stepIndex).Status;
            }
        }
        else if (mode == QuestDebugMode.Legacy &&
                 quest.questSteps != null &&
                 stepIndex >= 0 &&
                 stepIndex < quest.questSteps.Count)
        {
            QuestStep step = quest.questSteps[stepIndex];
            if (step != null)
            {
                stepTypeName = step.GetType().Name;
                stepFinished = step.IsFinished;
                stepActive = step.StepIsActive;
            }

            stepStatus = quest.GetStepStatus(stepIndex);
        }

        return new QuestDebugSnapshot(
            quest.id,
            string.IsNullOrEmpty(quest.displayName) ? quest.id : quest.displayName,
            quest.state,
            mode,
            stepIndex,
            stepCount,
            objectiveText,
            progressText,
            objectiveTypeName,
            targetId,
            stepTypeName,
            stepFinished,
            stepActive,
            stepStatus);
    }

    public static string FormatLine(QuestDebugSnapshot snapshot)
    {
        string progress = string.IsNullOrEmpty(snapshot.ProgressText) ? "—" : snapshot.ProgressText;
        return
            $"{snapshot.DisplayName} [{snapshot.Id}] {snapshot.ModeLabel} {snapshot.State} " +
            $"step {snapshot.StepLabel} prog {progress} | {snapshot.Summary}";
    }

    public static string FormatLine(QuestInfo quest, IQuestService questService = null, IQuestWorldResolver worldResolver = null) =>
        FormatLine(Build(quest, questService, worldResolver));

    static string BuildProgressText(QuestInfo quest, IQuestService questService)
    {
        if (quest == null || quest.state != QuestState.IN_PROGRESS || !quest.CurrentStepExists())
            return string.Empty;

        ObjectiveProgress progress = questService != null
            ? questService.GetObjectiveProgress(quest, quest.currentStepIndex)
            : quest.GetObjectiveProgress(quest.currentStepIndex);

        if (quest.UsesObjectives())
            return $"{progress.Current}/{Mathf.Max(1, progress.Target)}";

        // Legacy: show step status as compact progress hint when no numeric target.
        if (progress.Target > 1)
            return $"{progress.Current}/{progress.Target}";

        return progress.Status.ToString();
    }

    static IQuestService ResolveQuestService(QuestInfo quest)
    {
        if (QuestManager.HasInstance)
            return QuestManager.Instance;

        if (quest != null &&
            ServiceLocator.For(quest) != null &&
            ServiceLocator.For(quest).TryGet(out IQuestService service))
            return service;

        return null;
    }

    static IQuestWorldResolver ResolveWorldResolver(QuestInfo quest)
    {
        if (quest == null)
            return null;

        if (ServiceLocator.For(quest) != null &&
            ServiceLocator.For(quest).TryGet(out IQuestWorldResolver resolver))
            return resolver;

        if (QuestManager.HasInstance &&
            ServiceLocator.For(QuestManager.Instance) != null &&
            ServiceLocator.For(QuestManager.Instance).TryGet(out IQuestWorldResolver fromManager))
            return fromManager;

        return null;
    }
}

