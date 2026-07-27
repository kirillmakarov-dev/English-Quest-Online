using System.Collections;
using EnglishQuest.Tests;
using EnglishQuest.Tests.RunTime.Fixtures;
using Fusion;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EnglishQuest.Tests.RunTime
{
    [Category(TestCategories.Fast)]
    public class FusionSocialPanelMultiPeerTest : CombatTestMultiPeerFixture
    {
        [UnitySetUp]
        public IEnumerator EnsureBootstrap() => BootstrapCombatSceneIfNeededIgnoringLogs();

        [UnityTest]
        public IEnumerator MultiPeer_SocialPanel_BindsServicesPerRunner() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(BindServicesBody());

        [UnityTest]
        public IEnumerator MultiPeer_SocialPanel_OnlyFocusedRunnerOpensAndClosesOnFocusSwitch() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(FocusSwitchBody());

        private IEnumerator BindServicesBody()
        {
            for (int i = 0; i < Runners.Count; i++)
            {
                NetworkRunner runner = Runners[i];
                SocialPanelController panel = FusionMultiPeerTestSupport.FindSocialPanelForRunner(runner);

                Assert.That(
                    panel,
                    Is.Not.Null,
                    $"Runner '{runner.name}' should have a SocialPanelController in its simulation scene. {FusionMultiPeerTestSupport.DescribeSocialPanels()}");

                yield return FusionMultiPeerTestSupport.WaitForSocialServicesReady(
                    runner,
                    panel,
                    FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,
                    $"Social services should bind for runner '{runner.name}'.");

                Assert.That(
                    SceneNetworkRunner.TryGetForScene(panel.gameObject.scene, out NetworkRunner sceneRunner),
                    Is.True,
                    $"Social panel for '{runner.name}' should map to a scene runner.");

                Assert.That(
                    sceneRunner,
                    Is.EqualTo(runner),
                    $"Social panel scene runner should match '{runner.name}'.");
            }
        }

        private IEnumerator FocusSwitchBody()
        {
            NetworkRunner firstRunner = Runners[0];
            NetworkRunner secondRunner = Runners[1];

            SocialPanelController firstPanel = FusionMultiPeerTestSupport.FindSocialPanelForRunner(firstRunner);
            SocialPanelController secondPanel = FusionMultiPeerTestSupport.FindSocialPanelForRunner(secondRunner);

            Assert.That(firstPanel, Is.Not.Null,
                $"First runner should have a social panel. {FusionMultiPeerTestSupport.DescribeSocialPanels()}");
            Assert.That(secondPanel, Is.Not.Null,
                $"Second runner should have a social panel. {FusionMultiPeerTestSupport.DescribeSocialPanels()}");

            yield return FusionMultiPeerTestSupport.WaitForSocialServicesReady(
                firstRunner,
                firstPanel,
                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,
                "First runner social services should be ready.");
            yield return FusionMultiPeerTestSupport.WaitForSocialServicesReady(
                secondRunner,
                secondPanel,
                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,
                "Second runner social services should be ready.");

            yield return FusionMultiPeerTestSupport.WaitForRemotePlayersVisible(
                firstRunner,
                secondRunner,
                FusionMultiPeerTestSupport.NetworkSyncTimeoutSeconds,
                "Both runners should see a remote player in the shared Fusion session.");

            FusionMultiPeerTestSupport.SetSoloInputRunner(firstRunner);
            yield return null;

            firstPanel.Open();
            yield return null;

            Assert.That(firstPanel.IsOpen, Is.True, "Focused first runner should open its social panel.");
            Assert.That(secondPanel.IsOpen, Is.False, "Unfocused second runner panel should stay closed.");

            Assert.That(
                FusionMultiPeerTestSupport.TryFindRemotePlayerRef(firstRunner, out _),
                Is.True,
                "Session should include a remote player from the shared Fusion session.");

            FusionMultiPeerTestSupport.SetSoloInputRunner(secondRunner);
            yield return null;
            yield return null;

            Assert.That(
                firstPanel.IsOpen,
                Is.False,
                "First runner panel should close after input focus moves away.");

            secondPanel.Open();
            yield return null;

            Assert.That(secondPanel.IsOpen, Is.True, "Focused second runner should open its social panel.");
            Assert.That(firstPanel.IsOpen, Is.False, "First runner panel should remain closed.");

            secondPanel.Close();
            yield return null;

            Assert.That(secondPanel.IsOpen, Is.False, "Second runner panel should close explicitly.");
        }
    }
}

