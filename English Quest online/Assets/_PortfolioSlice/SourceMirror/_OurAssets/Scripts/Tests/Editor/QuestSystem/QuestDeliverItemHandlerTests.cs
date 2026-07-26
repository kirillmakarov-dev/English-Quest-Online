using System.Collections.Generic;
using EnglishKingdom.QuestSystem;
using NUnit.Framework;
using UnityEngine;

namespace EnglishKingdom.Tests.QuestSystem
{
    [TestFixture]
    public class QuestDeliverItemHandlerTests
    {
        [Test]
        public void DeliverItem_WithoutInventory_DoesNotCompleteStep()
        {
            var definition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            definition.id = "q_deliver";
            definition.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition
                {
                    type = QuestObjectiveType.DeliverItem,
                    targetId = "recipient_npc",
                    requiredItemId = 99999,
                    displayText = "Deliver the item"
                }
            };

            QuestInfo quest = QuestSystemTestSupport.CreateObjectiveQuestWithDefinition(definition);
            quest.InitializeQuest();
            quest.SetState(QuestState.IN_PROGRESS);

            var questService = new TrackingQuestService(quest);
            var resolver = new StubNpcResolver("recipient_npc");
            var registry = new QuestObjectiveHandlerRegistry(resolver, questService);

            registry.HandleNpcInteracted(new QuestObjectiveEvents.NpcInteracted("recipient_npc"));

            Assert.AreEqual(0, questService.CompleteStepCalls);
            Assert.AreEqual(QuestState.IN_PROGRESS, quest.state);
            Assert.AreEqual(0, quest.currentStepIndex);
        }

        sealed class TrackingQuestService : IQuestService
        {
            readonly QuestInfo _quest;
            public int CompleteStepCalls { get; private set; }

            public TrackingQuestService(QuestInfo quest) => _quest = quest;

            public IReadOnlyList<QuestInfo> AllQuests => new[] { _quest };

            public event System.Action<QuestInfo> OnQuestStarted;
            public event System.Action<QuestInfo> OnQuestUpdated;
            public event System.Action<QuestInfo> OnQuestCompleted;
            public event System.Action<QuestInfo> OnQuestStateChanged;
            public event System.Action<QuestObjectiveProgressEvent> OnObjectiveProgressChanged;
            public event System.Action OnLevelCompleted;

            public bool IsLevelCompleted => false;

            public void ReportObjectiveProgress(QuestInfo questInfo, int stepIndex, int current, int target) { }

            public ObjectiveProgress GetObjectiveProgress(QuestInfo questInfo, int stepIndex) =>
                questInfo.GetObjectiveProgress(stepIndex);

            public void CompleteObjectiveStep(QuestInfo questInfo, int stepIndex, string finalState = "")
            {
                if (questInfo == _quest)
                    CompleteStepCalls++;
            }

            public void StartQuest(QuestInfo questInfo) { }
            public void FinishQuest(QuestInfo questInfo) { }
            public void RegisterQuest(QuestInfo questInfo) { }
            public void RegisterQuestStep(QuestStep step, QuestInfo questInfo, int stepIndex) { }
            public bool IsQuestCompleted(QuestInfo questInfo) => false;
            public QuestInfo GetQuestById(string questId) => questId == _quest.id ? _quest : null;
            public void ReevaluateQuestRequirements() { }
        }

        sealed class StubNpcResolver : IQuestWorldResolver
        {
            readonly HashSet<string> _npcIds;

            public StubNpcResolver(params string[] npcIds) => _npcIds = new HashSet<string>(npcIds);

            public bool TryGetNpc(string npcId, out NpcCatalogEntry entry)
            {
                entry = null;
                return _npcIds.Contains(npcId);
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
