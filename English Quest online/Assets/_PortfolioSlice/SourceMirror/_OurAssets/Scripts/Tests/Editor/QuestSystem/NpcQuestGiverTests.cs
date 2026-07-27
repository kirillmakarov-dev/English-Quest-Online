using System;
using System.Collections.Generic;
using EnglishQuest.QuestSystem;
using NUnit.Framework;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishQuest.Tests.QuestSystem
{
    [TestFixture]
    public class NpcQuestGiverTests
    {
        private GameObject _serviceLocatorGo;
        private GameObject _npcGo;
        private GameObject _eventBusGo;
        private StubQuestService _questService;
        private QuestAvailabilityService _availabilityService;
        private StubDialogueService _dialogueService;
        private NpcQuestGiver _giver;
        private QuestObjectiveHandlerRegistry _objectiveRegistry;
        private Dictionary<QuestInfo, QuestDefinitionSO> _definitionMap;
        private readonly List<UnityEngine.Object> _ownedObjects = new();

        [SetUp]
        public void SetUp()
        {
            _serviceLocatorGo = new GameObject("ServiceLocator");
            ServiceLocator locator = QuestSystemTestSupport.CreateServiceLocator(_serviceLocatorGo);

            _questService = new StubQuestService();
            locator.Register<IQuestService>(_questService);

            _availabilityService = new GameObject("Availability").AddComponent<QuestAvailabilityService>();
            locator.Register<IQuestAvailabilityService>(_availabilityService);

            _dialogueService = new StubDialogueService();
            locator.Register<IDialogueService>(_dialogueService);

            _eventBusGo = new GameObject("EventBus");
            var eventBus = _eventBusGo.AddComponent<QuestObjectiveEventBus>();
            QuestSystemTestSupport.InvokeAwake(eventBus);
            locator.Register<IQuestObjectiveEventBus>(eventBus);

            var resolver = new StubNpcResolver("teacher_maya", "baker_bob");
            locator.Register<IQuestWorldResolver>(resolver);

            _objectiveRegistry = new QuestObjectiveHandlerRegistry(resolver, _questService);
            eventBus.OnNpcInteracted += _objectiveRegistry.HandleNpcInteracted;

            _npcGo = new GameObject("Npc");
            _giver = _npcGo.AddComponent<NpcQuestGiver>();
            QuestSystemTestSupport.SetPrivateField(_giver, "npcId", "teacher_maya");
            _definitionMap = new Dictionary<QuestInfo, QuestDefinitionSO>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object owned in _ownedObjects)
            {
                if (owned != null)
                    UnityEngine.Object.DestroyImmediate(owned);
            }
            _ownedObjects.Clear();

            if (_npcGo != null)
                UnityEngine.Object.DestroyImmediate(_npcGo);
            if (_eventBusGo != null)
                UnityEngine.Object.DestroyImmediate(_eventBusGo);
            if (_availabilityService != null)
                UnityEngine.Object.DestroyImmediate(_availabilityService.gameObject);
            if (_serviceLocatorGo != null)
                UnityEngine.Object.DestroyImmediate(_serviceLocatorGo);
            QuestSystemTestSupport.ClearGlobalLocator();
        }

        [Test]
        public void Interact_TurnInQuest_FinishesQuest()
        {
            QuestInfo quest = SetupQuest("q01", QuestState.CAN_FINISH, "teacher_maya");

            bool handled = _giver.Interact(null);

            Assert.IsTrue(handled);
            Assert.AreEqual(QuestState.FINISHED, quest.state);
        }

        [Test]
        public void Interact_SingleAvailableQuest_StartsQuest()
        {
            QuestInfo quest = SetupQuest("q01", QuestState.CAN_START, "teacher_maya");

            bool handled = _giver.Interact(null);

            Assert.IsTrue(handled);
            Assert.AreEqual(QuestState.IN_PROGRESS, quest.state);
        }

        [Test]
        public void Interact_InProgressQuest_DoesNotStartAnotherQuest()
        {
            QuestInfo quest = SetupQuest("q01", QuestState.IN_PROGRESS, "teacher_maya");

            bool handled = _giver.Interact(null);

            Assert.IsFalse(handled);
            Assert.AreEqual(QuestState.IN_PROGRESS, quest.state);
        }

        [Test]
        public void Interact_NoQuestsAvailable_ReturnsFalse()
        {
            bool handled = _giver.Interact(null);

            Assert.IsFalse(handled);
        }

        [Test]
        public void CanInteract_NoVisibleQuests_ReturnsFalse()
        {
            Assert.IsFalse(_giver.CanInteract);
        }

        [Test]
        public void Interact_TurnInTakesPriorityOverAvailable()
        {
            QuestInfo turnIn = SetupQuest("q_turnin", QuestState.CAN_FINISH, "teacher_maya");
            QuestInfo available = SetupQuest("q_available", QuestState.CAN_START, "teacher_maya");

            _giver.Interact(null);

            Assert.AreEqual(QuestState.FINISHED, turnIn.state);
            Assert.AreEqual(QuestState.CAN_START, available.state);
        }

        [Test]
        public void Interact_ActiveTalkToNpc_PlaysObjectiveDialogue()
        {
            DialogueNode stepDialogue = CreateDialogue("step_talk");
            DialogueNode inProgressDialogue = CreateDialogue("quest_in_progress");

            QuestInfo quest = SetupObjectiveQuest(
                "q_talk",
                "teacher_maya",
                QuestState.IN_PROGRESS,
                new QuestObjectiveDefinition
                {
                    type = QuestObjectiveType.TalkToNpc,
                    targetId = "teacher_maya",
                    dialogue = stepDialogue
                },
                definition => definition.inProgressDialogue = inProgressDialogue);

            bool handled = _giver.Interact(null);

            Assert.IsTrue(handled);
            Assert.AreSame(stepDialogue, _dialogueService.LastNode);
            Assert.AreNotSame(inProgressDialogue, _dialogueService.LastNode);
            Assert.AreEqual(QuestState.CAN_FINISH, quest.state);
        }

        [Test]
        public void Interact_TalkToNpc_NonGiverTarget_PlaysObjectiveDialogue()
        {
            DialogueNode stepDialogue = CreateDialogue("baker_step");
            QuestSystemTestSupport.SetPrivateField(_giver, "npcId", "baker_bob");

            SetupObjectiveQuest(
                "q_non_giver",
                giverNpcId: "teacher_maya",
                QuestState.IN_PROGRESS,
                new QuestObjectiveDefinition
                {
                    type = QuestObjectiveType.TalkToNpc,
                    targetId = "baker_bob",
                    dialogue = stepDialogue
                });

            bool handled = _giver.Interact(null);

            Assert.IsTrue(handled);
            Assert.AreSame(stepDialogue, _dialogueService.LastNode);
        }

        [Test]
        public void Interact_LastTalkToNpc_DoesNotTurnInSameInteract()
        {
            DialogueNode stepDialogue = CreateDialogue("last_talk");
            DialogueNode turnInDialogue = CreateDialogue("turn_in");

            QuestInfo quest = SetupObjectiveQuest(
                "q_last_talk",
                "teacher_maya",
                QuestState.IN_PROGRESS,
                new QuestObjectiveDefinition
                {
                    type = QuestObjectiveType.TalkToNpc,
                    targetId = "teacher_maya",
                    dialogue = stepDialogue
                },
                definition =>
                {
                    definition.waitForNpcTurnIn = true;
                    definition.turnInDialogue = turnInDialogue;
                });

            bool firstHandled = _giver.Interact(null);

            Assert.IsTrue(firstHandled);
            Assert.AreEqual(QuestState.CAN_FINISH, quest.state);
            Assert.AreSame(stepDialogue, _dialogueService.LastNode);

            bool secondHandled = _giver.Interact(null);

            Assert.IsTrue(secondHandled);
            Assert.AreEqual(QuestState.FINISHED, quest.state);
            Assert.AreSame(turnInDialogue, _dialogueService.LastNode);
        }

        [Test]
        public void Interact_PriorTalkStep_PlaysDialogueAfterFinished()
        {
            DialogueNode afterFinished = CreateDialogue("after_finished");
            DialogueNode inProgress = CreateDialogue("in_progress");

            var definition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            _ownedObjects.Add(definition);
            definition.id = "q_prior_talk";
            definition.giverNpcId = "teacher_maya";
            definition.inProgressDialogue = inProgress;
            definition.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition
                {
                    type = QuestObjectiveType.TalkToNpc,
                    targetId = "teacher_maya",
                    dialogueAfterFinished = afterFinished
                },
                new QuestObjectiveDefinition
                {
                    type = QuestObjectiveType.EnterArea,
                    targetId = "area_a"
                }
            };

            QuestInfo quest = QuestSystemTestSupport.CreateObjectiveQuestWithDefinition(definition);
            _ownedObjects.Add(quest.gameObject);
            quest.InitializeQuest();
            quest.SetState(QuestState.IN_PROGRESS);
            quest.MoveToNextStep();
            _questService.RegisterQuest(quest);
            _definitionMap[quest] = definition;
            _availabilityService.Initialize(null, _definitionMap);

            bool handled = _giver.Interact(null);

            Assert.IsFalse(handled);
            Assert.AreSame(afterFinished, _dialogueService.LastNode);
            Assert.AreEqual(QuestState.IN_PROGRESS, quest.state);
        }

        private QuestInfo SetupQuest(string id, QuestState state, string npcId)
        {
            var definition = QuestSystemTestSupport.CreateDefinition(id, npcId);
            _ownedObjects.Add(definition);
            definition.startDialogue = CreateDialogue($"{id}_start");
            definition.inProgressDialogue = CreateDialogue($"{id}_in_progress");
            definition.turnInDialogue = CreateDialogue($"{id}_turn_in");
            definition.alreadyFinishedDialogue = CreateDialogue($"{id}_finished");
            QuestInfo quest = QuestSystemTestSupport.CreateQuestInfo(id, state);
            _ownedObjects.Add(quest.gameObject);
            _questService.RegisterQuest(quest);

            _definitionMap[quest] = definition;
            _availabilityService.Initialize(null, _definitionMap);

            return quest;
        }

        private QuestInfo SetupObjectiveQuest(
            string id,
            string giverNpcId,
            QuestState state,
            QuestObjectiveDefinition objective,
            Action<QuestDefinitionSO> configure = null)
        {
            var definition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            _ownedObjects.Add(definition);
            definition.id = id;
            definition.displayName = id;
            definition.giverNpcId = giverNpcId;
            definition.waitForNpcTurnIn = true;
            definition.objectives = new List<QuestObjectiveDefinition> { objective };
            configure?.Invoke(definition);

            QuestInfo quest = QuestSystemTestSupport.CreateObjectiveQuestWithDefinition(definition);
            _ownedObjects.Add(quest.gameObject);
            quest.InitializeQuest();
            quest.SetState(state);
            _questService.RegisterQuest(quest);

            _definitionMap[quest] = definition;
            _availabilityService.Initialize(null, _definitionMap);

            return quest;
        }

        private DialogueNode CreateDialogue(string name)
        {
            var node = ScriptableObject.CreateInstance<DialogueNode>();
            node.name = name;
            _ownedObjects.Add(node);
            return node;
        }

        sealed class StubDialogueService : IDialogueService
        {
            public DialogueNode LastNode { get; private set; }

            public event Action OnDialogueStart;
            public event Action OnDialogueEnd;
            public bool IsDialogueActive => false;

            public void StartDialogue(
                DialogueNode startNode,
                Transform speaker = null,
                Transform localPlayer = null,
                Sprite speakerSprite = null,
                string speakerName = "")
            {
                LastNode = startNode;
                OnDialogueStart?.Invoke();
            }
        }

        sealed class StubNpcResolver : IQuestWorldResolver
        {
            readonly HashSet<string> _npcIds;

            public StubNpcResolver(params string[] npcIds) =>
                _npcIds = new HashSet<string>(npcIds);

            public bool TryGetNpc(string npcId, out NpcCatalogEntry entry)
            {
                entry = _npcIds.Contains(npcId)
                    ? new NpcCatalogEntry { id = npcId, displayName = npcId }
                    : null;
                return entry != null;
            }

            public bool TryGetArea(string areaId, out AreaCatalogEntry entry)
            {
                entry = null;
                return false;
            }

            public bool TryGetInteractable(string interactableId, out InteractableCatalogEntry entry)
            {
                entry = null;
                return false;
            }

            public bool TryGetQuestDefinition(string questId, out QuestDefinitionSO definition)
            {
                definition = null;
                return false;
            }
        }
    }
}

