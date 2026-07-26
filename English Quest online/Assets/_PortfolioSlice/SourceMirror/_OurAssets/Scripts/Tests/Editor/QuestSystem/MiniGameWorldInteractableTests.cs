using System.Collections.Generic;
using EnglishKingdom.QuestSystem;
using NUnit.Framework;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishKingdom.Tests.QuestSystem
{
    public class MiniGameWorldInteractableTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            QuestSystemTestSupport.ClearGlobalLocator();

            if (_root != null)
                Object.DestroyImmediate(_root);
        }

        [Test]
        public void CanInteract_ReturnsFalse_WhenNoActiveBinding()
        {
            _root = new GameObject("mini-game-test");
            QuestSystemTestSupport.CreateServiceLocator(_root);

            var interactable = _root.AddComponent<MiniGameWorldInteractable>();
            QuestSystemTestSupport.SetPrivateField(interactable, "gameId", "missing_game");

            Assert.IsFalse(interactable.CanInteract);
        }

        [Test]
        public void CanInteract_ReturnsTrue_WhenQuestBinderHasBinding()
        {
            _root = new GameObject("mini-game-test");
            ServiceLocator locator = QuestSystemTestSupport.CreateServiceLocator(_root);

            var binder = new QuestMiniGameBinder();
            locator.Register<IQuestMiniGameBinder>(binder);

            QuestDefinitionSO definition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            definition.id = "quest_minigame";
            definition.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition
                {
                    type = QuestObjectiveType.CompleteMiniGame,
                    targetId = "active_game",
                    miniGameConfig = QuestSystemTestSupport.CreateLetterOrderingQuestConfig("active_game")
                }
            };

            QuestInfo quest = QuestSystemTestSupport.CreateObjectiveQuestWithDefinition(definition);
            quest.SetState(QuestState.IN_PROGRESS);

            var service = new StubQuestService();
            service.Quests.Add(quest);
            binder.Refresh(service);

            var interactable = _root.AddComponent<MiniGameWorldInteractable>();
            QuestSystemTestSupport.SetPrivateField(interactable, "gameId", "active_game");

            Assert.IsTrue(interactable.CanInteract);
        }
    }
}
