using System.Collections.Generic;
using System.Reflection;
using EnglishQuest.QuestSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityServiceLocator;

namespace EnglishQuest.Tests.QuestSystem
{
    [TestFixture]
    public class QuestObjectiveManagerTests
    {
        private GameObject _managerGo;
        private QuestManager _manager;
        private GameObject _serviceLocatorGo;
        private List<GameObject> _created;
        private QuestObjectiveEventBus _eventBus;

        [SetUp]
        public void SetUp()
        {
            _created = new List<GameObject>();

            _serviceLocatorGo = new GameObject("ServiceLocator [Global] (Test)");
            _created.Add(_serviceLocatorGo);
            var locator = _serviceLocatorGo.AddComponent<ServiceLocator>();
            typeof(ServiceLocator)
                .GetField("global", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, locator);

            _managerGo = new GameObject("QuestManager_Test");
            _created.Add(_managerGo);
            _manager = _managerGo.AddComponent<QuestManager>();
            QuestSystemTestSupport.InvokeAwake(_manager);

            typeof(StaticInstance<QuestManager>)
                .GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, _manager);
            locator.Register<IQuestService>(_manager);

            var busGo = new GameObject("EventBus");
            _created.Add(busGo);
            _eventBus = busGo.AddComponent<QuestObjectiveEventBus>();
            locator.Register<IQuestObjectiveEventBus>(_eventBus);

            var catalogSet = ScriptableObject.CreateInstance<QuestWorldCatalogSetSO>();
            catalogSet.areaCatalog = ScriptableObject.CreateInstance<AreaCatalogSO>();
            typeof(AreaCatalogSO)
                .GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(catalogSet.areaCatalog, new List<AreaCatalogEntry>
                {
                    new AreaCatalogEntry { id = "test_area", displayName = "Test Area" }
                });
            catalogSet.interactableCatalog = QuestSystemTestSupport.CreateInteractableCatalog(
                new InteractableCatalogEntry
                {
                    id = "test_minigame",
                    kind = InteractableCatalogKind.MiniGame,
                    displayName = "Test Mini Game"
                });

            var resolver = new QuestWorldResolver(catalogSet);
            locator.Register<IQuestWorldResolver>(resolver);
            _manager.ConfigureObjectiveRuntime(resolver);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            typeof(StaticInstance<QuestManager>)
                .GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, null);
            typeof(ServiceLocator)
                .GetField("global", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, null);
        }

        [Test]
        public void ObjectiveQuest_AreaEntered_AdvancesStep()
        {
            QuestInfo quest = CreateObjectiveQuest("obj_quest", "test_area");
            _manager.RegisterQuest(quest);
            _manager.StartQuest(quest);

            _eventBus.Publish(new QuestObjectiveEvents.AreaEntered("test_area"));

            Assert.AreEqual(QuestState.FINISHED, quest.state);
        }

        [Test]
        public void ObjectiveQuest_UnknownArea_DoesNotAdvance()
        {
            QuestInfo quest = CreateObjectiveQuest("obj_quest", "test_area");
            _manager.RegisterQuest(quest);
            _manager.StartQuest(quest);

            _eventBus.Publish(new QuestObjectiveEvents.AreaEntered("wrong_area"));

            Assert.AreEqual(QuestState.IN_PROGRESS, quest.state);
            Assert.AreEqual(0, quest.currentStepIndex);
        }

        [Test]
        public void QuestInfo_ProgressEntry_RoundTrips()
        {
            QuestInfo quest = CreateObjectiveQuest("save_quest", "test_area");
            quest.InitializeQuest();
            quest.SetState(QuestState.IN_PROGRESS);
            _manager.ReportObjectiveProgress(quest, 0, 1, 1);

            QuestProgressEntry saved = quest.CreateProgressEntry();
            quest.InitializeQuest();
            quest.ApplyProgressEntry(saved);

            Assert.AreEqual(QuestState.IN_PROGRESS, quest.state);
            Assert.AreEqual(1, quest.GetObjectiveProgress(0).Current);
        }

        [Test]
        public void BinderRefresh_ExposesActiveMiniGameConfig()
        {
            LetterOrderingQuestConfigSO config =
                QuestSystemTestSupport.CreateLetterOrderingQuestConfig("test_minigame");
            QuestInfo quest = CreateMiniGameObjectiveQuest("minigame_quest", "test_minigame", config);
            _manager.RegisterQuest(quest);
            _manager.StartQuest(quest);

            Assert.IsTrue(
                ServiceLocator.For(_manager).TryGet(out IQuestMiniGameBinder binder));
            Assert.IsTrue(binder.TryGetConfig("test_minigame", out QuestMiniGameConfigSO resolved));
            Assert.AreSame(config, resolved);
        }

        [Test]
        public void BinderClearsOnAdvance_RemovesConfigAfterMiniGameStepCompletes()
        {
            LetterOrderingQuestConfigSO config =
                QuestSystemTestSupport.CreateLetterOrderingQuestConfig("test_minigame");
            QuestInfo quest = CreateTwoStepMiniGameThenAreaQuest("two_step_quest", "test_minigame", config);
            _manager.RegisterQuest(quest);
            _manager.StartQuest(quest);

            Assert.IsTrue(ServiceLocator.For(_manager).TryGet(out IQuestMiniGameBinder binder));
            Assert.IsTrue(binder.TryGetConfig("test_minigame", out _));

            _eventBus.Publish(new QuestObjectiveEvents.MiniGameCompleted("test_minigame"));

            Assert.AreEqual(1, quest.currentStepIndex);
            Assert.IsFalse(binder.TryGetConfig("test_minigame", out _));
        }

        [Test]
        public void MiniGameCompleted_AdvancesQuest()
        {
            LetterOrderingQuestConfigSO config =
                QuestSystemTestSupport.CreateLetterOrderingQuestConfig("test_minigame");
            QuestInfo quest = CreateMiniGameObjectiveQuest("minigame_finish_quest", "test_minigame", config);
            _manager.RegisterQuest(quest);
            _manager.StartQuest(quest);

            _eventBus.Publish(new QuestObjectiveEvents.MiniGameCompleted("test_minigame"));

            Assert.AreEqual(QuestState.FINISHED, quest.state);
        }

        static QuestInfo CreateObjectiveQuest(string id, string areaId)
        {
            var definition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            definition.id = id;
            definition.waitForNpcTurnIn = false;
            definition.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition
                {
                    type = QuestObjectiveType.EnterArea,
                    targetId = areaId,
                    count = 1,
                    displayText = "Go"
                }
            };

            var go = new GameObject("Quest");
            var quest = go.AddComponent<QuestInfo>();
            var link = go.AddComponent<QuestDefinitionLink>();
            link.SetDefinition(definition);
            quest.id = id;
            quest.questSteps = new List<QuestStep>();
            return quest;
        }

        static QuestInfo CreateMiniGameObjectiveQuest(string id, string gameId, QuestMiniGameConfigSO config)
        {
            var definition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            definition.id = id;
            definition.waitForNpcTurnIn = false;
            definition.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition
                {
                    type = QuestObjectiveType.CompleteMiniGame,
                    targetId = gameId,
                    count = 1,
                    displayText = "Play",
                    miniGameConfig = config
                }
            };

            return QuestSystemTestSupport.CreateObjectiveQuestWithDefinition(definition);
        }

        static QuestInfo CreateTwoStepMiniGameThenAreaQuest(string id, string gameId, QuestMiniGameConfigSO config)
        {
            var definition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            definition.id = id;
            definition.waitForNpcTurnIn = false;
            definition.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition
                {
                    type = QuestObjectiveType.CompleteMiniGame,
                    targetId = gameId,
                    count = 1,
                    displayText = "Play",
                    miniGameConfig = config
                },
                new QuestObjectiveDefinition
                {
                    type = QuestObjectiveType.EnterArea,
                    targetId = "test_area",
                    count = 1,
                    displayText = "Go"
                }
            };

            return QuestSystemTestSupport.CreateObjectiveQuestWithDefinition(definition);
        }
    }
}

