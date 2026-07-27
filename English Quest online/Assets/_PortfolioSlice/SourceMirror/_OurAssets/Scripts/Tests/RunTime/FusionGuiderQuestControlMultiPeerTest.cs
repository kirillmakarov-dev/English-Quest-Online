using System.Collections;
using EnglishQuest.Tests;
using EnglishQuest.Tests.RunTime.Fixtures;
using Fusion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EnglishQuest.Tests.RunTime
{
    [Category(TestCategories.Fast)]
    public class FusionGuiderQuestControlMultiPeerTest : CombatTestMultiPeerFixture
    {
        private string _previousRole;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previousRole = UnityEngine.PlayerPrefs.GetString("playerRole", PlayerRoleProfile.RoleStudent);
            PlayerRoleProfile.SetGuider(true);
            yield return BootstrapCombatSceneIfNeededIgnoringLogs();
        }

        [UnityTearDown]
        public void TearDownRole()
        {
            if (string.IsNullOrEmpty(_previousRole))
                PlayerRoleProfile.Clear();
            else
                PlayerRoleProfile.SetRole(_previousRole);
        }

        [UnityTest]
        public IEnumerator MultiPeer_GuiderQuestSnapshot_CanBeRequestedForRemotePlayer() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(SnapshotBody());

        private IEnumerator SnapshotBody()
        {
            NetworkRunner guiderRunner = FusionMultiPeerTestSupport.GetHostRunner(Runners);
            NetworkRunner targetRunner = FusionMultiPeerTestSupport.GetClientRunner(Runners);

            yield return FusionMultiPeerTestSupport.WaitForRemotePlayersVisible(
                guiderRunner,
                targetRunner,
                FusionMultiPeerTestSupport.NetworkSyncTimeoutSeconds,
                "Both runners should see each other.");

            yield return WaitForGuider(guiderRunner);

            PlayerRef remotePlayer = default;
            Assert.That(
                FusionMultiPeerTestSupport.TryFindRemotePlayerRef(guiderRunner, out remotePlayer),
                Is.True,
                "Guider runner should see a remote player.");

            bool snapshotReceived = false;
            GuiderQuestSnapshotReceivedArgs receivedArgs = default;

            void HandleSnapshot(GuiderQuestSnapshotReceivedArgs args)
            {
                snapshotReceived = true;
                receivedArgs = args;
            }

            TeacherTeleport guiderTeleport = FusionMultiPeerTestSupport.FindComponentInRunner<TeacherTeleport>(guiderRunner);
            Assert.That(guiderTeleport, Is.Not.Null, "TeacherTeleport should exist in the combat test scene.");
            guiderTeleport.OnGuiderQuestSnapshotReceived += HandleSnapshot;

            try
            {
                guiderTeleport.RequestQuestSnapshot(remotePlayer);

                float deadline = Time.realtimeSinceStartup + FusionMultiPeerTestSupport.NetworkSyncTimeoutSeconds;
                while (!snapshotReceived && Time.realtimeSinceStartup < deadline)
                    yield return null;

                Assert.That(snapshotReceived, Is.True, "Guider should receive a quest snapshot response.");
                Assert.That(receivedArgs.TargetPlayer, Is.EqualTo(remotePlayer));
                Assert.That(receivedArgs.Entries, Is.Not.Null);
            }
            finally
            {
                guiderTeleport.OnGuiderQuestSnapshotReceived -= HandleSnapshot;
            }
        }

        [UnityTest]
        public IEnumerator MultiPeer_SocialPanel_ShowsQuestButtonOnlyForGuiderOnRemotePlayers() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(QuestButtonBody());

        private IEnumerator QuestButtonBody()
        {
            NetworkRunner guiderRunner = FusionMultiPeerTestSupport.GetHostRunner(Runners);
            NetworkRunner targetRunner = FusionMultiPeerTestSupport.GetClientRunner(Runners);

            SocialPanelController guiderPanel = FusionMultiPeerTestSupport.FindSocialPanelForRunner(guiderRunner);
            SocialPanelController targetPanel = FusionMultiPeerTestSupport.FindSocialPanelForRunner(targetRunner);

            Assert.That(guiderPanel, Is.Not.Null);
            Assert.That(targetPanel, Is.Not.Null);

            yield return FusionMultiPeerTestSupport.WaitForSocialServicesReady(
                guiderRunner,
                guiderPanel,
                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,
                "Guider social services should be ready.");
            yield return FusionMultiPeerTestSupport.WaitForSocialServicesReady(
                targetRunner,
                targetPanel,
                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,
                "Target social services should be ready.");
            yield return FusionMultiPeerTestSupport.WaitForRemotePlayersVisible(
                guiderRunner,
                targetRunner,
                FusionMultiPeerTestSupport.NetworkSyncTimeoutSeconds,
                "Runners should see each other.");

            yield return WaitForGuider(guiderRunner);

            FusionMultiPeerTestSupport.SetSoloInputRunner(guiderRunner);
            yield return null;

            Assert.That(
                GuiderService.IsLocalPlayerGuiderFor(guiderRunner),
                Is.True,
                "Focused host runner should be session guider.");

            guiderPanel.Open();
            yield return null;
            Assert.That(guiderPanel.IsOpen, Is.True);
            guiderPanel.Close();
        }

        private static IEnumerator WaitForGuider(NetworkRunner runner)
        {
            float deadline = Time.realtimeSinceStartup + FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                FusionMultiPeerTestSupport.SetSoloInputRunner(runner);
                yield return null;

                if (GuiderService.IsLocalPlayerGuiderFor(runner))
                    yield break;
            }

            Assert.That(
                GuiderService.IsLocalPlayerGuiderFor(runner),
                Is.True,
                "Expected a local guider on the focused runner.");
        }
    }
}

