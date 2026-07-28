using EnglishQuest.QuestSystem;
using UnityEngine;

namespace EnglishQuest.PortfolioDemo
{
    public static class PortfolioPlayerStatusFormatter
    {
        public const string DefaultExploringStatus = "Exploring open world";
        public const string CompletedStatus = "Finished all lessons";
        public const string SoloSessionLabel = "Solo session";
        public const string SharedSessionLabel = "Shared session";
        public const string SoloSessionSuffix = "Solo session active";
        public const string SharedSessionSuffix = "Shared session active";
        public const string PreparingLocalPlayerStatus = "Preparing the local player";
        public const string SoloPlayerPanelTitle = "Player - Solo Progress";
        public const string SharedPlayerPanelTitle = "Players - Independent Progress";

        public static string BuildActiveLessonStatus(
            PortfolioGameFlowState flowState,
            int lessonIndex,
            QuestState questState,
            string questTitle,
            int currentStepIndex,
            int totalSteps)
        {
            if (lessonIndex <= 0)
                return DefaultExploringStatus;

            if (flowState == PortfolioGameFlowState.LevelCompleted)
                return CompletedStatus;

            return flowState switch
            {
                PortfolioGameFlowState.Dialogue => $"In dialogue - Lesson {lessonIndex}",
                PortfolioGameFlowState.MiniGame => $"In mini-game - Lesson {lessonIndex}",
                PortfolioGameFlowState.QuestCompleted => $"Finished Lesson {lessonIndex}",
                _ => BuildOpenWorldLessonStatus(lessonIndex, questState, questTitle, currentStepIndex, totalSteps)
            };
        }

        public static string BuildAvailabilityStatus(QuestState questState, int lessonIndex, string questTitle)
        {
            if (lessonIndex <= 0)
                return DefaultExploringStatus;

            string title = SanitizeQuestTitle(questTitle);

            return questState switch
            {
                QuestState.CAN_START => $"Ready: Lesson {lessonIndex} - {title}",
                QuestState.REQUIREMENTS_NOT_MET => $"Locked: Lesson {lessonIndex} - {title}",
                _ => DefaultExploringStatus
            };
        }

        public static string DecorateSessionStatus(string baseStatus, int connectedPlayerCount, bool isLocalPlayer)
        {
            string status = string.IsNullOrWhiteSpace(baseStatus)
                ? DefaultExploringStatus
                : baseStatus.Trim();

            if (!isLocalPlayer)
                return status;

            string suffix = connectedPlayerCount > 1
                ? SharedSessionSuffix
                : SoloSessionSuffix;

            return $"{status} | {suffix}";
        }

        public static string GetSessionTopologyLabel(int connectedPlayerCount)
        {
            return connectedPlayerCount > 1
                ? SharedSessionLabel
                : SoloSessionLabel;
        }

        public static string GetPlayerPanelTitle(int connectedPlayerCount)
        {
            return connectedPlayerCount > 1
                ? SharedPlayerPanelTitle
                : SoloPlayerPanelTitle;
        }

        public static string GetPlayerPanelSupportText(int connectedPlayerCount)
        {
            return connectedPlayerCount > 1
                ? "Each player advances their own lesson chain. No mission requires a partner."
                : "You can complete the entire lesson chain alone in this session.";
        }

        private static string BuildOpenWorldLessonStatus(
            int lessonIndex,
            QuestState questState,
            string questTitle,
            int currentStepIndex,
            int totalSteps)
        {
            if (questState == QuestState.CAN_FINISH)
                return $"Ready to finish Lesson {lessonIndex}";

            string title = SanitizeQuestTitle(questTitle);
            int safeTotalSteps = Mathf.Max(totalSteps, 1);
            int safeCurrentStep = Mathf.Clamp(currentStepIndex + 1, 1, safeTotalSteps);

            return safeTotalSteps > 1
                ? $"Lesson {lessonIndex}: {title} ({safeCurrentStep}/{safeTotalSteps})"
                : $"Lesson {lessonIndex}: {title}";
        }

        private static string SanitizeQuestTitle(string questTitle)
        {
            return string.IsNullOrWhiteSpace(questTitle)
                ? "Quest"
                : questTitle.Trim();
        }
    }
}
