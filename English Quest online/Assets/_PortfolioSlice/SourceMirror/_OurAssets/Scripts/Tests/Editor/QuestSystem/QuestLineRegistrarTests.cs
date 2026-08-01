using System.Collections.Generic;
using System.Reflection;
using EnglishQuest.QuestSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using UnityServiceLocator;

namespace EnglishQuest.Tests.QuestSystem
{
    [TestFixture]
    public class QuestLineRegistrarTests
    {
        private const string ActualRegistryAssetPath =
            "Assets/_PortfolioSlice/Demo/Data/QuestLines/Shared_MVP_Catalogs/QuestLineRegistry_MVP.asset";

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
        public void Awake_DuplicateLineIds_StopsRegistration()
        {
            QuestDefinitionSO q01 = QuestSystemTestSupport.CreateDefinition("q01", "teacher_maya");
            q01.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition { type = QuestObjectiveType.EnterArea, targetId = "area_a" }
            };
            QuestDefinitionSO q02 = QuestSystemTestSupport.CreateDefinition("q02", "coach_ben");
            q02.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition { type = QuestObjectiveType.EnterArea, targetId = "area_b" }
            };

            var lineA = ScriptableObject.CreateInstance<QuestLineSO>();
            lineA.lineId = "line_duplicate";
            lineA.quests = new List<QuestDefinitionSO> { q01 };
            var lineB = ScriptableObject.CreateInstance<QuestLineSO>();
            lineB.lineId = "line_duplicate";
            lineB.quests = new List<QuestDefinitionSO> { q02 };
            _createdAssets.Add(lineA);
            _createdAssets.Add(lineB);

            var registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
            registry.questLines = new List<QuestLineSO> { lineA, lineB };
            _createdAssets.Add(registry);

            QuestLineRegistrar registrar = CreateRegistrarWithRegistry(registry);

            LogAssert.Expect(LogType.Error, "[QuestLineRegistrar] Duplicate line id 'line_duplicate' detected.");
            QuestSystemTestSupport.InvokeAwake(registrar);

            Assert.AreEqual(0, _manager.AllQuests.Count);
        }

        [Test]
        public void Awake_MissingPrerequisiteLine_StopsRegistration()
        {
            QuestDefinitionSO q01 = QuestSystemTestSupport.CreateDefinition("q01", "coach_ben");
            q01.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition { type = QuestObjectiveType.EnterArea, targetId = "area_a" }
            };

            var line = ScriptableObject.CreateInstance<QuestLineSO>();
            line.lineId = "line_b";
            line.npcId = "coach_ben";
            line.prerequisiteLineId = "missing_line";
            line.quests = new List<QuestDefinitionSO> { q01 };
            _createdAssets.Add(line);

            var registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
            registry.questLines = new List<QuestLineSO> { line };
            _createdAssets.Add(registry);

            QuestLineRegistrar registrar = CreateRegistrarWithRegistry(registry);

            LogAssert.Expect(
                LogType.Error,
                "[QuestLineRegistrar] Quest line 'line_b' references missing prerequisite line 'missing_line'.");
            QuestSystemTestSupport.InvokeAwake(registrar);

            Assert.AreEqual(0, _manager.AllQuests.Count);
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
        public void Awake_LinePrerequisite_BlocksSecondNpcUntilFirstLineIsComplete()
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
            lineA.npcId = "teacher_maya";
            lineA.quests = new List<QuestDefinitionSO> { q01 };

            var lineB = ScriptableObject.CreateInstance<QuestLineSO>();
            lineB.lineId = "line_b";
            lineB.npcId = "teacher_maya";
            lineB.prerequisiteLineId = "line_a";
            lineB.quests = new List<QuestDefinitionSO> { q02 };

            var registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
            registry.questLines = new List<QuestLineSO> { lineA, lineB };
            _createdAssets.Add(lineA);
            _createdAssets.Add(lineB);
            _createdAssets.Add(registry);

            QuestLineRegistrar registrar = CreateRegistrarWithRegistry(registry);
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
        public void Awake_LinePrerequisite_AddsPreviousLineCompletionQuestIdToFirstQuestRequirements()
        {
            QuestDefinitionSO adaQuest = QuestSystemTestSupport.CreateDefinition("q01", "teacher_maya");
            adaQuest.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition { type = QuestObjectiveType.EnterArea, targetId = "area_a" }
            };

            QuestDefinitionSO benQuest = QuestSystemTestSupport.CreateDefinition("q02", "coach_ben");
            benQuest.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition { type = QuestObjectiveType.EnterArea, targetId = "area_b" }
            };

            var lineA = ScriptableObject.CreateInstance<QuestLineSO>();
            lineA.lineId = "line_a";
            lineA.npcId = "teacher_maya";
            lineA.quests = new List<QuestDefinitionSO> { adaQuest };

            var lineB = ScriptableObject.CreateInstance<QuestLineSO>();
            lineB.lineId = "line_b";
            lineB.npcId = "coach_ben";
            lineB.prerequisiteLineId = "line_a";
            lineB.quests = new List<QuestDefinitionSO> { benQuest };

            var registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
            registry.questLines = new List<QuestLineSO> { lineA, lineB };
            _createdAssets.Add(lineA);
            _createdAssets.Add(lineB);
            _createdAssets.Add(registry);

            QuestLineRegistrar registrar = CreateRegistrarWithRegistry(registry);
            QuestSystemTestSupport.InvokeAwake(registrar);

            QuestInfo second = _manager.GetQuestById("q02");

            Assert.IsNotNull(second);
            Assert.IsNotNull(second.requirements);
            Assert.IsNotEmpty(second.requirements);
            CollectionAssert.Contains(second.requirements[0].requiredQuestIds, "q01");
        }

        [Test]
        public void Awake_FirstQuestInLine_PreservesDirectPrerequisiteAndAddsLinePrerequisite()
        {
            QuestDefinitionSO adaQuest = QuestSystemTestSupport.CreateDefinition("q01", "teacher_maya");
            adaQuest.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition { type = QuestObjectiveType.EnterArea, targetId = "area_a" }
            };

            QuestDefinitionSO benQuest = QuestSystemTestSupport.CreateDefinition(
                "q02",
                "coach_ben",
                prerequisiteQuestId: "bonus_gate");
            benQuest.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition { type = QuestObjectiveType.EnterArea, targetId = "area_b" }
            };

            var lineA = ScriptableObject.CreateInstance<QuestLineSO>();
            lineA.lineId = "line_a";
            lineA.npcId = "teacher_maya";
            lineA.quests = new List<QuestDefinitionSO> { adaQuest };

            var lineB = ScriptableObject.CreateInstance<QuestLineSO>();
            lineB.lineId = "line_b";
            lineB.npcId = "coach_ben";
            lineB.prerequisiteLineId = "line_a";
            lineB.quests = new List<QuestDefinitionSO> { benQuest };

            var registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
            registry.questLines = new List<QuestLineSO> { lineA, lineB };
            _createdAssets.Add(lineA);
            _createdAssets.Add(lineB);
            _createdAssets.Add(registry);

            QuestLineRegistrar registrar = CreateRegistrarWithRegistry(registry);
            QuestSystemTestSupport.InvokeAwake(registrar);

            QuestInfo second = _manager.GetQuestById("q02");

            Assert.IsNotNull(second);
            Assert.IsNotNull(second.requirements);
            Assert.IsNotEmpty(second.requirements);
            CollectionAssert.Contains(second.requirements[0].requiredQuestIds, "bonus_gate");
            CollectionAssert.Contains(second.requirements[0].requiredQuestIds, "q01");
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

        [Test]
        public void SoloFirstFlow_FirstNpcCanInteract_WhileSecondNpcStaysLockedUntilPreviousLineCompletes()
        {
            QuestDefinitionSO adaQuest = QuestSystemTestSupport.CreateDefinition("q01", "teacher_ada");
            adaQuest.startDialogue = CreateDialogueNode("ada_start");
            adaQuest.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition { type = QuestObjectiveType.EnterArea, targetId = "area_a" }
            };

            QuestDefinitionSO benQuest = QuestSystemTestSupport.CreateDefinition("q02", "coach_ben");
            benQuest.startDialogue = CreateDialogueNode("ben_start");
            benQuest.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition { type = QuestObjectiveType.EnterArea, targetId = "area_b" }
            };

            var adaLine = ScriptableObject.CreateInstance<QuestLineSO>();
            adaLine.lineId = "line_ada";
            adaLine.npcId = "teacher_ada";
            adaLine.quests = new List<QuestDefinitionSO> { adaQuest };

            var benLine = ScriptableObject.CreateInstance<QuestLineSO>();
            benLine.lineId = "line_ben";
            benLine.npcId = "coach_ben";
            benLine.prerequisiteLineId = "line_ada";
            benLine.quests = new List<QuestDefinitionSO> { benQuest };

            var registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
            registry.questLines = new List<QuestLineSO> { adaLine, benLine };
            _createdAssets.Add(adaLine);
            _createdAssets.Add(benLine);
            _createdAssets.Add(registry);

            QuestLineRegistrar registrar = CreateRegistrarWithRegistry(registry);
            QuestSystemTestSupport.InvokeAwake(registrar);

            NpcQuestGiver adaGiver = CreateNpcQuestGiver("teacher_ada");
            NpcQuestGiver benGiver = CreateNpcQuestGiver("coach_ben");

            QuestInfo firstQuest = _manager.GetQuestById("q01");
            QuestInfo secondQuest = _manager.GetQuestById("q02");

            Assert.AreEqual(QuestState.CAN_START, firstQuest.state);
            Assert.AreEqual(QuestState.REQUIREMENTS_NOT_MET, secondQuest.state);
            Assert.IsTrue(adaGiver.CanInteract, "The first NPC should be usable in a solo session.");
            Assert.IsFalse(benGiver.CanInteract, "The second NPC must stay locked because of quest order, not player count.");

            firstQuest.SetState(QuestState.CAN_FINISH);
            _manager.FinishQuest(firstQuest);

            Assert.AreEqual(QuestState.CAN_START, secondQuest.state);
            Assert.IsTrue(benGiver.CanInteract, "Once the first line is finished, the next NPC should unlock without needing another player.");
        }

        [Test]
        public void SoloFirstFlow_FirstNpcInteraction_StartsQuestWithoutNetworkRunnerOrSecondPlayer()
        {
            QuestDefinitionSO definition = QuestSystemTestSupport.CreateDefinition("q01", "teacher_ada");
            definition.startDialogue = CreateDialogueNode("ada_start");
            definition.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition { type = QuestObjectiveType.EnterArea, targetId = "area_a" }
            };

            var line = ScriptableObject.CreateInstance<QuestLineSO>();
            line.lineId = "line_ada";
            line.npcId = "teacher_ada";
            line.quests = new List<QuestDefinitionSO> { definition };
            _createdAssets.Add(line);

            _serviceLocatorGo
                .GetComponent<ServiceLocator>()
                .Register<IDialogueService>(new StubDialogueService());

            QuestLineRegistrar registrar = CreateRegistrarWithLegacyLine(line);
            QuestSystemTestSupport.InvokeAwake(registrar);

            NpcQuestGiver adaGiver = CreateNpcQuestGiver("teacher_ada");
            QuestInfo firstQuest = _manager.GetQuestById("q01");

            Assert.AreEqual(QuestState.CAN_START, firstQuest.state);
            Assert.IsTrue(adaGiver.CanInteract);

            bool handled = adaGiver.Interact(null);

            Assert.IsTrue(handled);
            Assert.AreEqual(QuestState.IN_PROGRESS, firstQuest.state);
        }

        [Test]
        public void SoloFirstFlow_FullAdaBenNoraChainUnlocksSequentiallyForOnePlayer()
        {
            QuestDefinitionSO adaQuest = CreateOpenWorldQuestDefinition(
                "q_ada_letters",
                "teacher_ada",
                "ada_start",
                "area_ada");

            QuestDefinitionSO benQuest = CreateOpenWorldQuestDefinition(
                "q_ben_missing_letter",
                "coach_ben",
                "ben_start",
                "area_ben");

            QuestDefinitionSO noraQuest = CreateOpenWorldQuestDefinition(
                "q_nora_sentence",
                "guide_nora",
                "nora_start",
                "area_nora");

            var adaLine = CreateLine("line_teacher_ada", "teacher_ada", adaQuest);
            var benLine = CreateLine("line_coach_ben", "coach_ben", benQuest, prerequisiteLineId: "line_teacher_ada");
            var noraLine = CreateLine("line_guide_nora", "guide_nora", noraQuest, prerequisiteLineId: "line_coach_ben");

            var registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
            registry.questLines = new List<QuestLineSO> { adaLine, benLine, noraLine };
            _createdAssets.Add(registry);

            _serviceLocatorGo
                .GetComponent<ServiceLocator>()
                .Register<IDialogueService>(new StubDialogueService());

            QuestLineRegistrar registrar = CreateRegistrarWithRegistry(registry);
            QuestSystemTestSupport.InvokeAwake(registrar);

            NpcQuestGiver adaGiver = CreateNpcQuestGiver("teacher_ada");
            NpcQuestGiver benGiver = CreateNpcQuestGiver("coach_ben");
            NpcQuestGiver noraGiver = CreateNpcQuestGiver("guide_nora");

            QuestInfo ada = _manager.GetQuestById("q_ada_letters");
            QuestInfo ben = _manager.GetQuestById("q_ben_missing_letter");
            QuestInfo nora = _manager.GetQuestById("q_nora_sentence");

            Assert.That(ada.state, Is.EqualTo(QuestState.CAN_START));
            Assert.That(ben.state, Is.EqualTo(QuestState.REQUIREMENTS_NOT_MET));
            Assert.That(nora.state, Is.EqualTo(QuestState.REQUIREMENTS_NOT_MET));
            Assert.IsTrue(adaGiver.CanInteract);
            Assert.IsFalse(benGiver.CanInteract);
            Assert.IsFalse(noraGiver.CanInteract);

            Assert.IsTrue(adaGiver.Interact(null));
            Assert.That(ada.state, Is.EqualTo(QuestState.IN_PROGRESS));

            ada.SetState(QuestState.CAN_FINISH);
            _manager.FinishQuest(ada);

            Assert.That(ada.state, Is.EqualTo(QuestState.FINISHED));
            Assert.That(ben.state, Is.EqualTo(QuestState.CAN_START));
            Assert.That(nora.state, Is.EqualTo(QuestState.REQUIREMENTS_NOT_MET));
            Assert.IsTrue(benGiver.CanInteract);
            Assert.IsFalse(noraGiver.CanInteract);

            Assert.IsTrue(benGiver.Interact(null));
            Assert.That(ben.state, Is.EqualTo(QuestState.IN_PROGRESS));

            ben.SetState(QuestState.CAN_FINISH);
            _manager.FinishQuest(ben);

            Assert.That(ben.state, Is.EqualTo(QuestState.FINISHED));
            Assert.That(nora.state, Is.EqualTo(QuestState.CAN_START));
            Assert.IsTrue(noraGiver.CanInteract);

            Assert.IsTrue(noraGiver.Interact(null));
            Assert.That(nora.state, Is.EqualTo(QuestState.IN_PROGRESS));

            nora.SetState(QuestState.CAN_FINISH);
            _manager.FinishQuest(nora);

            Assert.That(nora.state, Is.EqualTo(QuestState.FINISHED));
            Assert.IsTrue(_manager.IsLevelCompleted, "One player should be able to finish the full MVP lesson chain alone.");
        }

        [Test]
        public void SoloFirstFlow_ActualMvpRegistryAsset_AllowsOnePlayerToUnlockAdaBenNoraSequentially()
        {
            QuestLineRegistrySO actualRegistry = AssetDatabase.LoadAssetAtPath<QuestLineRegistrySO>(ActualRegistryAssetPath);
            Assert.That(actualRegistry, Is.Not.Null, $"Missing MVP registry asset under test: {ActualRegistryAssetPath}");

            QuestLineRegistrySO runtimeRegistry = Object.Instantiate(actualRegistry);
            _createdAssets.Add(runtimeRegistry);

            _serviceLocatorGo
                .GetComponent<ServiceLocator>()
                .Register<IDialogueService>(new StubDialogueService());

            QuestLineRegistrar registrar = CreateRegistrarWithRegistry(runtimeRegistry);
            QuestSystemTestSupport.InvokeAwake(registrar);

            NpcQuestGiver adaGiver = CreateNpcQuestGiver("teacher_ada");
            NpcQuestGiver benGiver = CreateNpcQuestGiver("coach_ben");
            NpcQuestGiver noraGiver = CreateNpcQuestGiver("guide_nora");

            QuestInfo ada = _manager.GetQuestById("quest_teacher_ada_letters");
            QuestInfo ben = _manager.GetQuestById("quest_coach_ben_missing_letter");
            QuestInfo nora = _manager.GetQuestById("quest_guide_nora_choose_word");

            Assert.That(ada, Is.Not.Null, "Teacher Ada quest should exist in the committed MVP registry.");
            Assert.That(ben, Is.Not.Null, "Coach Ben quest should exist in the committed MVP registry.");
            Assert.That(nora, Is.Not.Null, "Guide Nora quest should exist in the committed MVP registry.");

            Assert.That(ada.state, Is.EqualTo(QuestState.CAN_START));
            Assert.That(ben.state, Is.EqualTo(QuestState.REQUIREMENTS_NOT_MET));
            Assert.That(nora.state, Is.EqualTo(QuestState.REQUIREMENTS_NOT_MET));
            Assert.IsTrue(adaGiver.CanInteract, "Teacher Ada should be available immediately in the actual MVP asset flow.");
            Assert.IsFalse(benGiver.CanInteract, "Coach Ben should stay locked until Ada is completed in the actual MVP asset flow.");
            Assert.IsFalse(noraGiver.CanInteract, "Guide Nora should stay locked until Ben is completed in the actual MVP asset flow.");

            Assert.IsTrue(adaGiver.Interact(null));
            Assert.That(ada.state, Is.EqualTo(QuestState.IN_PROGRESS));

            ada.SetState(QuestState.CAN_FINISH);
            _manager.FinishQuest(ada);

            Assert.That(ada.state, Is.EqualTo(QuestState.FINISHED));
            Assert.That(ben.state, Is.EqualTo(QuestState.CAN_START));
            Assert.That(nora.state, Is.EqualTo(QuestState.REQUIREMENTS_NOT_MET));
            Assert.IsTrue(benGiver.CanInteract);
            Assert.IsFalse(noraGiver.CanInteract);

            Assert.IsTrue(benGiver.Interact(null));
            Assert.That(ben.state, Is.EqualTo(QuestState.IN_PROGRESS));

            ben.SetState(QuestState.CAN_FINISH);
            _manager.FinishQuest(ben);

            Assert.That(ben.state, Is.EqualTo(QuestState.FINISHED));
            Assert.That(nora.state, Is.EqualTo(QuestState.CAN_START));
            Assert.IsTrue(noraGiver.CanInteract);

            Assert.IsTrue(noraGiver.Interact(null));
            Assert.That(nora.state, Is.EqualTo(QuestState.IN_PROGRESS));

            nora.SetState(QuestState.CAN_FINISH);
            _manager.FinishQuest(nora);

            Assert.That(nora.state, Is.EqualTo(QuestState.FINISHED));
            Assert.IsTrue(_manager.IsLevelCompleted, "The committed MVP registry should still allow one player to finish the full Ada -> Ben -> Nora flow alone.");
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

        private DialogueNode CreateDialogueNode(string name)
        {
            var node = ScriptableObject.CreateInstance<DialogueNode>();
            node.name = name;
            _createdAssets.Add(node);
            return node;
        }

        private QuestDefinitionSO CreateOpenWorldQuestDefinition(
            string id,
            string npcId,
            string dialogueName,
            string areaId)
        {
            QuestDefinitionSO definition = QuestSystemTestSupport.CreateDefinition(id, npcId);
            definition.startDialogue = CreateDialogueNode(dialogueName);
            definition.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition
                {
                    type = QuestObjectiveType.EnterArea,
                    targetId = areaId
                }
            };
            return definition;
        }

        private QuestLineSO CreateLine(
            string lineId,
            string npcId,
            QuestDefinitionSO quest,
            string prerequisiteLineId = null)
        {
            var line = ScriptableObject.CreateInstance<QuestLineSO>();
            line.lineId = lineId;
            line.npcId = npcId;
            line.prerequisiteLineId = prerequisiteLineId;
            line.quests = new List<QuestDefinitionSO> { quest };
            _createdAssets.Add(line);
            return line;
        }

        private NpcQuestGiver CreateNpcQuestGiver(string npcId)
        {
            var npcGo = new GameObject($"NPC_{npcId}");
            _created.Add(npcGo);

            NpcQuestGiver giver = npcGo.AddComponent<NpcQuestGiver>();
            QuestSystemTestSupport.SetPrivateField(giver, "npcId", npcId);
            QuestSystemTestSupport.InvokeAwake(giver);
            return giver;
        }

        private sealed class StubDialogueService : IDialogueService
        {
            public event System.Action OnDialogueStart;
            public event System.Action OnDialogueEnd;
            public bool IsDialogueActive => false;

            public void StartDialogue(
                DialogueNode startNode,
                Transform speaker = null,
                Transform localPlayer = null,
                Sprite speakerSprite = null,
                string speakerName = "")
            {
                OnDialogueStart?.Invoke();
            }
        }
    }
}

