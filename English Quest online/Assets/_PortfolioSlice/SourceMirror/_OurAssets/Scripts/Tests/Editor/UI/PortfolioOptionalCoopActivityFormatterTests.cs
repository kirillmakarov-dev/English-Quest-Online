using EnglishQuest.PortfolioDemo;
using NUnit.Framework;

namespace EnglishQuest.Tests.UI
{
    [Category(TestCategories.Fast)]
    public class PortfolioOptionalCoopActivityFormatterTests
    {
        [Test]
        public void BuildPlayerStatusSuffix_ForSoloSession_ReturnsEmpty()
        {
            string suffix = PortfolioOptionalCoopActivityFormatter.BuildPlayerStatusSuffix(
                new PortfolioOptionalCoopActivitySnapshot(
                    "Study Circle",
                    connectedPlayers: 1,
                    playersInside: 1,
                    requiredPlayers: 2,
                    isLocalPlayerInside: true,
                    isGroupActive: false));

            Assert.AreEqual(string.Empty, suffix);
        }

        [Test]
        public void BuildPlayerStatusSuffix_ForWaitingSharedPlayer_ShowsWaitingState()
        {
            string suffix = PortfolioOptionalCoopActivityFormatter.BuildPlayerStatusSuffix(
                new PortfolioOptionalCoopActivitySnapshot(
                    "Study Circle",
                    connectedPlayers: 2,
                    playersInside: 1,
                    requiredPlayers: 2,
                    isLocalPlayerInside: true,
                    isGroupActive: false));

            Assert.AreEqual("Waiting at Study Circle (1/2)", suffix);
        }

        [Test]
        public void BuildPlayerStatusSuffix_ForActiveSharedPlayer_ShowsActiveState()
        {
            string suffix = PortfolioOptionalCoopActivityFormatter.BuildPlayerStatusSuffix(
                new PortfolioOptionalCoopActivitySnapshot(
                    "Study Circle",
                    connectedPlayers: 2,
                    playersInside: 2,
                    requiredPlayers: 2,
                    isLocalPlayerInside: true,
                    isGroupActive: true));

            Assert.AreEqual("Study Circle active", suffix);
        }

        [Test]
        public void BuildPlayerStatusSuffix_ForSharedSessionWhenLocalPlayerIsOutside_ReturnsEmpty()
        {
            string suffix = PortfolioOptionalCoopActivityFormatter.BuildPlayerStatusSuffix(
                new PortfolioOptionalCoopActivitySnapshot(
                    "Study Circle",
                    connectedPlayers: 2,
                    playersInside: 1,
                    requiredPlayers: 2,
                    isLocalPlayerInside: false,
                    isGroupActive: false));

            Assert.AreEqual(string.Empty, suffix);
        }

        [Test]
        public void AppendOptionalCoopStatus_ForSoloSession_LeavesBaseStatusUnchanged()
        {
            string status = PortfolioOptionalCoopActivityFormatter.AppendOptionalCoopStatus(
                "Lesson 1: Letters",
                new PortfolioOptionalCoopActivitySnapshot(
                    "Study Circle",
                    connectedPlayers: 1,
                    playersInside: 0,
                    requiredPlayers: 2,
                    isLocalPlayerInside: false,
                    isGroupActive: false));

            Assert.AreEqual("Lesson 1: Letters", status);
        }

        [Test]
        public void BuildBriefingLine_ForSoloSession_ReassuresThatCoopIsOptional()
        {
            string line = PortfolioOptionalCoopActivityFormatter.BuildBriefingLine(
                new PortfolioOptionalCoopActivitySnapshot(
                    "Study Circle",
                    connectedPlayers: 1,
                    playersInside: 0,
                    requiredPlayers: 2,
                    isLocalPlayerInside: false,
                    isGroupActive: false));

            StringAssert.Contains("if a second player joins later", line);
            StringAssert.Contains("never blocks quest progress", line);
        }

        [Test]
        public void BuildBriefingLine_ForWaitingSharedSession_StatesThatQuestProgressIsUnaffected()
        {
            string line = PortfolioOptionalCoopActivityFormatter.BuildBriefingLine(
                new PortfolioOptionalCoopActivitySnapshot(
                    "Study Circle",
                    connectedPlayers: 2,
                    playersInside: 1,
                    requiredPlayers: 2,
                    isLocalPlayerInside: true,
                    isGroupActive: false));

            StringAssert.Contains("1/2 players are inside", line);
            StringAssert.Contains("Quest progression is unaffected", line);
        }

        [Test]
        public void BuildBriefingLine_ForActiveSharedSession_StatesThatProgressRemainsPerPlayer()
        {
            string line = PortfolioOptionalCoopActivityFormatter.BuildBriefingLine(
                new PortfolioOptionalCoopActivitySnapshot(
                    "Study Circle",
                    connectedPlayers: 2,
                    playersInside: 2,
                    requiredPlayers: 2,
                    isLocalPlayerInside: true,
                    isGroupActive: true));

            StringAssert.Contains("Optional co-op active", line);
            StringAssert.Contains("per-player", line);
        }

        [Test]
        public void BuildBriefingLine_ForIdleSharedSession_StatesThatMissionsAreNotUnlockedOrBlocked()
        {
            string line = PortfolioOptionalCoopActivityFormatter.BuildBriefingLine(
                new PortfolioOptionalCoopActivitySnapshot(
                    "Study Circle",
                    connectedPlayers: 2,
                    playersInside: 0,
                    requiredPlayers: 2,
                    isLocalPlayerInside: false,
                    isGroupActive: false));

            StringAssert.Contains("Optional co-op only", line);
            StringAssert.Contains("does not unlock or block missions", line);
        }

        [Test]
        public void BuildDebugLine_ForActiveGroup_ShowsActiveCounter()
        {
            string line = PortfolioOptionalCoopActivityFormatter.BuildDebugLine(
                new PortfolioOptionalCoopActivitySnapshot(
                    "Study Circle",
                    connectedPlayers: 2,
                    playersInside: 2,
                    requiredPlayers: 2,
                    isLocalPlayerInside: true,
                    isGroupActive: true));

            Assert.AreEqual("Optional Co-op: Study Circle - ACTIVE (2/2)", line);
        }
    }
}
