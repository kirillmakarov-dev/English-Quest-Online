using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace EnglishKingdom.Tests.MonsterSpawn
{
    [TestFixture]
    public class MonsterSpawnPoolSelectorTests
    {
        [Test]
        public void Select_ReturnsNull_WhenEntriesEmpty()
        {
            var entries = new List<MonsterSpawnPoolEntry>();
            Assert.That(MonsterSpawnPoolSelector.Select(entries, new System.Random(1)), Is.Null);
        }

        [Test]
        public void Select_ReturnsOnlyValidEntry()
        {
            var definition = ScriptableObject.CreateInstance<MonsterDefinitionSO>();
            var entries = new List<MonsterSpawnPoolEntry>
            {
                new MonsterSpawnPoolEntry { monster = null, weight = 5 },
                new MonsterSpawnPoolEntry { monster = definition, weight = 1 }
            };

            try
            {
                Assert.That(MonsterSpawnPoolSelector.Select(entries, new System.Random(1)), Is.SameAs(definition));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void Select_RespectsWeights()
        {
            var heavy = ScriptableObject.CreateInstance<MonsterDefinitionSO>();
            var light = ScriptableObject.CreateInstance<MonsterDefinitionSO>();
            var entries = new List<MonsterSpawnPoolEntry>
            {
                new MonsterSpawnPoolEntry { monster = heavy, weight = 9 },
                new MonsterSpawnPoolEntry { monster = light, weight = 1 }
            };

            int heavyCount = 0;
            var rng = new System.Random(42);

            try
            {
                for (int i = 0; i < 1000; i++)
                {
                    MonsterDefinitionSO selected = MonsterSpawnPoolSelector.Select(entries, rng);
                    if (selected == heavy)
                        heavyCount++;
                }

                Assert.That(heavyCount, Is.GreaterThan(850));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(heavy);
                UnityEngine.Object.DestroyImmediate(light);
            }
        }
    }
}
