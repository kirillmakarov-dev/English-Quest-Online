using EnglishQuest.PortfolioDemo;
using NUnit.Framework;

namespace EnglishQuest.Tests.UI
{
    [Category(TestCategories.Fast)]
    public class PortfolioDemoBriefingFormatterTests
    {
        [Test]
        public void Build_WhenSessionIsNotReady_ShowsConnectingBriefing()
        {
            PortfolioDemoBriefingContent content = PortfolioDemoBriefingFormatter.Build(
                isSessionReady: false,
                isLevelCompleted: false,
                connectedPlayerCount: 0,
                highlightedQuestState: null,
                questHeadline: null,
                npcDisplayName: null,
                objectiveSummary: null);

            Assert.AreEqual("Connecting", content.Title);
            StringAssert.Contains("Starting the session", content.Body);
            StringAssert.Contains("Solo play is fully supported", content.Body);
            StringAssert.DoesNotContain("shared session", content.Body);
        }

        [Test]
        public void Build_WhenLevelIsCompleted_ShowsCompletionBriefing()
        {
            PortfolioDemoBriefingContent content = PortfolioDemoBriefingFormatter.Build(
                isSessionReady: true,
                isLevelCompleted: true,
                connectedPlayerCount: 1,
                highlightedQuestState: null,
                questHeadline: null,
                npcDisplayName: null,
                objectiveSummary: null);

            Assert.AreEqual("Prototype Complete", content.Title);
            StringAssert.Contains("replay from the beginning", content.Body);
            StringAssert.Contains("Solo completion remains fully valid", content.Body);
            StringAssert.Contains("second player stays optional", content.Body);
        }

        [Test]
        public void Build_WhenQuestCanStart_ShowsNextNpcAndGoal()
        {
            PortfolioDemoBriefingContent content = PortfolioDemoBriefingFormatter.Build(
                isSessionReady: true,
                isLevelCompleted: false,
                connectedPlayerCount: 1,
                highlightedQuestState: QuestState.CAN_START,
                questHeadline: "Lesson 1 - Letters",
                npcDisplayName: "Teacher Ada",
                objectiveSummary: "Match A and B with the correct words.",
                optionalCoopLine: "Optional co-op: stand together in the Study Circle.");

            Assert.AreEqual("Next: Lesson 1 - Letters", content.Title);
            StringAssert.Contains("Talk to Teacher Ada to begin.", content.Body);
            StringAssert.Contains("Learning goal: Match A and B with the correct words.", content.Body);
            StringAssert.Contains("Solo play is fully supported", content.Body);
            StringAssert.Contains("Optional co-op: stand together in the Study Circle.", content.Body);
        }

        [Test]
        public void Build_WhenQuestCanStart_WithMultiplePlayers_KeepsIndependentProgressMessage()
        {
            PortfolioDemoBriefingContent content = PortfolioDemoBriefingFormatter.Build(
                isSessionReady: true,
                isLevelCompleted: false,
                connectedPlayerCount: 2,
                highlightedQuestState: QuestState.CAN_START,
                questHeadline: "Lesson 1 - Letters",
                npcDisplayName: "Teacher Ada",
                objectiveSummary: "Match A and B with the correct words.");

            Assert.AreEqual("Next: Lesson 1 - Letters", content.Title);
            StringAssert.Contains("Other players can stay on their own lesson state", content.Body);
        }

        [Test]
        public void Build_WhenQuestIsInProgress_AndSoloPlayer_ShowsSoloFriendlyMessage()
        {
            PortfolioDemoBriefingContent content = PortfolioDemoBriefingFormatter.Build(
                isSessionReady: true,
                isLevelCompleted: false,
                connectedPlayerCount: 1,
                highlightedQuestState: QuestState.IN_PROGRESS,
                questHeadline: "Lesson 2 - Missing Letter",
                npcDisplayName: "Coach Ben",
                objectiveSummary: "Fill the missing letter to complete each word.");

            Assert.AreEqual("Current: Lesson 2 - Missing Letter", content.Title);
            StringAssert.Contains("Current objective: Fill the missing letter", content.Body);
            StringAssert.Contains("Solo play is active", content.Body);
            StringAssert.Contains("does not block this lesson", content.Body);
        }

        [Test]
        public void Build_WhenQuestIsInProgress_WithMultiplePlayers_PreservesIndependentProgressMessage()
        {
            PortfolioDemoBriefingContent content = PortfolioDemoBriefingFormatter.Build(
                isSessionReady: true,
                isLevelCompleted: false,
                connectedPlayerCount: 2,
                highlightedQuestState: QuestState.IN_PROGRESS,
                questHeadline: "Lesson 2 - Missing Letter",
                npcDisplayName: "Coach Ben",
                objectiveSummary: "Fill the missing letter to complete each word.");

            Assert.AreEqual("Current: Lesson 2 - Missing Letter", content.Title);
            StringAssert.Contains("Current objective: Fill the missing letter", content.Body);
            StringAssert.Contains("they can keep progressing independently", content.Body);
        }

        [Test]
        public void Build_WhenQuestCanFinish_ShowsReturnToNpc()
        {
            PortfolioDemoBriefingContent content = PortfolioDemoBriefingFormatter.Build(
                isSessionReady: true,
                isLevelCompleted: false,
                connectedPlayerCount: 1,
                highlightedQuestState: QuestState.CAN_FINISH,
                questHeadline: "Lesson 3 - Sentence Order",
                npcDisplayName: "Guide Nora",
                objectiveSummary: "Build the full sentence.");

            Assert.AreEqual("Return to Guide Nora", content.Title);
            Assert.AreEqual(
                "Lesson complete. Talk to Guide Nora to close this stage and unlock the next one.",
                content.Body);
        }

        [Test]
        public void Build_WhenQuestIsLocked_ShowsLockedBriefing()
        {
            PortfolioDemoBriefingContent content = PortfolioDemoBriefingFormatter.Build(
                isSessionReady: true,
                isLevelCompleted: false,
                connectedPlayerCount: 1,
                highlightedQuestState: QuestState.REQUIREMENTS_NOT_MET,
                questHeadline: "Lesson 3 - Sentence Order",
                npcDisplayName: "Guide Nora",
                objectiveSummary: "Build the full sentence.");

            Assert.AreEqual("Locked: Lesson 3 - Sentence Order", content.Title);
            StringAssert.Contains("Finish the earlier lesson chain first", content.Body);
            StringAssert.Contains("not player count", content.Body);
        }

        [Test]
        public void Build_WhenNoQuestIsHighlighted_ShowsExploreFallback()
        {
            PortfolioDemoBriefingContent content = PortfolioDemoBriefingFormatter.Build(
                isSessionReady: true,
                isLevelCompleted: false,
                connectedPlayerCount: 1,
                highlightedQuestState: null,
                questHeadline: null,
                npcDisplayName: null,
                objectiveSummary: null);

            Assert.AreEqual("Explore the Demo", content.Title);
            StringAssert.Contains("Solo completion remains the primary flow", content.Body);
        }
    }
}
