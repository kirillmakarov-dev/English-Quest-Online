using System.Collections.Generic;
using System.Reflection;
using EnglishKingdom.QuestSystem;
using NUnit.Framework;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishKingdom.Tests.QuestSystem
{
    [TestFixture]
    public class QuestLineRegistrarTests
    {
        private readonly List<GameObject> _created = new();
        private readonly List<ScriptableObject> _createdAssets = new();
        private GameObject _serviceLocatorGo;
        private QuestManager _manager;

        [SetUp]
        public void SetUp()
        {
            _serviceLocatorGo = new GameObject("ServiceLocator");
            ServiceLocator locator = QuestSystemTestSupport.CreateServiceLocator(_serviceLocatorGo);

            var managerGo = new GameObject("QuestManager");
            _created.Add(managerGo);
            _manager = managerGo.AddComponent<QuestManager>();
            locator.Register<IQuestService>(_manager);

            typeof(StaticInstance<QuestManager>)
                .GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, _manager);

            QuestSystemTestSupport.SetPrivateField(_manager, "allQuestInfos", new List<QuestInfo>());
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _created.Clear();

            foreach (ScriptableObject asset in _createdAssets)
            {
                if (asset != null)
                    Object.DestroyImmediate(asset);
            }

            _createdAssets.Clear();
            Object.DestroyImmediate(_serviceLocatorGo);

            typeof(StaticInstance<QuestManager>)
                .GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, null);

            QuestSystemTestSupport.ClearGlobalLocator();
        }

        [Test]
        public void Awake_RegistersQuestsWithAuthoringMetadata()
        {
            QuestDefinitionSO definition = QuestSystemTestSupport.CreateDefinition(
                "q01",
                "teacher_maya",
                levelRequired: 4,
                waitForNpcTurnIn: true);
            definition.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition { type = QuestObjectiveType.EnterArea, targetId = "area_a" }
            };

            var line = ScriptableObject.CreateInstance<QuestLineSO>();
            line.lineId = "maya_line";
            line.quests = new List<QuestDefinitionSO> { definition };
            _createdAssets.Add(line);

            QuestLineRegistrar registrar = CreateRegistrarWithLegacyLine(line);
            QuestSystemTestSupport.InvokeAwake(registrar);

            Assert.AreEqual(1, _manager.AllQuests.Count);

            QuestInfo quest = _manager.GetQuestById("q01");
            Assert.NotNull(quest);
            Assert.AreEqual("q01", quest.displayName);
            Assert.IsTrue(quest.waitForNpcTurnIn);
            Assert.AreEqual(QuestState.REQUIREMENTS_NOT_MET, quest.state);
            Assert.AreEqual(4, quest.requirements[0].minPlayerLevel);
            Assert.IsTrue(quest.TryGetComponent(out QuestDefinitionLink link));
            Assert.AreSame(definition, link.Definition);
        }

        [Test]
        public void Awake_FromRegistry_RegistersAllLines()
        {
            QuestDefinitionSO q01 = QuestSystemTestSupport.CreateDefinition("q01", "teacher_maya");
            q01.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition { type = QuestObjectiveType.EnterArea, targetId = "area_a" }
            };
            QuestDefinitionSO q02 = QuestSystemTestSupport.CreateDefinition("q02", "teacher_maya");
            q02.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition { type = QuestObjectiveType.EnterArea, targetId = "area_b" }
            };

            var lineA = ScriptableObject.CreateInstance<QuestLineSO>();
            lineA.lineId = "line_a";
            lineA.quests = new List<QuestDefinitionSO> { q01 };
            var lineB = ScriptableObject.CreateInstance<QuestLineSO>();
            lineB.lineId = "line_b";
            lineB.quests = new List<QuestDefinitionSO> { q02 };
            _createdAssets.Add(lineA);
            _createdAssets.Add(lineB);

            var registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
            registry.questLines = new List<QuestLineSO> { lineA, lineB };
            _createdAssets.Add(registry);

            QuestLineRegistrar registrar = CreateRegistrarWithRegistry(registry);
            QuestSystemTestSupport.InvokeAwake(registrar);

            Assert.AreEqual(2, _manager.AllQuests.Count);
            Assert.NotNull(_manager.GetQuestById("q01"));
            Assert.NotNull(_manager.GetQuestById("q02"));
            Assert.AreSame(registry, registrar.Registry);
        }

        [Test]
        public void Awake_ChainedPrerequisite_BlocksSecondQuestUntilFirstCompletes()
        {
            QuestDefinitionSO q01 = QuestSystemTestSupport.CreateDefinition("q01", "teacher_maya");
            q01.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition { type = QuestObjectiveType.EnterArea, targetId = "area_a" }
            };
            QuestDefinitionSO q02 = QuestSystemTestSupport.CreateDefinition(
                "q02",
                "teacher_maya",
                prerequisiteQuestId: "q01");
            q02.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition { type = QuestObjectiveType.EnterArea, targetId = "area_b" }
            };

            var line = ScriptableObject.CreateInstance<QuestLineSO>();
            line.lineId = "maya_line";
            line.quests = new List<QuestDefinitionSO> { q01, q02 };
            _createdAssets.Add(line);

            QuestLineRegistrar registrar = CreateRegistrarWithLegacyLine(line);
            QuestSystemTestSupport.InvokeAwake(registrar);

            QuestInfo first = _manager.GetQuestById("q01");
            QuestInfo second = _manager.GetQuestById("q02");

            Assert.AreEqual(QuestState.CAN_START, first.state);
            Assert.AreEqual(QuestState.REQUIREMENTS_NOT_MET, second.state);

            first.SetState(QuestState.CAN_FINISH);
            _manager.FinishQuest(first);

            Assert.AreEqual(QuestState.CAN_START, second.state);
        }

        [Test]
        public void Awake_RegistersAvailabilityService()
        {
            QuestDefinitionSO definition = QuestSystemTestSupport.CreateDefinition(
                "q01",
                "teacher_maya");
            definition.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition { type = QuestObjectiveType.EnterArea, targetId = "area_a" }
            };

            var line = ScriptableObject.CreateInstance<QuestLineSO>();
            line.lineId = "maya_line";
            line.quests = new List<QuestDefinitionSO> { definition };
            _createdAssets.Add(line);

            QuestLineRegistrar registrar = CreateRegistrarWithLegacyLine(line);
            QuestSystemTestSupport.InvokeAwake(registrar);

            Assert.IsTrue(
                _serviceLocatorGo.GetComponent<ServiceLocator>().TryGet(out IQuestAvailabilityService availability));
            Assert.IsNotNull(availability);
            Assert.AreEqual(1, availability.GetAvailableToStart("teacher_maya").Count);
        }

        private QuestLineRegistrar CreateRegistrarWithLegacyLine(QuestLineSO line)
        {
            GameObject shell = QuestSystemTestSupport.CreateQuestPrefabTemplate();
            _created.Add(shell);

            var registrarGo = new GameObject("QuestLineRegistrar");
            _created.Add(registrarGo);
            QuestLineRegistrar registrar = registrarGo.AddComponent<QuestLineRegistrar>();
            QuestSystemTestSupport.SetPrivateField(registrar, "questLine", line);
            QuestSystemTestSupport.SetPrivateField(registrar, "questRuntimeShellPrefab", shell);
            return registrar;
        }

        private QuestLineRegistrar CreateRegistrarWithRegistry(QuestLineRegistrySO registry)
        {
            GameObject shell = QuestSystemTestSupport.CreateQuestPrefabTemplate();
            _created.Add(shell);
            registry.questRuntimeShellPrefab = shell;

            var registrarGo = new GameObject("QuestLineRegistrar");
            _created.Add(registrarGo);
            QuestLineRegistrar registrar = registrarGo.AddComponent<QuestLineRegistrar>();
            QuestSystemTestSupport.SetPrivateField(registrar, "registry", registry);
            return registrar;
        }
    }
}
