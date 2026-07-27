using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace EnglishQuest.Tests.QuestSystem
{
    [TestFixture]
    public class QuestCatalogSyncTests
    {
        [Test]
        public void BuildSyncedDefinitions_PreservesLineOrderAndDedupes()
        {
            var line = ScriptableObject.CreateInstance<QuestLineSO>();
            var first = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            first.id = "q01";
            var duplicate = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            duplicate.id = "q01";
            var second = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            second.id = "q02";

            line.quests = new List<QuestDefinitionSO> { first, duplicate, second, null };

            List<QuestDefinitionSO> synced = QuestCatalogSync.BuildSyncedDefinitions(line);

            Assert.AreEqual(2, synced.Count);
            Assert.AreSame(first, synced[0]);
            Assert.AreSame(second, synced[1]);
        }

        [Test]
        public void BuildSyncedDefinitions_NullLine_ReturnsEmptyList()
        {
            List<QuestDefinitionSO> synced = QuestCatalogSync.BuildSyncedDefinitions(null);
            Assert.AreEqual(0, synced.Count);
        }
    }
}

