using System.Collections;
using EnglishKingdom.Tests;
using EnglishKingdom.Tests.RunTime.Fixtures;
using Fusion;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EnglishKingdom.Tests.RunTime
{
    /// <summary>
    /// Play Mode smoke test for Fusion Multi-Peer editor setup.
    /// </summary>
    [Category(TestCategories.Fast)]
    public class FusionMultiPeerSmokeTest : CombatTestMultiPeerFixture
    {
        [UnitySetUp]
        public IEnumerator EnsureBootstrap() => BootstrapCombatSceneIfNeededIgnoringLogs();

        [UnityTest]
        public IEnumerator MultiPeer_CombatTest_StartsAtLeastTwoRunners() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(TestBody());

        private IEnumerator TestBody()
        {
            Assert.That(Runners.Count, Is.GreaterThanOrEqualTo(2),
                "Expected at least two running NetworkRunner instances after Fusion Multi-Peer auto-start.");

            for (int i = 0; i < Runners.Count; i++)
            {
                NetworkObject localPlayer = Runners[i].GetPlayerObject(Runners[i].LocalPlayer);
                Assert.That(localPlayer, Is.Not.Null,
                    $"Runner '{Runners[i].name}' should have a spawned local player object.");
            }

            yield return null;
        }
    }
}
