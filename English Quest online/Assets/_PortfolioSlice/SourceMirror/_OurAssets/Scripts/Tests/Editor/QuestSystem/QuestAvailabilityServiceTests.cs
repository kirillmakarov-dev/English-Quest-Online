using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishKingdom.Tests.QuestSystem
{
    [TestFixture]
    public class QuestAvailabilityServiceTests
    {
        private GameObject _serviceGo;
        private QuestAvailabilityService _service;
        private StubQuestService _questService;
        private GameObject _serviceLocatorGo;
        private Dictionary<QuestInfo, QuestDefinitionSO> _definitionMap;

        [SetUp]
        public void SetUp()
        {
            _serviceLocatorGo = new GameObject("ServiceLocator");
            ServiceLocator locator = QuestSystemTestSupport.CreateServiceLocator(_serviceLocatorGo);

            _questService = new StubQuestService();
            locator.Register<IQuestService>(_questService);

            _serviceGo = new GameObject("QuestAvailabilityService");
            _service = _serviceGo.AddComponent<QuestAvailabilityService>();

            var definitionA = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            definitionA.id = "q01";
            definitionA.giverNpcId = "teacher_maya";

            var definitionB = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            definitionB.id = "q02";
            definitionB.giverNpcId = "teacher_maya";

            var questA = CreateQuest("q01", QuestState.CAN_START);
            var questB = CreateQuest("q02", QuestState.IN_PROGRESS);

            _questService.Quests.Add(questA);
            _questService.Quests.Add(questB);

            var map = new Dictionary<QuestInfo, QuestDefinitionSO>
            {
                { questA, definitionA },
                { questB, definitionB },
            };
            _definitionMap = map;

            _service.Initialize(null, map);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_serviceGo);
            Object.DestroyImmediate(_serviceLocatorGo);
            QuestSystemTestSupport.ClearGlobalLocator();
        }

        [Test]
        public void GetAvailableToStart_ReturnsMatchingNpcQuests()
        {
            IReadOnlyList<QuestInfo> available = _service.GetAvailableToStart("teacher_maya");
            Assert.AreEqual(1, available.Count);
            Assert.AreEqual("q01", available[0].id);
        }

        [Test]
        public void GetBestIndicator_PrefersTurnInOverAvailable()
        {
            var turnInDefinition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            turnInDefinition.id = "q03";
            turnInDefinition.giverNpcId = "teacher_maya";

            QuestInfo turnInQuest = CreateQuest("q03", QuestState.CAN_FINISH);
            _questService.Quests.Add(turnInQuest);

            var map = new Dictionary<QuestInfo, QuestDefinitionSO>
            {
                { _questService.Quests[0], ScriptableObject.CreateInstance<QuestDefinitionSO>() },
                { _questService.Quests[1], ScriptableObject.CreateInstance<QuestDefinitionSO>() },
                { turnInQuest, turnInDefinition },
            };
            map[_questService.Quests[0]].giverNpcId = "teacher_maya";
            map[_questService.Quests[1]].giverNpcId = "teacher_maya";

            _service.Initialize(null, map);

            Assert.AreEqual(QuestNpcIndicatorState.TurnIn, _service.GetBestIndicator("teacher_maya"));
        }

        [Test]
        public void GetInProgress_ReturnsMatchingNpcQuests()
        {
            IReadOnlyList<QuestInfo> inProgress = _service.GetInProgress("teacher_maya");

            Assert.AreEqual(1, inProgress.Count);
            Assert.AreEqual("q02", inProgress[0].id);
        }

        [Test]
        public void GetReadyToTurnIn_ReturnsMatchingNpcQuests()
        {
            QuestInfo turnInQuest = CreateQuest("q03", QuestState.CAN_FINISH);
            _questService.Quests.Add(turnInQuest);

            var definition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            definition.id = "q03";
            definition.giverNpcId = "teacher_maya";

            var map = new Dictionary<QuestInfo, QuestDefinitionSO>
            {
                { _questService.Quests[0], ScriptableObject.CreateInstance<QuestDefinitionSO>() },
                { _questService.Quests[1], ScriptableObject.CreateInstance<QuestDefinitionSO>() },
                { turnInQuest, definition },
            };
            map[_questService.Quests[0]].giverNpcId = "teacher_maya";
            map[_questService.Quests[1]].giverNpcId = "teacher_maya";

            _service.Initialize(null, map);

            IReadOnlyList<QuestInfo> ready = _service.GetReadyToTurnIn("teacher_maya");
            Assert.AreEqual(1, ready.Count);
            Assert.AreEqual("q03", ready[0].id);
        }

        [Test]
        public void GetBestIndicator_InProgressWhenNoTurnInOrAvailable()
        {
            _questService.Quests.Clear();
            QuestInfo quest = CreateQuest("q02", QuestState.IN_PROGRESS);
            _questService.Quests.Add(quest);

            var definition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            definition.id = "q02";
            definition.giverNpcId = "teacher_maya";
            _service.Initialize(null, new Dictionary<QuestInfo, QuestDefinitionSO> { { quest, definition } });

            Assert.AreEqual(QuestNpcIndicatorState.InProgress, _service.GetBestIndicator("teacher_maya"));
        }

        [Test]
        public void CanInteractWithNpc_ReturnsFalse_WhenNoQuestIsVisible()
        {
            _questService.Quests.Clear();

            Assert.IsFalse(_service.CanInteractWithNpc("teacher_maya"));
        }

        [Test]
        public void GetBestIndicator_ReturnsLocked_WhenQuestRequirementsAreNotMet()
        {
            _questService.Quests.Clear();
            QuestInfo quest = CreateQuest("q_locked", QuestState.REQUIREMENTS_NOT_MET);
            _questService.Quests.Add(quest);

            var definition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            definition.id = "q_locked";
            definition.giverNpcId = "teacher_maya";
            _service.Initialize(null, new Dictionary<QuestInfo, QuestDefinitionSO> { { quest, definition } });

            Assert.AreEqual(QuestNpcIndicatorState.Locked, _service.GetBestIndicator("teacher_maya"));
            Assert.IsFalse(_service.CanInteractWithNpc("teacher_maya"));
        }

        [Test]
        public void CanInteractWithNpc_ReturnsTrue_WhenQuestIsAvailable()
        {
            Assert.IsTrue(_service.CanInteractWithNpc("teacher_maya"));
        }

        [Test]
        public void TryGetDefinition_FromCatalog_FallsBackWhenNotInMap()
        {
            var catalog = ScriptableObject.CreateInstance<QuestCatalogSO>();
            var definition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            definition.id = "catalog_quest";
            catalog.definitions.Add(definition);

            QuestInfo quest = CreateQuest("catalog_quest", QuestState.CAN_START);
            _questService.Quests.Add(quest);
            _service.Initialize(catalog, new Dictionary<QuestInfo, QuestDefinitionSO>());

            bool found = _service.TryGetDefinition(quest, out QuestDefinitionSO result);

            Assert.IsTrue(found);
            Assert.AreSame(definition, result);
        }

        [Test]
        public void TryGetDefinition_FromQuestDefinitionLink_WhenNotInMapOrCatalog()
        {
            QuestInfo quest = CreateQuest("linked_quest", QuestState.CAN_START);
            _questService.Quests.Add(quest);

            var definition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            definition.id = "linked_quest";
            quest.gameObject.AddComponent<QuestDefinitionLink>().SetDefinition(definition);

            _service.Initialize(null, new Dictionary<QuestInfo, QuestDefinitionSO>());

            bool found = _service.TryGetDefinition(quest, out QuestDefinitionSO result);

            Assert.IsTrue(found);
            Assert.AreSame(definition, result);
        }

        private static QuestInfo CreateQuest(string id, QuestState state)
        {
            return QuestSystemTestSupport.CreateQuestInfo(id, state);
        }
    }
}
