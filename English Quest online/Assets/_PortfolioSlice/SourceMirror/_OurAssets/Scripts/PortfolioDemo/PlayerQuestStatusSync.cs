using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishQuest.PortfolioDemo
{
    [DisallowMultipleComponent]
    [AddComponentMenu("English Quest/Portfolio Demo/Player Quest Status Sync")]
    public sealed class PlayerQuestStatusSync : NetworkBehaviour
    {
        private const float RefreshInterval = 0.35f;

        [Networked, OnChangedRender(nameof(OnStatusChanged))]
        public NetworkString<_128> StatusText { get; set; }

        [Networked] public int LessonIndex { get; set; }
        [Networked] public NetworkBool LevelCompleted { get; set; }
        [Networked] public int FlowStateCode { get; set; }

        public PortfolioGameFlowState FlowState => DecodeFlowState(FlowStateCode);

        private IQuestService questService;
        private QuestManager questManager;
        private PortfolioGameFlowCoordinator flowCoordinator;
        private float nextRefreshTime;
        private bool subscribed;

        public override void Spawned()
        {
            ResolveQuestService();
            RefreshStatus(force: true);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (!Object || !Object.IsValid || !Object.HasStateAuthority)
                return;

            ResolveQuestService();

            if (Time.unscaledTime < nextRefreshTime)
                return;

            nextRefreshTime = Time.unscaledTime + RefreshInterval;
            RefreshStatus(force: false);
        }

        private void ResolveQuestService()
        {
            if (questService == null)
                ServiceLocator.For(this)?.TryGet(out questService);

            if (questManager == null && QuestManager.HasInstance)
                questManager = QuestManager.Instance;

            if (questService == null)
                questService = questManager;

            if (flowCoordinator == null)
                flowCoordinator = FindFirstObjectByType<PortfolioGameFlowCoordinator>(FindObjectsInactive.Include);

            if (!subscribed && questService != null && Object != null && Object.IsValid && Object.HasStateAuthority)
                Subscribe();
        }

        private void Subscribe()
        {
            if (subscribed || questService == null)
                return;

            questService.OnQuestStarted += HandleQuestChanged;
            questService.OnQuestUpdated += HandleQuestChanged;
            questService.OnQuestCompleted += HandleQuestChanged;
            questService.OnQuestStateChanged += HandleQuestChanged;
            questService.OnLevelCompleted += HandleLevelCompleted;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || questService == null)
                return;

            questService.OnQuestStarted -= HandleQuestChanged;
            questService.OnQuestUpdated -= HandleQuestChanged;
            questService.OnQuestCompleted -= HandleQuestChanged;
            questService.OnQuestStateChanged -= HandleQuestChanged;
            questService.OnLevelCompleted -= HandleLevelCompleted;
            subscribed = false;
        }

        private void HandleQuestChanged(QuestInfo _)
        {
            RefreshStatus(force: true);
        }

        private void HandleLevelCompleted()
        {
            RefreshStatus(force: true);
        }

        private void RefreshStatus(bool force)
        {
            if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
                return;

            ComputeStatus(out int lessonIndex, out string statusText, out bool isCompleted);
            int flowStateCode = (int)(flowCoordinator != null ? flowCoordinator.CurrentState : PortfolioGameFlowState.OpenWorld);

            string currentStatus = StatusText.ToString();
            if (!force &&
                lessonIndex == LessonIndex &&
                flowStateCode == FlowStateCode &&
                isCompleted == LevelCompleted &&
                string.Equals(currentStatus, statusText, System.StringComparison.Ordinal))
            {
                return;
            }

            LessonIndex = lessonIndex;
            FlowStateCode = flowStateCode;
            LevelCompleted = isCompleted;
            StatusText = statusText;
        }

        private void ComputeStatus(out int lessonIndex, out string statusText, out bool isCompleted)
        {
            List<QuestInfo> orderedQuests = GetOrderedOpenWorldQuests();
            if (orderedQuests.Count == 0)
            {
                lessonIndex = 0;
                statusText = PortfolioPlayerStatusFormatter.DefaultExploringStatus;
                statusText = AppendOptionalCoopStatus(statusText);
                isCompleted = false;
                return;
            }

            if (questService != null && questService.IsLevelCompleted)
            {
                lessonIndex = orderedQuests.Count;
                statusText = PortfolioPlayerStatusFormatter.CompletedStatus;
                statusText = AppendOptionalCoopStatus(statusText);
                isCompleted = true;
                return;
            }

            QuestInfo activeQuest = FindActiveQuest(orderedQuests, out lessonIndex);
            if (activeQuest != null)
            {
                statusText = BuildStatusFromFlowState(activeQuest, lessonIndex);
                statusText = AppendOptionalCoopStatus(statusText);
                isCompleted = false;
                return;
            }

            for (int i = 0; i < orderedQuests.Count; i++)
            {
                QuestInfo quest = orderedQuests[i];
                if (quest == null)
                    continue;

                if (quest.state == QuestState.CAN_START)
                {
                    lessonIndex = i + 1;
                    statusText = PortfolioPlayerStatusFormatter.BuildAvailabilityStatus(
                        quest.state,
                        lessonIndex,
                        GetQuestTitle(quest));
                    statusText = AppendOptionalCoopStatus(statusText);
                    isCompleted = false;
                    return;
                }

                if (quest.state == QuestState.REQUIREMENTS_NOT_MET)
                {
                    lessonIndex = i + 1;
                    statusText = PortfolioPlayerStatusFormatter.BuildAvailabilityStatus(
                        quest.state,
                        lessonIndex,
                        GetQuestTitle(quest));
                    statusText = AppendOptionalCoopStatus(statusText);
                    isCompleted = false;
                    return;
                }
            }

            lessonIndex = orderedQuests.Count;
            statusText = PortfolioPlayerStatusFormatter.CompletedStatus;
            statusText = AppendOptionalCoopStatus(statusText);
            isCompleted = true;
        }

        private List<QuestInfo> GetOrderedOpenWorldQuests()
        {
            var quests = new List<QuestInfo>();

            if (questManager != null)
            {
                IReadOnlyList<QuestInfo> openWorldQuests = questManager.GetOpenWorldQuests();
                for (int i = 0; i < openWorldQuests.Count; i++)
                {
                    QuestInfo quest = openWorldQuests[i];
                    if (quest != null)
                        quests.Add(quest);
                }

                if (quests.Count > 0)
                    return quests;
            }

            if (questService?.AllQuests == null)
                return quests;

            for (int i = 0; i < questService.AllQuests.Count; i++)
            {
                QuestInfo quest = questService.AllQuests[i];
                if (quest != null && quest.UsesObjectives())
                    quests.Add(quest);
            }

            return quests;
        }

        private static QuestInfo FindActiveQuest(IReadOnlyList<QuestInfo> orderedQuests, out int lessonIndex)
        {
            for (int i = 0; i < orderedQuests.Count; i++)
            {
                QuestInfo quest = orderedQuests[i];
                if (quest == null)
                    continue;

                if (quest.state == QuestState.IN_PROGRESS || quest.state == QuestState.CAN_FINISH)
                {
                    lessonIndex = i + 1;
                    return quest;
                }
            }

            lessonIndex = 0;
            return null;
        }

        private string BuildStatusFromFlowState(QuestInfo quest, int lessonIndex)
        {
            PortfolioGameFlowState state = flowCoordinator != null
                ? flowCoordinator.CurrentState
                : PortfolioGameFlowState.OpenWorld;

            return PortfolioPlayerStatusFormatter.BuildActiveLessonStatus(
                state,
                lessonIndex,
                quest.state,
                GetQuestTitle(quest),
                quest.currentStepIndex,
                quest.StepCount);
        }

        private static string GetQuestTitle(QuestInfo quest)
        {
            if (quest == null)
                return "Quest";

            if (!string.IsNullOrWhiteSpace(quest.displayName))
                return quest.displayName.Trim();

            if (!string.IsNullOrWhiteSpace(quest.id))
                return quest.id.Trim();

            return "Quest";
        }

        private void OnStatusChanged()
        {
        }

        private string AppendOptionalCoopStatus(string baseStatus)
        {
            if (Runner == null || !Runner.IsRunning || Object == null || !Object.IsValid || !Object.HasStateAuthority)
                return baseStatus;

            PortfolioOptionalCoopStudyCircle studyCircle = PortfolioOptionalCoopStudyCircle.FindOrCreateRuntimeInstance();
            if (studyCircle == null ||
                !studyCircle.TryGetSnapshot(Runner, Object.StateAuthority, out PortfolioOptionalCoopActivitySnapshot snapshot))
            {
                return baseStatus;
            }

            return PortfolioOptionalCoopActivityFormatter.AppendOptionalCoopStatus(baseStatus, snapshot);
        }

        public static PortfolioGameFlowState DecodeFlowState(int flowStateCode)
        {
            if (System.Enum.IsDefined(typeof(PortfolioGameFlowState), flowStateCode))
                return (PortfolioGameFlowState)flowStateCode;

            return PortfolioGameFlowState.OpenWorld;
        }
    }
}
