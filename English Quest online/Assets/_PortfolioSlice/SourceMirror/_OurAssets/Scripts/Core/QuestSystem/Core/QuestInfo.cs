using System;
using System.Collections.Generic;
using EnglishKingdom.QuestSystem;
using EnglishKingdom.SaveSystem.Data;
using UnityEngine;
using UnityServiceLocator;

[AddComponentMenu(QuestSystemComponentMenuPaths.Root + "/Quest Info")]
public class QuestInfo : MonoBehaviour
{
    [field: SerializeField] public string id { get; set; }

    [Header("General")]
    public string displayName;
    [TextArea] public string description;

    [Header("Requirements")]
    public List<QuestRequirement> requirements;

    [Header("Steps")]
    public List<QuestStep> questSteps;

    [Header("Repeatability")]
    public bool isRepeatable;

    [Header("Turn-In")]
    [Tooltip("When true, the quest waits in CAN_FINISH until an NPC calls FinishQuest.")]
    public bool waitForNpcTurnIn;

    [Header("Runtime State")]
    public QuestState state { get; private set; }
    public int currentStepIndex { get; private set; }
    private QuestStepState[] stepStates = new QuestStepState[0];
    private ObjectiveProgress[] objectiveProgress = new ObjectiveProgress[0];
    private bool _isInitialized;

    public int StepCount => ResolveStepCount();

    public void SetState(QuestState newState) => state = newState;

    public bool UsesObjectives()
    {
        return TryGetDefinition(out QuestDefinitionSO definition) &&
               definition.objectives != null &&
               definition.objectives.Count > 0;
    }

    public bool UsesLegacySteps() => !UsesObjectives() && questSteps != null && questSteps.Count > 0;

    public bool TryGetDefinition(out QuestDefinitionSO definition)
    {
        definition = null;
        if (TryGetComponent(out QuestDefinitionLink link) && link.Definition != null)
        {
            definition = link.Definition;
            return true;
        }

        if (ServiceLocator.For(this) is ServiceLocator locator &&
            locator.TryGet(out IQuestAvailabilityService availability))
            return availability.TryGetDefinition(this, out definition);

        return false;
    }

    public bool TryGetObjectiveDefinition(int stepIndex, out QuestObjectiveDefinition definition)
    {
        definition = null;
        if (!TryGetDefinition(out QuestDefinitionSO questDefinition) ||
            questDefinition.objectives == null ||
            stepIndex < 0 ||
            stepIndex >= questDefinition.objectives.Count)
            return false;

        definition = questDefinition.objectives[stepIndex];
        return definition != null;
    }

    public void ApplyAuthoringMetadata(
        string newId,
        string newDisplayName,
        string newDescription,
        int levelRequired,
        bool waitForTurnIn,
        string prerequisiteQuestId)
    {
        id = newId;
        displayName = newDisplayName;
        description = newDescription;
        waitForNpcTurnIn = waitForTurnIn;

        if (levelRequired > 0 || !string.IsNullOrEmpty(prerequisiteQuestId))
        {
            if (requirements == null)
                requirements = new List<QuestRequirement>();

            if (requirements.Count == 0)
                requirements.Add(new QuestRequirement());

            QuestRequirement requirement = requirements[0];
            requirement.minPlayerLevel = levelRequired;

            if (!string.IsNullOrEmpty(prerequisiteQuestId))
            {
                if (requirement.requiredQuestIds == null)
                    requirement.requiredQuestIds = new List<string>();
                requirement.requiredQuestIds.Clear();
                requirement.requiredQuestIds.Add(prerequisiteQuestId);
            }
            else if (requirement.requiredQuestIds != null)
            {
                requirement.requiredQuestIds.Clear();
            }
        }
    }

    public void InitializeQuest()
    {
        int stepCount = ResolveStepCount();
        currentStepIndex = 0;
        stepStates = new QuestStepState[stepCount];
        objectiveProgress = new ObjectiveProgress[stepCount];

        for (int i = 0; i < stepCount; i++)
        {
            stepStates[i] = new QuestStepState();
            int target = ResolveObjectiveTarget(i);
            objectiveProgress[i] = ObjectiveProgress.NotStarted(target);
        }

        state = CheckRequirements() ? QuestState.CAN_START : QuestState.REQUIREMENTS_NOT_MET;
        _isInitialized = true;
    }

    public bool CheckRequirements()
    {
        IQuestService questService = null;
        IPlayerLevelProvider levelProvider = null;
        if (ServiceLocator.For(this) is ServiceLocator locator)
        {
            locator.TryGet(out questService);
            locator.TryGet(out levelProvider);
        }

        if (requirements == null || requirements.Count == 0)
            return true;

        foreach (QuestRequirement req in requirements)
        {
            if (!req.IsMet(questService, levelProvider))
                return false;
        }

        return true;
    }

    public void MoveToNextStep()
    {
        currentStepIndex++;
    }

    public void MoveToPreviousStep()
    {
        if (currentStepIndex > 0) currentStepIndex--;
    }

    public bool CurrentStepExists()
    {
        return currentStepIndex < StepCount;
    }

    public ObjectiveProgress GetObjectiveProgress(int stepIndex)
    {
        if (!_isInitialized || stepIndex < 0 || stepIndex >= objectiveProgress.Length)
            return ObjectiveProgress.NotStarted(1);

        return objectiveProgress[stepIndex];
    }

    public void SetObjectiveProgress(int stepIndex, ObjectiveProgress progress)
    {
        if (!_isInitialized || stepIndex < 0 || stepIndex >= objectiveProgress.Length)
            return;

        objectiveProgress[stepIndex] = progress;
    }

    public void StoreQuestStepState(QuestStepState questStepState, int stepIndex)
    {
        if (!_isInitialized) { AppLog.Error($"[QuestInfo] Quest '{id}' accessed before InitializeQuest() was called."); return; }
        if (stepIndex < stepStates.Length)
        {
            stepStates[stepIndex].state = questStepState.state;
            stepStates[stepIndex].status = questStepState.status;
            stepStates[stepIndex].stepStatus = questStepState.stepStatus;

            if (stepIndex < objectiveProgress.Length)
            {
                int target = objectiveProgress[stepIndex].Target > 0 ? objectiveProgress[stepIndex].Target : 1;
                objectiveProgress[stepIndex] = questStepState.stepStatus == QuestStepStatus.COMPLETED
                    ? ObjectiveProgress.Completed(target)
                    : new ObjectiveProgress
                    {
                        Current = 0,
                        Target = target,
                        Status = questStepState.stepStatus
                    };
            }
        }
        else
        {
            AppLog.Warning("Tried to access quest step state, but stepIndex was out of range: Quest Id = " + id + ", Step Index = " + stepIndex);
        }
    }

    public string GetStepState(int stepIndex)
    {
        if (!_isInitialized) { AppLog.Error($"[QuestInfo] Quest '{id}' accessed before InitializeQuest() was called."); return ""; }
        if (stepIndex < stepStates.Length)
            return stepStates[stepIndex].state;

        return "";
    }

    public QuestStepStatus GetStepStatus(int stepIndex)
    {
        if (!_isInitialized) { AppLog.Error($"[QuestInfo] Quest '{id}' accessed before InitializeQuest() was called."); return QuestStepStatus.NOT_STARTED; }
        if (stepIndex < stepStates.Length)
            return stepStates[stepIndex].stepStatus;

        return QuestStepStatus.NOT_STARTED;
    }

    public QuestProgressEntry CreateProgressEntry()
    {
        var entry = new QuestProgressEntry
        {
            State = (int)state,
            CurrentStepIndex = currentStepIndex,
            Steps = new List<StepProgressEntry>()
        };

        for (int i = 0; i < objectiveProgress.Length; i++)
        {
            ObjectiveProgress p = objectiveProgress[i];
            entry.Steps.Add(new StepProgressEntry
            {
                Current = p.Current,
                Target = p.Target,
                Status = (int)p.Status
            });
        }

        return entry;
    }

    public void ApplyProgressEntry(QuestProgressEntry entry)
    {
        if (entry == null)
            return;

        if (!_isInitialized)
            InitializeQuest();

        SetState((QuestState)entry.State);
        currentStepIndex = entry.CurrentStepIndex;

        if (entry.Steps == null)
            return;

        for (int i = 0; i < entry.Steps.Count && i < objectiveProgress.Length; i++)
        {
            StepProgressEntry saved = entry.Steps[i];
            objectiveProgress[i] = new ObjectiveProgress
            {
                Current = saved.Current,
                Target = saved.Target > 0 ? saved.Target : 1,
                Status = (QuestStepStatus)saved.Status
            };

            if (i < stepStates.Length)
            {
                stepStates[i].stepStatus = (QuestStepStatus)saved.Status;
                stepStates[i].status = saved.Status.ToString();
            }
        }
    }

    private int ResolveStepCount()
    {
        if (UsesObjectives() && TryGetDefinition(out QuestDefinitionSO definition))
            return definition.objectives.Count;

        return questSteps != null ? questSteps.Count : 0;
    }

    private int ResolveObjectiveTarget(int stepIndex)
    {
        if (TryGetObjectiveDefinition(stepIndex, out QuestObjectiveDefinition definition))
            return definition.GetTargetCount();

        return 1;
    }
}
