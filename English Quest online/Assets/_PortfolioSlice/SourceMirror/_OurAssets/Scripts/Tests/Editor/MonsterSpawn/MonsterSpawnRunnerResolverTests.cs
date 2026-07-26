using Fusion;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using UnityEngine;

namespace EnglishKingdom.Tests.MonsterSpawn
{
    [TestFixture]
    public class MonsterSpawnRunnerResolverTests
    {
        [Test]
        public void IsNetworkSessionActive_ReturnsFalse_WhenNoContext()
        {
            Assert.That(MonsterSpawnRunnerResolver.IsNetworkSessionActive(null), Is.False);
        }

        [Test]
        public void TryGetSpawnRunner_ReturnsFalse_WhenNoSession()
        {
            var host = new GameObject("ResolverHost");
            try
            {
                Assert.That(
                    MonsterSpawnRunnerResolver.TryGetSpawnRunner(host.AddComponent<MonsterSpawnDirector>(), out NetworkRunner runner),
                    Is.False);
                Assert.That(runner, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
