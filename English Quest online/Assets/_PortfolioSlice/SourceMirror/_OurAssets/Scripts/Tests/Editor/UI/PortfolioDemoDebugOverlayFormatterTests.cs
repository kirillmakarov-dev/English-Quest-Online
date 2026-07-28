using System.Collections.Generic;
using EnglishQuest.PortfolioDemo;
using NUnit.Framework;

namespace EnglishQuest.Tests.UI
{
    [Category(TestCategories.Fast)]
    public class PortfolioDemoDebugOverlayFormatterTests
    {
        [Test]
        public void FormatLocalPlayerSection_WhenMissing_ReturnsMissingLine()
        {
            string result = PortfolioDemoDebugOverlayFormatter.FormatLocalPlayerSection(
                new PortfolioDebugLocalPlayerSnapshot(
                    objectName: string.Empty,
                    ownsPlayer: false,
                    drivesView: false,
                    stateAuthorityPlayerId: 0,
                    inputAuthorityPlayerId: 0,
                    isMissing: true));

            Assert.AreEqual("Local Object: Missing", result);
        }

        [Test]
        public void FormatLocalPlayerSection_WhenPresent_ReturnsOwnershipDetails()
        {
            string result = PortfolioDemoDebugOverlayFormatter.FormatLocalPlayerSection(
                new PortfolioDebugLocalPlayerSnapshot(
                    objectName: "Player(Clone)",
                    ownsPlayer: true,
                    drivesView: true,
                    stateAuthorityPlayerId: 1,
                    inputAuthorityPlayerId: 1));

            StringAssert.Contains("Local Object: Player(Clone)", result);
            StringAssert.Contains("Owns Player: True", result);
            StringAssert.Contains("Drives View: True", result);
            StringAssert.Contains("State Authority: 1", result);
            StringAssert.Contains("Input Authority: 1", result);
        }

        [Test]
        public void FormatSessionPlayersSection_WhenEmpty_ShowsNone()
        {
            string result = PortfolioDemoDebugOverlayFormatter.FormatSessionPlayersSection(
                new List<PortfolioDebugSessionPlayerSnapshot>(),
                isSoloSession: false);

            Assert.AreEqual("Session Players: Shared session\n- None".Replace("\n", System.Environment.NewLine), result);
        }

        [Test]
        public void FormatSessionPlayersSection_WhenSoloSessionWithoutResolvedPlayers_ShowsPreparingMessage()
        {
            string result = PortfolioDemoDebugOverlayFormatter.FormatSessionPlayersSection(
                new List<PortfolioDebugSessionPlayerSnapshot>(),
                isSoloSession: true);

            Assert.AreEqual(
                "Session Players: Solo session\n- Local player is preparing".Replace("\n", System.Environment.NewLine),
                result);
        }

        [Test]
        public void FormatSessionPlayersSection_WhenPlayersExist_FormatsEachLine()
        {
            string result = PortfolioDemoDebugOverlayFormatter.FormatSessionPlayersSection(
                new List<PortfolioDebugSessionPlayerSnapshot>
                {
                    new("Player 1", "Lesson 1"),
                    new("Player 2", "In mini-game [MiniGame]")
                },
                isSoloSession: false);

            StringAssert.Contains("Session Players: Shared session", result);
            StringAssert.Contains("- Player 1: Lesson 1", result);
            StringAssert.Contains("- Player 2: In mini-game [MiniGame]", result);
        }

        [Test]
        public void FormatSessionPlayersSection_WhenSoloPlayerExists_ShowsSoloHeader()
        {
            string result = PortfolioDemoDebugOverlayFormatter.FormatSessionPlayersSection(
                new List<PortfolioDebugSessionPlayerSnapshot>
                {
                    new("Player 1", "Lesson 1 | Solo session active")
                },
                isSoloSession: true);

            StringAssert.Contains("Session Players: Solo session", result);
            StringAssert.Contains("- Player 1: Lesson 1 | Solo session active", result);
        }

        [Test]
        public void AppendFlowState_AppendsEnumToActivity()
        {
            string result = PortfolioDemoDebugOverlayFormatter.AppendFlowState(
                "Finished Lesson 2",
                PortfolioGameFlowState.QuestCompleted);

            Assert.AreEqual("Finished Lesson 2 [QuestCompleted]", result);
        }

        [Test]
        public void AppendFlowState_UsesFallbackForEmptyActivity()
        {
            string result = PortfolioDemoDebugOverlayFormatter.AppendFlowState(
                "",
                PortfolioGameFlowState.OpenWorld);

            Assert.AreEqual("Exploring open world [OpenWorld]", result);
        }

        [Test]
        public void FormatOwnershipRulesSection_ReturnsLocalAndSharedRules()
        {
            string result = PortfolioDemoDebugOverlayFormatter.FormatOwnershipRulesSection();

            StringAssert.Contains("Ownership Rules:", result);
            StringAssert.Contains("- Dialogue: local player only", result);
            StringAssert.Contains("- Mini-games: local player only", result);
            StringAssert.Contains("- Quest progress: per-player", result);
            StringAssert.Contains("- Optional co-op: shared world only", result);
            StringAssert.Contains("- Main lesson chain: fully solo-playable", result);
            StringAssert.Contains("- Second player: never required", result);
        }
    }
}
