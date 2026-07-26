using System.Collections;
using System.Collections.Generic;
using EnglishKingdom.Tests;
using EnglishKingdom.Tests.RunTime.Fixtures;
using Fusion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EnglishKingdom.Tests.RunTime
{
    [Category(TestCategories.Fast)]
    public class FusionCoSessionMultiPeerTest : CombatTestMultiPeerFixture
    {
        [UnitySetUp]
        public IEnumerator EnsureBootstrap() => BootstrapCombatSceneIfNeededIgnoringLogs();

        [UnityTest]
        public IEnumerator MultiPeer_CoSessionRunners_SeeBothLocalPlayers() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(CoSessionRunnersBody());

        [UnityTest]
        public IEnumerator MultiPeer_LocalPlayerReadiness_ReadyPerRunner() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(LifecycleReadyBody());

        private IEnumerator CoSessionRunnersBody()
        {
            NetworkRunner contextRunner = Runners[0];

            Assert.That(
                FusionMultiPeerTestSupport.CountCoSessionRunners(contextRunner),
                Is.GreaterThanOrEqualTo(2),
                "FusionCoSessionRunners.Enumerate should return at least two running peers.");

            Assert.That(
                FusionMultiPeerTestSupport.CountCoSessionPlayers(contextRunner),
                Is.GreaterThanOrEqualTo(2),
                "FusionCoSessionRunners.EnumeratePlayers should return at least two distinct players.");

            var localPlayers = new HashSet<PlayerRef>();
            foreach (PlayerRef player in FusionCoSessionRunners.EnumeratePlayers(contextRunner))
            {
                Assert.That(player.IsRealPlayer, Is.True, "Enumerated players should be real players.");
                localPlayers.Add(player);
            }

            Assert.That(
                localPlayers.Count,
                Is.GreaterThanOrEqualTo(2),
                "Co-session player enumeration should include two distinct PlayerRefs.");

            for (int i = 0; i < Runners.Count; i++)
            {
                NetworkRunner runner = Runners[i];
                Assert.That(
                    FusionCoSessionRunners.AreInSameSession(contextRunner, runner),
                    Is.True,
                    $"Runner '{runner.name}' should be in the same Fusion session as the context runner.");
            }

            yield return null;
        }

        private IEnumerator LifecycleReadyBody()
        {
            for (int i = 0; i < Runners.Count; i++)
            {
                NetworkRunner runner = Runners[i];
                Scene simulationScene = runner.SimulationUnityScene;

                Assert.That(simulationScene.IsValid(), Is.True,
                    $"Runner '{runner.name}' should have a valid simulation scene.");

                Assert.That(
                    LocalPlayerReadiness.IsReady(runner, simulationScene),
                    Is.True,
                    $"Runner '{runner.name}' should report local player ready in its simulation scene.");

                NetworkObject localPlayer = runner.GetPlayerObject(runner.LocalPlayer);
                Assert.That(localPlayer, Is.Not.Null,
                    $"Runner '{runner.name}' should have a spawned local player object.");
            }

            yield return null;
        }
    }
}
