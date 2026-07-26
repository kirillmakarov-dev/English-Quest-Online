using Fusion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Assert = NUnit.Framework.Assert;

namespace EnglishKingdom.Tests.MonsterSpawn
{
    [TestFixture]
    public class MonsterSpawnDirectorTests
    {
        [Test]
        public void RegisterPoint_DoesNotThrow_WhenPoolMissing()
        {
            var directorObject = new GameObject("Director");
            var pointObject = new GameObject("Point");
            var director = directorObject.AddComponent<MonsterSpawnDirector>();
            var point = pointObject.AddComponent<MonsterSpawnPoint>();

            try
            {
                Assert.DoesNotThrow(() => director.RegisterPoint(point));
                Assert.That(director.RegisteredPointCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(directorObject);
                Object.DestroyImmediate(pointObject);
            }
        }

        [Test]
        public void RegisterPoint_DoesNotSpawnSynchronously_WhenPoolAssigned()
        {
            var directorObject = new GameObject("Director");
            var pointObject = new GameObject("Point");
            var director = directorObject.AddComponent<MonsterSpawnDirector>();
            var point = pointObject.AddComponent<MonsterSpawnPoint>();
            var pool = ScriptableObject.CreateInstance<MonsterSpawnPoolSO>();
            var definition = ScriptableObject.CreateInstance<MonsterDefinitionSO>();

            try
            {
                SetPointPool(point, pool, maxPopulation: 5);

                Assert.DoesNotThrow(() => director.RegisterPoint(point));
                Assert.That(director.RegisteredPointCount, Is.EqualTo(1));
                Assert.That(director.HasCompletedInitialPopulation, Is.False);
                Assert.That(director.GetTotalLiveCount(), Is.EqualTo(0));
                Assert.That(Object.FindObjectsByType<MonsterSpawnInstance>(FindObjectsSortMode.None), Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(directorObject);
                Object.DestroyImmediate(pointObject);
                Object.DestroyImmediate(pool);
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void NotifyMobDied_DoesNotThrow_WhenPointNotTracked()
        {
            var directorObject = new GameObject("Director");
            var pointObject = new GameObject("Point");
            var instanceObject = new GameObject("Instance");

            var director = directorObject.AddComponent<MonsterSpawnDirector>();
            var point = pointObject.AddComponent<MonsterSpawnPoint>();
            var instance = instanceObject.AddComponent<MonsterSpawnInstance>();

            try
            {
                Assert.DoesNotThrow(() => director.NotifyMobDied(point, instance));
            }
            finally
            {
                Object.DestroyImmediate(directorObject);
                Object.DestroyImmediate(pointObject);
                Object.DestroyImmediate(instanceObject);
            }
        }

        [Test]
        public void Defaults_MatchOpenWorldBestPractice()
        {
            var directorObject = new GameObject("Director");
            var director = directorObject.AddComponent<MonsterSpawnDirector>();

            try
            {
                Assert.That(director.ActivateRadius, Is.EqualTo(50f));
                Assert.That(director.DeactivateGraceSeconds, Is.EqualTo(20f));
                Assert.That(director.GlobalMaxLiveMonsters, Is.EqualTo(40));
                Assert.That(director.ActivateRadius, Is.GreaterThan(35f));
            }
            finally
            {
                Object.DestroyImmediate(directorObject);
            }
        }

        private static void SetPointPool(MonsterSpawnPoint point, MonsterSpawnPoolSO pool, int maxPopulation)
        {
            var serialized = new SerializedObject(point);
            serialized.FindProperty("_pool").objectReferenceValue = pool;
            serialized.FindProperty("_maxPopulation").intValue = maxPopulation;
            serialized.FindProperty("_spawnOnEnable").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    [TestFixture]
    public class MonsterSpawnProximityTests
    {
        [Test]
        public void IsWithinRadiusXZ_IgnoresHeightDifference()
        {
            Vector3 point = new Vector3(0f, 100f, 0f);
            Vector3 player = new Vector3(3f, 0f, 4f);

            Assert.That(MonsterSpawnProximity.IsWithinRadiusXZ(point, player, 5f), Is.True);
            Assert.That(MonsterSpawnProximity.IsWithinRadiusXZ(point, player, 4.9f), Is.False);
        }

        [Test]
        public void IsNearAnyPlayer_ReturnsTrue_WhenAnyPlayerInRadius()
        {
            var players = new[]
            {
                new Vector3(100f, 0f, 100f),
                new Vector3(2f, 0f, 0f)
            };

            Assert.That(
                MonsterSpawnProximity.IsNearAnyPlayer(Vector3.zero, players, 3f),
                Is.True);
        }

        [Test]
        public void IsNearAnyPlayer_ReturnsFalse_WhenAllPlayersFar()
        {
            var players = new[]
            {
                new Vector3(100f, 0f, 0f),
                new Vector3(0f, 0f, 100f)
            };

            Assert.That(
                MonsterSpawnProximity.IsNearAnyPlayer(Vector3.zero, players, 50f),
                Is.False);
        }
    }

    [TestFixture]
    public class EnglishKingdomNetworkObjectProviderTests
    {
        [Test]
        public void DebugEnqueue_IncreasesPooledCount()
        {
            var host = new GameObject("PoolProviderHost");
            var provider = host.AddComponent<EnglishKingdomNetworkObjectProvider>();
            var pooledObject = new GameObject("Pooled");
            var networkObject = pooledObject.AddComponent<NetworkObject>();

            try
            {
                var prefabId = default(NetworkPrefabId);
                Assert.That(provider.GetPooledCount(prefabId), Is.EqualTo(0));

                provider.DebugEnqueue(prefabId, networkObject);
                Assert.That(provider.GetPooledCount(prefabId), Is.EqualTo(1));
                Assert.That(pooledObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
                if (pooledObject != null)
                    Object.DestroyImmediate(pooledObject);
            }
        }
    }
}
