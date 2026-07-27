using NUnit.Framework;
using UnityEngine;

namespace EnglishQuest.Tests.QuestSystem
{
    [TestFixture]
    public class QuestCatalogSOTests
    {
        [Test]
        public void TryGetDefinition_ExistingId_ReturnsDefinition()
        {
            var catalog = ScriptableObject.CreateInstance<QuestCatalogSO>();
            var definition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            definition.id = "q01";
            catalog.definitions.Add(definition);

            bool found = catalog.TryGetDefinition("q01", out QuestDefinitionSO result);

            Assert.IsTrue(found);
            Assert.AreSame(definition, result);
        }

        [Test]
        public void TryGetDefinition_MissingId_ReturnsFalse()
        {
            var catalog = ScriptableObject.CreateInstance<QuestCatalogSO>();

            bool found = catalog.TryGetDefinition("missing", out QuestDefinitionSO result);

            Assert.IsFalse(found);
            Assert.IsNull(result);
        }

        [Test]
        public void TryGetDefinition_NullOrEmptyId_ReturnsFalse()
        {
            var catalog = ScriptableObject.CreateInstance<QuestCatalogSO>();

            Assert.IsFalse(catalog.TryGetDefinition(null, out _));
            Assert.IsFalse(catalog.TryGetDefinition("", out _));
        }
    }
}

