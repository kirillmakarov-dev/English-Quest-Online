using System.Collections.Generic;
using EnglishKingdom.QuestSystem;
using NUnit.Framework;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishKingdom.Tests.QuestSystem
{
    [TestFixture]
    public class GuiderQuestManagerTests
    {
        private readonly List<Object> _created = new();
        private GameObject _serviceLocatorGo;
        private QuestManager _manager;

        [SetUp]
        public void SetUp()
        {
            _serviceLocatorGo = new GameObject("ServiceLocator");
            _created.Add(_serviceLocatorGo);
            ServiceLocator locator = QuestSystemTestSupport.CreateServiceLocator(_serviceLocatorGo);

            var managerGo = new GameObject("QuestManager");
            _created.Add(managerGo);
            _manager = managerGo.AddComponent<QuestManager>();
            locator.Register<IQuestService>(_manager);

            typeof(StaticInstance<QuestManager>)
                .GetField("_instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .SetValue(null, _manager);

            QuestSystemTestSupport.SetPrivateField(_manager, "allQuestInfos", new List<QuestInfo>());
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in _created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }

            _created.Clear();
            QuestSystemTestSupport.ClearGlobalLocator();

            typeof(StaticInstance<QuestManager>)
                .GetField("_instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .SetValue(null, null);
        }

        [Test]
        public void Guider_ForceStartQuest_BypassesRequirementsAndStartsQuest()
        {
            QuestDefinitionSO definition = QuestSystemTestSupport.CreateDefinition(
                "q_test_locked",
                "npc_test",
                levelRequired: 30);
            definition.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition { type = QuestObjectiveType.TalkToNpc, targetId = "npc_test" }
            };
            _created.Add(definition);

            QuestInfo quest = QuestSystemTestSupport.CreateObjectiveQuestWithDefinition(definition);
            _created.Add(quest.gameObject);
            quest.InitializeQuest();

            _manager.RegisterQuest(quest);

            Assert.That(quest.state, Is.EqualTo(QuestState.REQUIREMENTS_NOT_MET));

            _manager.Guider_ForceStartQuest(quest);

            Assert.That(quest.state, Is.EqualTo(QuestState.IN_PROGRESS));
        }

        [Test]
        public void GetOpenWorldQuests_ReturnsOnlyObjectiveQuests()
        {
            QuestDefinitionSO openWorldDefinition = QuestSystemTestSupport.CreateDefinition("q_open", "npc_a");
            openWorldDefinition.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition { type = QuestObjectiveType.TalkToNpc, targetId = "npc_a" }
            };
            _created.Add(openWorldDefinition);

            QuestInfo openWorldQuest = QuestSystemTestSupport.CreateObjectiveQuestWithDefinition(openWorldDefinition);
            _created.Add(openWorldQuest.gameObject);

            QuestInfo legacyQuest = QuestSystemTestSupport.CreateQuestInfo(
                "legacy_1",
                QuestState.CAN_START,
                steps: new List<QuestStep>
                {
                    new GameObject("legacy_step").AddComponent<QuestStep_TalkToNPC>()
                });
            _created.Add(legacyQuest.gameObject);
            _created.Add(legacyQuest.questSteps[0].gameObject);

            _manager.RegisterQuest(openWorldQuest);
            _manager.RegisterQuest(legacyQuest);

            IReadOnlyList<QuestInfo> openWorldQuests = _manager.GetOpenWorldQuests();

            Assert.That(openWorldQuests.Count, Is.EqualTo(1));
            Assert.That(openWorldQuests[0].id, Is.EqualTo("q_open"));
        }
    }
}
