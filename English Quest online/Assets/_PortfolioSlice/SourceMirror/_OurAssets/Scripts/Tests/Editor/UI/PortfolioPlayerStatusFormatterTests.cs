using EnglishQuest.PortfolioDemo;
using NUnit.Framework;

namespace EnglishQuest.Tests.UI
{
    [Category(TestCategories.Fast)]
    public class PortfolioPlayerStatusFormatterTests
    {
        [Test]
        public void BuildActiveLessonStatus_ForDialogue_ShowsDialogueState()
        {
            string status = PortfolioPlayerStatusFormatter.BuildActiveLessonStatus(
                PortfolioGameFlowState.Dialogue,
                lessonIndex: 1,
                QuestState.IN_PROGRESS,
                "Lesson 1 - Letters",
                currentStepIndex: 0,
                totalSteps: 1);

            Assert.AreEqual("In dialogue - Lesson 1", status);
        }

        [Test]
        public void BuildActiveLessonStatus_ForMiniGame_ShowsMiniGameState()
        {
            string status = PortfolioPlayerStatusFormatter.BuildActiveLessonStatus(
                PortfolioGameFlowState.MiniGame,
                lessonIndex: 2,
                QuestState.IN_PROGRESS,
                "Lesson 2 - Missing Letter",
                currentStepIndex: 0,
                totalSteps: 1);

            Assert.AreEqual("In mini-game - Lesson 2", status);
        }

        [Test]
        public void BuildActiveLessonStatus_ForQuestCompleted_ShowsFinishedState()
        {
            string status = PortfolioPlayerStatusFormatter.BuildActiveLessonStatus(
                PortfolioGameFlowState.QuestCompleted,
                lessonIndex: 3,
                QuestState.CAN_FINISH,
                "Lesson 3 - Sentence Order",
                currentStepIndex: 0,
                totalSteps: 1);

            Assert.AreEqual("Finished Lesson 3", status);
        }

        [Test]
        public void BuildActiveLessonStatus_ForOpenWorldCanFinish_ShowsTurnInPrompt()
        {
            string status = PortfolioPlayerStatusFormatter.BuildActiveLessonStatus(
                PortfolioGameFlowState.OpenWorld,
                lessonIndex: 2,
                QuestState.CAN_FINISH,
                "Lesson 2 - Missing Letter",
                currentStepIndex: 0,
                totalSteps: 1);

            Assert.AreEqual("Ready to finish Lesson 2", status);
        }

        [Test]
        public void BuildActiveLessonStatus_ForMultiStepQuest_ShowsProgress()
        {
            string status = PortfolioPlayerStatusFormatter.BuildActiveLessonStatus(
                PortfolioGameFlowState.OpenWorld,
                lessonIndex: 2,
                QuestState.IN_PROGRESS,
                "Lesson 2 - Missing Letter",
                currentStepIndex: 1,
                totalSteps: 4);

            Assert.AreEqual("Lesson 2: Lesson 2 - Missing Letter (2/4)", status);
        }

        [Test]
        public void BuildAvailabilityStatus_ForStartableLesson_ShowsReadyStatus()
        {
            string status = PortfolioPlayerStatusFormatter.BuildAvailabilityStatus(
                QuestState.CAN_START,
                lessonIndex: 2,
                questTitle: "Lesson 2 - Missing Letter");

            Assert.AreEqual("Ready: Lesson 2 - Lesson 2 - Missing Letter", status);
        }

        [Test]
        public void BuildAvailabilityStatus_ForLockedLesson_ShowsLockedStatus()
        {
            string status = PortfolioPlayerStatusFormatter.BuildAvailabilityStatus(
                QuestState.REQUIREMENTS_NOT_MET,
                lessonIndex: 3,
                questTitle: "Lesson 3 - Sentence Order");

            Assert.AreEqual("Locked: Lesson 3 - Lesson 3 - Sentence Order", status);
        }

        [Test]
        public void BuildActiveLessonStatus_ForCompletedLevel_ShowsCompletedStatus()
        {
            string status = PortfolioPlayerStatusFormatter.BuildActiveLessonStatus(
                PortfolioGameFlowState.LevelCompleted,
                lessonIndex: 3,
                QuestState.FINISHED,
                "Lesson 3 - Sentence Order",
                currentStepIndex: 0,
                totalSteps: 1);

            Assert.AreEqual(PortfolioPlayerStatusFormatter.CompletedStatus, status);
        }

        [Test]
        public void BuildAvailabilityStatus_WithInvalidLessonIndex_FallsBackToExploring()
        {
            string status = PortfolioPlayerStatusFormatter.BuildAvailabilityStatus(
                QuestState.CAN_START,
                lessonIndex: 0,
                questTitle: "Anything");

            Assert.AreEqual(PortfolioPlayerStatusFormatter.DefaultExploringStatus, status);
        }

        [Test]
        public void DecorateSessionStatus_ForSoloLocalPlayer_ShowsSoloSessionSuffix()
        {
            string status = PortfolioPlayerStatusFormatter.DecorateSessionStatus(
                "Lesson 1: Letters",
                connectedPlayerCount: 1,
                isLocalPlayer: true);

            Assert.AreEqual("Lesson 1: Letters | Solo session active", status);
        }

        [Test]
        public void DecorateSessionStatus_ForSharedLocalPlayer_ShowsSharedSessionSuffix()
        {
            string status = PortfolioPlayerStatusFormatter.DecorateSessionStatus(
                "Lesson 2: Missing Letter",
                connectedPlayerCount: 2,
                isLocalPlayer: true);

            Assert.AreEqual("Lesson 2: Missing Letter | Shared session active", status);
        }

        [Test]
        public void DecorateSessionStatus_ForRemotePlayer_LeavesStatusUntouched()
        {
            string status = PortfolioPlayerStatusFormatter.DecorateSessionStatus(
                "In mini-game - Lesson 2",
                connectedPlayerCount: 2,
                isLocalPlayer: false);

            Assert.AreEqual("In mini-game - Lesson 2", status);
        }

        [Test]
        public void DecorateSessionStatus_WithEmptyBaseStatus_UsesExploringFallback()
        {
            string status = PortfolioPlayerStatusFormatter.DecorateSessionStatus(
                "",
                connectedPlayerCount: 1,
                isLocalPlayer: true);

            Assert.AreEqual("Exploring open world | Solo session active", status);
        }

        [Test]
        public void GetSessionTopologyLabel_ForSoloSession_ReturnsSoloLabel()
        {
            string label = PortfolioPlayerStatusFormatter.GetSessionTopologyLabel(connectedPlayerCount: 1);

            Assert.AreEqual(PortfolioPlayerStatusFormatter.SoloSessionLabel, label);
        }

        [Test]
        public void GetSessionTopologyLabel_ForSharedSession_ReturnsSharedLabel()
        {
            string label = PortfolioPlayerStatusFormatter.GetSessionTopologyLabel(connectedPlayerCount: 2);

            Assert.AreEqual(PortfolioPlayerStatusFormatter.SharedSessionLabel, label);
        }

        [Test]
        public void GetPlayerPanelTitle_ForSoloSession_ReturnsSingularTitle()
        {
            string title = PortfolioPlayerStatusFormatter.GetPlayerPanelTitle(connectedPlayerCount: 1);

            Assert.AreEqual(PortfolioPlayerStatusFormatter.SoloPlayerPanelTitle, title);
        }

        [Test]
        public void GetPlayerPanelTitle_ForSharedSession_ReturnsPluralTitle()
        {
            string title = PortfolioPlayerStatusFormatter.GetPlayerPanelTitle(connectedPlayerCount: 2);

            Assert.AreEqual(PortfolioPlayerStatusFormatter.SharedPlayerPanelTitle, title);
        }

        [Test]
        public void GetPlayerPanelSupportText_ForSoloSession_ExplainsThatFullProgressIsSoloPlayable()
        {
            string text = PortfolioPlayerStatusFormatter.GetPlayerPanelSupportText(connectedPlayerCount: 1);

            Assert.AreEqual("You can complete the entire lesson chain alone in this session.", text);
        }

        [Test]
        public void GetPlayerPanelSupportText_ForSharedSession_ExplainsIndependentProgressAndNoPartnerRequirement()
        {
            string text = PortfolioPlayerStatusFormatter.GetPlayerPanelSupportText(connectedPlayerCount: 2);

            Assert.AreEqual("Each player advances their own lesson chain. No mission requires a partner.", text);
        }
    }
}
