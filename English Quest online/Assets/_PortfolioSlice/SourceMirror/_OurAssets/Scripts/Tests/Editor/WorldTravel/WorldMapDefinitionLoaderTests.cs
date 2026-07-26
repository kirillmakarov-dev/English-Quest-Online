using NUnit.Framework;
using UnityEngine;

namespace EnglishKingdom.Tests.WorldTravel
{
    [TestFixture]
    public class WorldMapDefinitionLoaderTests
    {
        [TearDown]
        public void TearDown()
        {
            WorldMapDefinitionLoader.Initialize(null);
        }

        [Test]
        public void ResolveByMapId_ReturnsRegisteredMapAtRuntime()
        {
            var map = ScriptableObject.CreateInstance<WorldMapDefinitionSO>();
            try
            {
                var mapIdField = typeof(WorldMapDefinitionSO).GetField(
                    "_mapId",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                mapIdField?.SetValue(map, "campaign_v1");

                WorldMapDefinitionLoader.Initialize(map);

                WorldMapDefinitionSO resolved = WorldMapDefinitionLoader.ResolveByMapId("campaign_v1");
                Assert.That(resolved, Is.SameAs(map));
            }
            finally
            {
                Object.DestroyImmediate(map);
            }
        }

        [Test]
        public void ResolveByMapId_ReturnsNullForUnknownMap()
        {
            WorldMapDefinitionLoader.Initialize(null);
            Assert.That(WorldMapDefinitionLoader.ResolveByMapId("missing_map"), Is.Null);
        }
    }
}
