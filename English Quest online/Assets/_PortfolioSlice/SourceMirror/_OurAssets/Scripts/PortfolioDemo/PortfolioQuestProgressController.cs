using System.Collections.Generic;
using UnityEngine;

namespace EnglishQuest.PortfolioDemo
{
    [AddComponentMenu("English Quest/Portfolio Demo/Quest Progress Controller")]
    [DisallowMultipleComponent]
    public sealed class PortfolioQuestProgressController : MonoBehaviour
    {
        public enum ApplyMode
        {
            ResetOnly = 0,
            StartFromQuest = 1,
            CompleteEverything = 2
        }

        [Header("Auto Apply")]
        [SerializeField] private bool applySelectionOnPlay;
        [SerializeField] private bool clearSavedProgressBeforeApply = true;

        [Header("Preset")]
        [SerializeField] private ApplyMode applyMode = ApplyMode.ResetOnly;
        [SerializeField] [Min(0)] private int selectedQuestIndex;
        [SerializeField] [Min(0)] private int selectedStepIndex;
        [SerializeField] private bool completePreviousQuests = true;
        [SerializeField] private bool forceStartSelectedQuest = true;
        [SerializeField] private bool logQuestStatesAfterApply = true;

        [Header("Runtime")]
        [SerializeField] private QuestManager questManager;

        public bool ApplySelectionOnPlay => applySelectionOnPlay;
        public bool ClearSavedProgressBeforeApply => clearSavedProgressBeforeApply;
        public ApplyMode CurrentApplyMode => applyMode;
        public int SelectedQuestIndex => selectedQuestIndex;
        public int SelectedStepIndex => selectedStepIndex;
        public bool CompletePreviousQuests => completePreviousQuests;
        public bool ForceStartSelectedQuest => forceStartSelectedQuest;

        private void Awake()
        {
            ResolveQuestManager();
        }

        private void Start()
        {
            if (applySelectionOnPlay)
            {
                ApplySelectedProgressNow();
            }
        }

        public void ResetProgressNow()
        {
            if (!TryResolveManager(out QuestManager manager))
                return;

            manager.ResetAllProgress();
            manager.RefreshLevelCompletionState();

            if (logQuestStatesAfterApply)
                manager.LogAllQuestDebugStates();
        }

        public void ApplySelectedProgressNow()
        {
            if (!TryResolveManager(out QuestManager manager))
                return;

            if (clearSavedProgressBeforeApply)
            {
                manager.ResetAllProgress();
            }

            switch (applyMode)
            {
                case ApplyMode.ResetOnly:
                    manager.RefreshLevelCompletionState();
                    break;

                case ApplyMode.StartFromQuest:
                    ApplyQuestStartPreset(manager);
                    break;

                case ApplyMode.CompleteEverything:
                    manager.Debug_CompleteAllQuests();
                    break;
            }

            manager.ReevaluateQuestRequirements();
            manager.RefreshMiniGameBindings();
            manager.RefreshLevelCompletionState();

            if (logQuestStatesAfterApply)
                manager.LogAllQuestDebugStates();
        }

        public IReadOnlyList<QuestInfo> GetOrderedQuests()
        {
            if (!TryResolveManager(out QuestManager manager))
                return System.Array.Empty<QuestInfo>();

            IReadOnlyList<QuestInfo> openWorldQuests = manager.GetOpenWorldQuests();
            if (openWorldQuests != null && openWorldQuests.Count > 0)
                return openWorldQuests;

            return manager.AllQuests ?? System.Array.Empty<QuestInfo>();
        }

        public void SetSelectedQuestIndex(int index)
        {
            selectedQuestIndex = Mathf.Max(0, index);
        }

        public void SetSelectedStepIndex(int index)
        {
            selectedStepIndex = Mathf.Max(0, index);
        }

        private void ApplyQuestStartPreset(QuestManager manager)
        {
            IReadOnlyList<QuestInfo> quests = GetOrderedQuests();
            if (quests.Count == 0)
            {
                Debug.LogWarning("[PortfolioQuestProgressController] No quests found in QuestManager.");
                return;
            }

            int questIndex = Mathf.Clamp(selectedQuestIndex, 0, quests.Count - 1);

            if (completePreviousQuests)
            {
                for (int i = 0; i < questIndex; i++)
                {
                    QuestInfo previousQuest = quests[i];
                    if (previousQuest == null || previousQuest.state == QuestState.FINISHED)
                        continue;

                    manager.Debug_ForceCompleteQuest(previousQuest);
                }
            }

            QuestInfo selectedQuest = quests[questIndex];
            if (selectedQuest == null)
                return;

            if (forceStartSelectedQuest &&
                selectedQuest.state != QuestState.IN_PROGRESS &&
                selectedQuest.state != QuestState.FINISHED)
            {
                manager.Debug_ForceStartQuest(selectedQuest);
            }

            AdvanceSelectedQuestToStep(manager, selectedQuest, selectedStepIndex);
        }

        private static void AdvanceSelectedQuestToStep(QuestManager manager, QuestInfo quest, int targetStepIndex)
        {
            if (manager == null || quest == null || quest.state != QuestState.IN_PROGRESS)
                return;

            int clampedStepIndex = Mathf.Clamp(targetStepIndex, 0, Mathf.Max(quest.StepCount - 1, 0));
            int safety = 0;

            while (quest.state == QuestState.IN_PROGRESS &&
                   quest.currentStepIndex < clampedStepIndex &&
                   safety < 64)
            {
                manager.Debug_CompleteCurrentStep(quest);
                safety++;
            }
        }

        private bool TryResolveManager(out QuestManager manager)
        {
            ResolveQuestManager();
            manager = questManager;
            return manager != null;
        }

        private void ResolveQuestManager()
        {
            if (questManager != null)
                return;

            questManager = GetComponent<QuestManager>();
            if (questManager == null && QuestManager.HasInstance)
                questManager = QuestManager.Instance;
        }
    }
}
