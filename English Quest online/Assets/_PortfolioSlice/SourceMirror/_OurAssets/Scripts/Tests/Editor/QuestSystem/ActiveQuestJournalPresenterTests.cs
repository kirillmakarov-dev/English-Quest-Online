using System.Collections.Generic;
using EnglishKingdom.QuestSystem;
using NUnit.Framework;
using UnityEngine;

namespace EnglishKingdom.Tests.QuestSystem
{
    public class ActiveQuestJournalPresenterTests
    {
        StubQuestService _questService;
        QuestWorldResolver _worldResolver;
        NpcCatalogSO _npcCatalog;

        [SetUp]
        public void SetUp()
        {
            _questService = new StubQuestService();
            _npcCatalog = ScriptableObject.CreateInstance<NpcCatalogSO>();
            QuestSystemTestSupport.SetPrivateField(_npcCatalog, "entries", new List<NpcCatalogEntry>
            {
                new NpcCatalogEntry { id = "teacher_maya", displayName = "Teacher Maya" }
            });

            var catalogSet = ScriptableObject.CreateInstance<QuestWorldCatalogSetSO>();
            QuestSystemTestSupport.SetPrivateField(catalogSet, "npcCatalog", _npcCatalog);
            _worldResolver = new QuestWorldResolver(catalogSet);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_npcCatalog);
        }

        [Test]
        public void BuildEntry_InProgress_UsesObjectiveDisplayText()
        {
            var definition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            definition.id = "q_test";
            definition.displayName = "Test Quest";
            definition.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition
                {
                    type = QuestObjectiveType.EnterArea,
                    targetId = "zone_a",
                    displayText = "Find the runaway letter A"
                }
            };

            QuestInfo quest = QuestSystemTestSupport.CreateObjectiveQuestWithDefinition(definition);
            quest.displayName = definition.displayName;
            quest.InitializeQuest();
            quest.SetState(QuestState.IN_PROGRESS);

            ActiveQuestJournalEntry entry = ActiveQuestDisplayHelper.BuildEntry(quest, _questService, _worldResolver);

            Assert.AreEqual("Test Quest", entry.DisplayName);
            Assert.AreEqual(QuestState.IN_PROGRESS, entry.State);
            Assert.AreEqual("Find the runaway letter A", entry.ObjectiveText);
            Assert.AreEqual(string.Empty, entry.ProgressText);
        }

        [Test]
        public void BuildEntry_CanFinish_ShowsTurnInTextWithNpcName()
        {
            var definition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            definition.id = "q_turn_in";
            definition.giverNpcId = "teacher_maya";
            definition.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition { type = QuestObjectiveType.TalkToNpc, targetId = "teacher_maya" }
            };

            QuestInfo quest = QuestSystemTestSupport.CreateObjectiveQuestWithDefinition(definition);
            quest.InitializeQuest();
            quest.SetState(QuestState.CAN_FINISH);

            ActiveQuestJournalEntry entry = ActiveQuestDisplayHelper.BuildEntry(quest, _questService, _worldResolver);

            Assert.AreEqual(QuestState.CAN_FINISH, entry.State);
            Assert.AreEqual("חזרו למסור (Teacher Maya)", entry.ObjectiveText);
        }

        [Test]
        public void BuildProgressText_ShowsFractionWhenTargetGreaterThanOne()
        {
            var definition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            definition.id = "q_collect";
            definition.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition
                {
                    type = QuestObjectiveType.Collect,
                    targetId = "apple",
                    count = 5,
                    displayText = "Collect apples"
                }
            };

            QuestInfo quest = QuestSystemTestSupport.CreateObjectiveQuestWithDefinition(definition);
            quest.InitializeQuest();
            quest.SetState(QuestState.IN_PROGRESS);
            _questService.ReportObjectiveProgress(quest, 0, 2, 5);

            ActiveQuestJournalEntry entry = ActiveQuestDisplayHelper.BuildEntry(quest, _questService, _worldResolver);

            Assert.AreEqual("2/5", entry.ProgressText);
        }

        [Test]
        public void IsActiveJournalState_IncludesInProgressAndCanFinishOnly()
        {
            Assert.IsTrue(ActiveQuestDisplayHelper.IsActiveJournalState(QuestState.IN_PROGRESS));
            Assert.IsTrue(ActiveQuestDisplayHelper.IsActiveJournalState(QuestState.CAN_FINISH));
            Assert.IsFalse(ActiveQuestDisplayHelper.IsActiveJournalState(QuestState.CAN_START));
            Assert.IsFalse(ActiveQuestDisplayHelper.IsActiveJournalState(QuestState.FINISHED));
        }
    }
}
