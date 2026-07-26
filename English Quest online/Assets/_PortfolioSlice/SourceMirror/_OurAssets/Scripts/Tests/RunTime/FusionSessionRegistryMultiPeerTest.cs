using System.Collections;
using System.Linq;
using EnglishKingdom.Tests;
using EnglishKingdom.Tests.RunTime.Fixtures;
using Fusion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EnglishKingdom.Tests.RunTime
{
    [Category(TestCategories.Fast)]
    public class FusionSessionRegistryMultiPeerTest : CombatTestMultiPeerFixture
    {
        [UnitySetUp]
        public IEnumerator EnsureBootstrap() => BootstrapCombatSceneIfNeededIgnoringLogs();

        [UnityTest]
        public IEnumerator MultiPeer_SessionRegistry_ListsRemotePlayers() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(ListsRemotePlayersBody());

        [UnityTest]
        public IEnumerator MultiPeer_SessionRegistry_FlagsPartyAfterJoin() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(FlagsPartyAfterJoinBody());

        private IEnumerator ListsRemotePlayersBody()
        {
            NetworkRunner firstRunner = Runners[0];
            NetworkRunner secondRunner = Runners[1];

            MonoBehaviour firstContext =
                FusionMultiPeerTestSupport.FindSocialContextForRunner(firstRunner);
            MonoBehaviour secondContext =
                FusionMultiPeerTestSupport.FindSocialContextForRunner(secondRunner);

            yield return FusionMultiPeerTestSupport.WaitForSocialServicesReady(
                firstRunner,
                firstContext,
                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,
                "First runner social services should be ready.");
            yield return FusionMultiPeerTestSupport.WaitForSocialServicesReady(
                secondRunner,
                secondContext,
                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,
                "Second runner social services should be ready.");

            yield return FusionMultiPeerTestSupport.WaitForRemotePlayersVisible(
                firstRunner,
                secondRunner,
                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,
                "Both runners should see remote players before registry checks.");

            yield return FusionMultiPeerTestSupport.WaitForSessionPlayerCount(
                firstContext,
                2,
                FusionMultiPeerTestSupport.NetworkSyncTimeoutSeconds,
                "First runner registry should list two session players.");

            Assert.That(
                FusionMultiPeerTestSupport.TryGetSessionPlayerRegistry(firstRunner, out ISessionPlayerRegistry firstRegistry),
                Is.True,
                "First runner should resolve ISessionPlayerRegistry.");

            firstRegistry.Refresh();

            Assert.That(firstRegistry.Players.Count, Is.EqualTo(2),
                "Session registry should list exactly two players before party join.");

            int localCount = firstRegistry.Players.Count(player => player.IsLocal);
            int remoteCount = firstRegistry.Players.Count(player => !player.IsLocal);

            Assert.That(localCount, Is.EqualTo(1), "Registry should include one local player.");
            Assert.That(remoteCount, Is.EqualTo(1), "Registry should include one remote player.");

            SessionPlayerInfo localEntry =
                firstRegistry.Players.FirstOrDefault(player => player.IsLocal);
            SessionPlayerInfo remoteEntry =
                firstRegistry.Players.FirstOrDefault(player => !player.IsLocal);

            Assert.That(localEntry.IsPartyLeader, Is.True,
                "Local solo player should be listed as their own party leader.");
            Assert.That(localEntry.IsInMyParty, Is.True,
                "Local solo player counts as being in their own party.");
            Assert.That(remoteEntry.IsInMyParty, Is.False,
                "Remote solo player should not be listed as in the local party before join.");
        }

        private IEnumerator FlagsPartyAfterJoinBody()
        {
            NetworkRunner leaderRunner = Runners[0];
            NetworkRunner memberRunner = Runners[1];

            MonoBehaviour leaderContext =
                FusionMultiPeerTestSupport.FindSocialContextForRunner(leaderRunner);
            MonoBehaviour memberContext =
                FusionMultiPeerTestSupport.FindSocialContextForRunner(memberRunner);

            yield return FusionMultiPeerTestSupport.WaitForSocialServicesReady(
                leaderRunner,
                leaderContext,
                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,
                "Leader runner social services should be ready.");
            yield return FusionMultiPeerTestSupport.WaitForSocialServicesReady(
                memberRunner,
                memberContext,
                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,
                "Member runner social services should be ready.");

            yield return FusionMultiPeerTestSupport.WaitForRemotePlayersVisible(
                leaderRunner,
                memberRunner,
                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,
                "Both runners should see remote players before party join.");

            PlayerRef leaderRef = leaderRunner.LocalPlayer;
            Assert.That(
                FusionMultiPeerTestSupport.TryGetRemotePlayerRef(leaderRunner, out PlayerRef memberRef),
                Is.True,
                "Leader runner should resolve the remote member PlayerRef.");

            yield return FusionMultiPeerTestSupport.FormPartyViaInvite(
                leaderRunner, memberRunner, leaderRef, memberRef);

            Assert.That(
                FusionMultiPeerTestSupport.TryGetSessionPlayerRegistry(leaderRunner, out ISessionPlayerRegistry leaderRegistry),
                Is.True,
                "Leader runner should resolve ISessionPlayerRegistry.");

            Assert.That(
                FusionMultiPeerTestSupport.TryGetSessionPlayerRegistry(memberRunner, out ISessionPlayerRegistry memberRegistry),
                Is.True,
                "Member runner should resolve ISessionPlayerRegistry.");

            yield return WaitForRegistryPartyFlag(
                leaderRegistry,
                memberRef,
                true,
                "Leader registry should flag the member as in the local party after join.");

            yield return WaitForRegistryPartyFlag(
                memberRegistry,
                leaderRef,
                true,
                "Member registry should flag the leader as in the local party after join.");

            leaderRegistry.Refresh();
            memberRegistry.Refresh();

            SessionPlayerInfo leaderViewOfMember =
                leaderRegistry.Players.FirstOrDefault(player => player.PlayerRef == memberRef);
            SessionPlayerInfo memberViewOfLeader =
                memberRegistry.Players.FirstOrDefault(player => player.PlayerRef == leaderRef);

            Assert.That(leaderViewOfMember.IsInMyParty, Is.True,
                "Leader registry entry for member should have IsInMyParty true.");
            Assert.That(memberViewOfLeader.IsInMyParty, Is.True,
                "Member registry entry for leader should have IsInMyParty true.");
            Assert.That(leaderViewOfMember.IsPartyLeader, Is.False,
                "Member should not be flagged as party leader on the leader runner.");
            Assert.That(memberViewOfLeader.IsPartyLeader, Is.True,
                "Leader should be flagged as party leader on the member runner.");
        }

        private static IEnumerator WaitForRegistryPartyFlag(
            ISessionPlayerRegistry registry,
            PlayerRef playerRef,
            bool expectedInParty,
            string failureMessage)
        {
            yield return FusionMultiPeerTestSupport.WaitUntil(
                () =>
                {
                    registry.Refresh();
                    for (int i = 0; i < registry.Players.Count; i++)
                    {
                        SessionPlayerInfo info = registry.Players[i];
                        if (info.PlayerRef == playerRef)
                            return info.IsInMyParty == expectedInParty;
                    }

                    return false;
                },
                FusionMultiPeerTestSupport.NetworkSyncTimeoutSeconds,
                failureMessage);
        }
    }
}
