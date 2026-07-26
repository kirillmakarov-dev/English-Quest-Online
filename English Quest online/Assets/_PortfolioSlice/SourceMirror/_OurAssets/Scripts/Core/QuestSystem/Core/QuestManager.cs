using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EnglishKingdom.QuestSystem;
using EnglishKingdom.RewardSystem;
using UnityEngine;
using UnityServiceLocator;

[DefaultExecutionOrder(-50)]
[AddComponentMenu(QuestSystemComponentMenuPaths.Root + "/Quest Manager")]
public class QuestManager : StaticInstance<QuestManager>, IQuestService
{
    /// <summary>
    /// When true, logs happy-path lifecycle Info (register / state / start / step / complete).
    /// Toggled from Quest System → Live Debug. Warnings and Guider/Debug action logs always emit.
    /// </summary>
    public static bool VerboseLogging { get; set; }

    [Header("Config")]
    [SerializeField] private bool loadQuestState = true;
    [SerializeField] private List<QuestInfo> allQuestInfos;

    public bool LoadQuestStateEnabled => loadQuestState;

    [Header("Objective System")]
    [SerializeField] private QuestProgressPersistence questProgressPersistence;
    [SerializeField] private QuestObjectiveDirector questObjectiveDirector;

    public event Action<QuestInfo> OnQuestStarted;
    public event Action<QuestInfo> OnQuestUpdated;
    public event Action<QuestInfo> OnQuestCompleted;
    public event Action<QuestInfo> OnQuestStateChanged;
    public event Action<QuestObjectiveProgressEvent> OnObjectiveProgressChanged;

    private readonly Dictionary<string, QuestStep> activeSteps = new Dictionary<string, QuestStep>();
    private QuestProgressPersistence _persistence;
    private QuestMiniGameBinder _miniGameBinder;

    public IReadOnlyList<QuestInfo> AllQuests => allQuestInfos;

    protected override void Awake()
    {
        base.Awake();
        ServiceLocator.For(this).Register<IQuestService>(this);
        EnsureObjectiveComponents();
        InitializeQuests();
    }

    protected override void OnDestroy()
    {
        ServiceLocator.DeregisterFor<IQuestService>(this);
        base.OnDestroy();
    }

    private void EnsureObjectiveComponents()
    {
        if (questProgressPersistence == null)
            questProgressPersistence = GetComponent<QuestProgressPersistence>();
        if (questProgressPersistence == null)
            questProgressPersistence = gameObject.AddComponent<QuestProgressPersistence>();

        if (questObjectiveDirector == null)
            questObjectiveDirector = GetComponent<QuestObjectiveDirector>();
        if (questObjectiveDirector == null)
            questObjectiveDirector = gameObject.AddComponent<QuestObjectiveDirector>();

        if (GetComponent<QuestObjectiveEventBus>() == null)
            gameObject.AddComponent<QuestObjectiveEventBus>();

        // Register world targets before default-order marker OnEnable (see DefaultExecutionOrder).
        if (GetComponent<QuestWorldTargetRegistrar>() == null)
            gameObject.AddComponent<QuestWorldTargetRegistrar>();

        if (GetComponent<PlayerLevelProvider>() == null)
            gameObject.AddComponent<PlayerLevelProvider>();

        if (_miniGameBinder == null)
            _miniGameBinder = new QuestMiniGameBinder();
        ServiceLocator.For(this).Register<IQuestMiniGameBinder>(_miniGameBinder);

        _persistence = questProgressPersistence;
    }

    public void RefreshMiniGameBindings()
    {
        _miniGameBinder?.Refresh(this);
    }

    public void BeginProgressLoad(bool loadQuestState)
    {
        if (_persistence != null)
            _persistence.LoadAndApplyAsync(this, loadQuestState).Forget();
    }

    public void RegisterQuestStep(QuestStep step, QuestInfo questInfo, int stepIndex)
    {
        if (questInfo != null && questInfo.UsesObjectives())
            return;

        string key = GetStepKey(questInfo, stepIndex);
        if (activeSteps.ContainsKey(key))
            return;

        activeSteps.Add(key, step);
        step.OnStepFinished += (_, finalState) => CompleteQuestStep(questInfo, stepIndex, finalState);
    }

    public void CompleteObjectiveStep(QuestInfo questInfo, int stepIndex, string finalState = "")
    {
        if (questInfo == null || !questInfo.UsesObjectives())
            return;

        if (questInfo.state != QuestState.IN_PROGRESS)
            return;

        if (stepIndex != questInfo.currentStepIndex)
            return;

        CompleteQuestStep(questInfo, stepIndex, finalState);
    }

    public void ReportObjectiveProgress(QuestInfo questInfo, int stepIndex, int current, int target)
    {
        if (questInfo == null || !questInfo.UsesObjectives())
            return;

        var progress = new ObjectiveProgress
        {
            Current = current,
            Target = target > 0 ? target : 1,
            Status = current >= target ? QuestStepStatus.COMPLETED : QuestStepStatus.IN_PROGRESS
        };

        questInfo.SetObjectiveProgress(stepIndex, progress);
        OnObjectiveProgressChanged?.Invoke(new QuestObjectiveProgressEvent(questInfo, stepIndex, progress));
        _persistence?.ScheduleSave();
    }

    public ObjectiveProgress GetObjectiveProgress(QuestInfo questInfo, int stepIndex)
    {
        if (questInfo == null)
            return ObjectiveProgress.NotStarted(1);

        return questInfo.GetObjectiveProgress(stepIndex);
    }

    private void CompleteQuestStep(QuestInfo questInfo, int stepIndex, string finalState)
    {
        TryGrantObjectiveStepReward(questInfo, stepIndex);

        ObjectiveProgress progress = questInfo.GetObjectiveProgress(stepIndex);
        int target = progress.Target > 0 ? progress.Target : 1;
        questInfo.SetObjectiveProgress(stepIndex, ObjectiveProgress.Completed(target));
        questInfo.StoreQuestStepState(new QuestStepState(finalState, QuestStepStatus.COMPLETED), stepIndex);
        LogVerbose($"Quest '{questInfo.id}' step {stepIndex} completed with state '{finalState}'.");
        _persistence?.ScheduleSave();
        AdvanceQuest(questInfo);
    }

    private void TryGrantObjectiveStepReward(QuestInfo questInfo, int stepIndex)
    {
        if (questInfo == null || !questInfo.UsesObjectives())
            return;

        if (!questInfo.TryGetObjectiveDefinition(stepIndex, out QuestObjectiveDefinition definition) ||
            definition.stepReward == null)
            return;

        if (!ServiceLocator.For(this).TryGet<IRewardService>(out IRewardService rewardService))
        {
            AppLog.Warning($"[QuestManager] IRewardService not found; skipped step reward for quest '{questInfo.id}' step {stepIndex}.");
            return;
        }

        rewardService.Grant(definition.stepReward.ToBundle(), showPopup: definition.showStepRewardPopup);
        LogVerbose($"[QuestManager] Granted step reward '{definition.stepReward.name}' for quest '{questInfo.id}' step {stepIndex}.");
    }

    private string GetStepKey(QuestInfo questInfo, int stepIndex)
    {
        return $"{questInfo.id}_step_{stepIndex}";
    }

    private void UpdateQuestStepVisuals(QuestInfo quest)
    {
        if (quest.UsesObjectives())
        {
            RefreshMiniGameBindings();
            OnQuestUpdated?.Invoke(quest);
            return;
        }

        string prevKey = GetStepKey(quest, quest.currentStepIndex - 1);
        if (activeSteps.TryGetValue(prevKey, out QuestStep prevStep))
        {
            _ = prevStep;
        }

        string currentKey = GetStepKey(quest, quest.currentStepIndex);
        if (activeSteps.TryGetValue(currentKey, out QuestStep currentStep) && currentStep != null)
        {
            currentStep.InitializeQuestStep(
                quest,
                quest.currentStepIndex,
                quest.GetStepState(quest.currentStepIndex),
                quest.GetStepStatus(quest.currentStepIndex));
        }
    }

    private void ChangeQuestState(QuestInfo questInfo, QuestState state)
    {
        if (questInfo == null)
            return;

        questInfo.SetState(state);
        OnQuestStateChanged?.Invoke(questInfo);
        LogVerbose($"Quest '{questInfo.id}' state changed to {state}");
        _persistence?.ScheduleSave();
    }

    public void StartQuest(QuestInfo questInfo)
    {
        if (questInfo == null)
            return;

        if (questInfo.state == QuestState.CAN_START)
        {
            if (!questInfo.CheckRequirements())
            {
                ChangeQuestState(questInfo, QuestState.REQUIREMENTS_NOT_MET);
                AppLog.Warning($"Cannot start quest {questInfo.id}. Requirements are no longer met.");
                return;
            }

            ChangeQuestState(questInfo, QuestState.IN_PROGRESS);
            UpdateQuestStepVisuals(questInfo);
            OnQuestStarted?.Invoke(questInfo);
            LogVerbose($"Quest '{questInfo.id}' started.");
        }
        else
        {
            AppLog.Warning($"Cannot start quest {questInfo.id}. State is {questInfo.state}");
        }
    }

    private void AdvanceQuest(QuestInfo questInfo)
    {
        if (questInfo == null)
            return;

        questInfo.MoveToNextStep();

        if (questInfo.CurrentStepExists())
        {
            UpdateQuestStepVisuals(questInfo);
            OnQuestUpdated?.Invoke(questInfo);
        }
        else
        {
            ChangeQuestState(questInfo, QuestState.CAN_FINISH);
            OnQuestUpdated?.Invoke(questInfo);

            if (!questInfo.waitForNpcTurnIn)
                FinishQuest(questInfo);
        }
    }

    public void RegisterQuest(QuestInfo questInfo)
    {
        if (questInfo == null)
            return;

        if (allQuestInfos == null)
            allQuestInfos = new List<QuestInfo>();

        if (allQuestInfos.Contains(questInfo))
            return;

        allQuestInfos.Add(questInfo);
        questInfo.InitializeQuest();
        RegisterLegacySteps(questInfo);
        LogVerbose($"[QuestManager] Registered quest '{questInfo.id}'.");
    }

    private void RegisterLegacySteps(QuestInfo questInfo)
    {
        if (questInfo.UsesObjectives() || questInfo.questSteps == null)
            return;

        for (int i = 0; i < questInfo.questSteps.Count; i++)
        {
            if (questInfo.questSteps[i] != null)
                RegisterQuestStep(questInfo.questSteps[i], questInfo, i);
        }
    }

    public QuestInfo GetQuestById(string questId)
    {
        if (string.IsNullOrEmpty(questId))
            return null;

        foreach (QuestInfo quest in allQuestInfos)
        {
            if (quest != null && quest.id == questId)
                return quest;
        }

        return null;
    }

    public void FinishQuest(QuestInfo questInfo)
    {
        if (questInfo == null || questInfo.state != QuestState.CAN_FINISH)
            return;

        ChangeQuestState(questInfo, QuestState.FINISHED);
        TryGrantDefinitionReward(questInfo);
        OnQuestCompleted?.Invoke(questInfo);
        ReevaluateQuestRequirements();
        LogVerbose($"Quest '{questInfo.id}' completed.");
        _persistence?.CaptureFromQuests(this);
        _persistence?.ScheduleSave();

        if (questInfo.isRepeatable)
        {
            questInfo.InitializeQuest();
            OnQuestStateChanged?.Invoke(questInfo);
        }
    }

    private void TryGrantDefinitionReward(QuestInfo questInfo)
    {
        if (questInfo == null || !questInfo.UsesObjectives())
            return;

        if (!questInfo.TryGetDefinition(out QuestDefinitionSO definition))
            return;

        if (!ServiceLocator.For(this).TryGet<IRewardService>(out IRewardService rewardService))
        {
            AppLog.Warning($"[QuestManager] IRewardService not found; skipped reward for quest '{questInfo.id}'.");
            return;
        }

        if (definition.rewardDefinition != null)
        {
            rewardService.Grant(definition.rewardDefinition.ToBundle(), showPopup: true);
            LogVerbose($"[QuestManager] Granted reward '{definition.rewardDefinition.name}' for quest '{questInfo.id}'.");
            return;
        }

        if (definition.rewards != null && definition.rewards.xp > 0)
        {
            rewardService.Grant(definition.rewards.ToRewardBundle(), showPopup: true);
            LogVerbose($"[QuestManager] Granted fallback XP reward ({definition.rewards.xp}) for quest '{questInfo.id}'.");
        }
    }

    public void ReevaluateQuestRequirements()
    {
        if (allQuestInfos == null)
            return;

        foreach (QuestInfo q in allQuestInfos)
        {
            if (q == null)
                continue;

            bool met = q.CheckRequirements();

            if (q.state == QuestState.REQUIREMENTS_NOT_MET && met)
                ChangeQuestState(q, QuestState.CAN_START);
            else if (q.state == QuestState.CAN_START && !met)
                ChangeQuestState(q, QuestState.REQUIREMENTS_NOT_MET);
        }
    }

    private void InitializeQuests()
    {
        if (allQuestInfos == null)
            allQuestInfos = new List<QuestInfo>();

        foreach (QuestInfo questInfo in allQuestInfos)
        {
            questInfo.InitializeQuest();
            RegisterLegacySteps(questInfo);
        }
    }

    public void ConfigureObjectiveRuntime(IQuestWorldResolver resolver)
    {
        if (questObjectiveDirector != null && resolver != null)
            questObjectiveDirector.Initialize(resolver, this);
    }

    public QuestInfo GetQuestByInfo(QuestInfo questInfo)
    {
        if (allQuestInfos.Contains(questInfo))
            return questInfo;
        return null;
    }

    public bool IsQuestCompleted(QuestInfo questInfo)
    {
        return questInfo != null && questInfo.state == QuestState.FINISHED;
    }

    private QuestInfo ResolveTeacherQuest(QuestInfo questInfo)
    {
        if (questInfo != null)
            return questInfo;

        foreach (QuestInfo q in allQuestInfos)
        {
            if (q != null && q.state == QuestState.IN_PROGRESS)
                return q;
        }

        return null;
    }

    public void Teacher_AdvanceCurrentStep(QuestInfo questInfo)
    {
        Guider_AdvanceCurrentStep(questInfo);
    }

    public void Teacher_GoBackCurrentStep(QuestInfo questInfo)
    {
        Guider_GoBackCurrentStep(questInfo);
    }

    public IReadOnlyList<QuestInfo> GetOpenWorldQuests()
    {
        var results = new List<QuestInfo>();
        if (allQuestInfos == null)
            return results;

        foreach (QuestInfo quest in allQuestInfos)
        {
            if (quest != null && quest.UsesObjectives())
                results.Add(quest);
        }

        return results;
    }

    public void Guider_ForceStartQuest(QuestInfo questInfo)
    {
        if (questInfo == null)
            return;

        if (questInfo.state == QuestState.IN_PROGRESS || questInfo.state == QuestState.FINISHED)
        {
            AppLog.Warning($"[GuiderQuest] Quest '{questInfo.id}' is already {questInfo.state}.");
            return;
        }

        AppLog.Info($"[GuiderQuest] Force-starting quest '{questInfo.id}'.");
        ChangeQuestState(questInfo, QuestState.IN_PROGRESS);
        UpdateQuestStepVisuals(questInfo);
        OnQuestStarted?.Invoke(questInfo);
    }

    public void Guider_ForceCompleteQuest(QuestInfo questInfo)
    {
        if (questInfo == null)
            return;

        if (questInfo.state == QuestState.FINISHED)
        {
            AppLog.Warning($"[GuiderQuest] Quest '{questInfo.id}' is already FINISHED.");
            return;
        }

        AppLog.Info($"[GuiderQuest] Force-completing quest '{questInfo.id}'.");
        while (questInfo.CurrentStepExists())
        {
            questInfo.StoreQuestStepState(new QuestStepState("GUIDER_SKIP", QuestStepStatus.COMPLETED), questInfo.currentStepIndex);
            questInfo.MoveToNextStep();
        }

        ChangeQuestState(questInfo, QuestState.CAN_FINISH);
        FinishQuest(questInfo);
    }

    public void Guider_ResetQuest(QuestInfo questInfo)
    {
        if (questInfo == null)
            return;

        AppLog.Info($"[GuiderQuest] Resetting quest '{questInfo.id}'.");
        questInfo.InitializeQuest();
        OnQuestStateChanged?.Invoke(questInfo);
        _persistence?.ScheduleSave();
    }

    public void Guider_AdvanceCurrentStep(QuestInfo questInfo)
    {
        questInfo = ResolveTeacherQuest(questInfo);
        if (questInfo == null || questInfo.state != QuestState.IN_PROGRESS)
            return;

        if (questInfo.UsesObjectives())
        {
            CompleteObjectiveStep(questInfo, questInfo.currentStepIndex, "GUIDER_ADVANCE");
            return;
        }

        string key = GetStepKey(questInfo, questInfo.currentStepIndex);
        if (activeSteps.TryGetValue(key, out QuestStep step) && step != null && !step.IsFinished)
            step.CompleteExternally();
        else
        {
            questInfo.StoreQuestStepState(new QuestStepState("GUIDER_ADVANCE", QuestStepStatus.COMPLETED), questInfo.currentStepIndex);
            AdvanceQuest(questInfo);
        }
    }

    public void Guider_GoBackCurrentStep(QuestInfo questInfo)
    {
        questInfo = ResolveTeacherQuest(questInfo);
        if (questInfo == null || questInfo.state != QuestState.IN_PROGRESS)
            return;
        if (questInfo.currentStepIndex == 0)
            return;

        if (!questInfo.UsesObjectives())
        {
            string currentKey = GetStepKey(questInfo, questInfo.currentStepIndex);
            if (activeSteps.TryGetValue(currentKey, out QuestStep currentStep) && currentStep != null)
                currentStep.ResetStep();
        }

        questInfo.MoveToPreviousStep();
        questInfo.StoreQuestStepState(new QuestStepState("", QuestStepStatus.NOT_STARTED), questInfo.currentStepIndex);

        if (!questInfo.UsesObjectives())
        {
            string prevKey = GetStepKey(questInfo, questInfo.currentStepIndex);
            if (activeSteps.TryGetValue(prevKey, out QuestStep prevStep) && prevStep != null)
                prevStep.ResetStep();
        }
        else
        {
            int target = questInfo.GetObjectiveProgress(questInfo.currentStepIndex).Target;
            questInfo.SetObjectiveProgress(questInfo.currentStepIndex, ObjectiveProgress.NotStarted(target));
        }

        UpdateQuestStepVisuals(questInfo);
        OnQuestUpdated?.Invoke(questInfo);
        _persistence?.ScheduleSave();
    }

    public bool Guider_ApplyAction(string questId, GuiderQuestAction action)
    {
        QuestInfo quest = GetQuestById(questId);
        if (quest == null || !quest.UsesObjectives())
        {
            AppLog.Warning($"[GuiderQuest] Cannot apply action to unknown open-world quest '{questId}'.");
            return false;
        }

        switch (action)
        {
            case GuiderQuestAction.ForceStart:
                Guider_ForceStartQuest(quest);
                return true;
            case GuiderQuestAction.ForceComplete:
                Guider_ForceCompleteQuest(quest);
                return true;
            case GuiderQuestAction.Reset:
                Guider_ResetQuest(quest);
                return true;
            case GuiderQuestAction.AdvanceStep:
                Guider_AdvanceCurrentStep(quest);
                return true;
            case GuiderQuestAction.GoBackStep:
                Guider_GoBackCurrentStep(quest);
                return true;
            default:
                return false;
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void Debug_CompleteCurrentStep(QuestInfo questInfo)
    {
        if (questInfo == null || questInfo.state != QuestState.IN_PROGRESS)
        {
            AppLog.Warning($"[QuestDebug] Cannot complete step: quest '{questInfo?.id}' is not IN_PROGRESS.");
            return;
        }

        if (questInfo.UsesObjectives())
        {
            CompleteObjectiveStep(questInfo, questInfo.currentStepIndex, "DEBUG_COMPLETE");
            return;
        }

        string key = GetStepKey(questInfo, questInfo.currentStepIndex);
        if (activeSteps.TryGetValue(key, out QuestStep step) && step != null && !step.IsFinished)
        {
            AppLog.Info($"[QuestDebug] Completing step {questInfo.currentStepIndex} of quest '{questInfo.id}' via step finish.");
            step.Debug_TriggerFinish();
        }
        else
        {
            AppLog.Warning($"[QuestDebug] No active step found for step {questInfo.currentStepIndex} of quest '{questInfo.id}'. Falling back to skip.");
            questInfo.StoreQuestStepState(new QuestStepState("DEBUG_COMPLETE", QuestStepStatus.COMPLETED), questInfo.currentStepIndex);
            AdvanceQuest(questInfo);
        }
    }

    public void Debug_SkipCurrentStep(QuestInfo questInfo)
    {
        if (questInfo == null || questInfo.state != QuestState.IN_PROGRESS)
        {
            AppLog.Warning($"[QuestDebug] Cannot skip step: quest '{questInfo?.id}' is not IN_PROGRESS.");
            return;
        }

        questInfo.StoreQuestStepState(new QuestStepState("DEBUG_SKIP", QuestStepStatus.COMPLETED), questInfo.currentStepIndex);
        AppLog.Info($"[QuestDebug] Skipping step {questInfo.currentStepIndex} of quest '{questInfo.id}'.");
        AdvanceQuest(questInfo);
    }

    public void Debug_ForceStartQuest(QuestInfo questInfo) => Guider_ForceStartQuest(questInfo);

    public void Debug_ForceCompleteQuest(QuestInfo questInfo) => Guider_ForceCompleteQuest(questInfo);

    public void Debug_ResetQuest(QuestInfo questInfo) => Guider_ResetQuest(questInfo);

    public void Debug_CompleteAllQuests()
    {
        AppLog.Info("[QuestDebug] Force-completing all quests.");
        foreach (QuestInfo q in allQuestInfos)
        {
            if (q != null && q.state != QuestState.FINISHED)
                Debug_ForceCompleteQuest(q);
        }
    }

    public string FormatDebugState(QuestInfo quest) => QuestDebugSnapshotBuilder.FormatLine(quest, this);

    public void LogAllQuestDebugStates()
    {
        if (allQuestInfos == null)
            return;

        AppLog.Info("[QuestDebug] --- All quest states ---");
        foreach (QuestInfo quest in allQuestInfos)
        {
            if (quest != null)
                AppLog.Info($"[QuestDebug] {FormatDebugState(quest)}");
        }
    }
#endif

    static void LogVerbose(string message)
    {
        if (VerboseLogging)
            AppLog.Info(message);
    }
}
