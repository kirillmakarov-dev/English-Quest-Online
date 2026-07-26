using System.Collections;
using EnglishKingdom.Tests;
using Fusion;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using UnityEngine.TestTools;

namespace EnglishKingdom.Tests.Networking
{
    [Category(TestCategories.Fast)]
    public class NetworkAuthorityServiceMultiPeerTest
    {
        [SetUp]
        public void SetUp()
        {
            TravelSceneLoadCoordinator.ClearRunner(null);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (NetworkRunner runner in NetworkRunner.Instances)
            {
                if (runner != null)
                    TravelSceneLoadCoordinator.ClearRunner(runner);
            }
        }

        [UnityTest]
        public IEnumerator SceneTransitionState_IdleWhenNoRunner()
        {
            Assert.AreEqual(SceneTransitionPhase.Idle, SceneTransitionState.GetPhase(null));
            yield return null;
        }

        [Test]
        public void SceneTransitionState_ClearRunner_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => SceneTransitionState.ClearRunner(null));
        }
    }
}
