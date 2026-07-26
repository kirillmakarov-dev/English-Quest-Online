using System.Collections.Generic;
using EnglishKingdom.QuestSystem;
using NUnit.Framework;
using UnityEngine;

namespace EnglishKingdom.Tests.QuestSystem
{
    [TestFixture]
    public class QuestWorldResolverTests
    {
        [Test]
        public void TryGetNpc_UnknownId_ReturnsFalseWithoutThrowing()
        {
            var catalogSet = ScriptableObject.CreateInstance<QuestWorldCatalogSetSO>();
            catalogSet.npcCatalog = ScriptableObject.CreateInstance<NpcCatalogSO>();
            var resolver = new QuestWorldResolver(catalogSet);

            Assert.IsFalse(resolver.TryGetNpc("missing_npc", out _));
        }

        [Test]
        public void TryGetArea_KnownId_ReturnsTrue()
        {
            var npcCatalog = ScriptableObject.CreateInstance<NpcCatalogSO>();
            var areaCatalog = ScriptableObject.CreateInstance<AreaCatalogSO>();
            SeedArea(areaCatalog, "zone_a", "Zone A");

            var catalogSet = ScriptableObject.CreateInstance<QuestWorldCatalogSetSO>();
            catalogSet.areaCatalog = areaCatalog;

            var resolver = new QuestWorldResolver(catalogSet);
            Assert.IsTrue(resolver.TryGetArea("zone_a", out AreaCatalogEntry entry));
            Assert.AreEqual("Zone A", entry.displayName);
        }

        static void SeedArea(AreaCatalogSO catalog, string id, string displayName)
        {
            var field = typeof(AreaCatalogSO).GetField("entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(catalog, new List<AreaCatalogEntry>
            {
                new AreaCatalogEntry { id = id, displayName = displayName }
            });
        }
    }
}
