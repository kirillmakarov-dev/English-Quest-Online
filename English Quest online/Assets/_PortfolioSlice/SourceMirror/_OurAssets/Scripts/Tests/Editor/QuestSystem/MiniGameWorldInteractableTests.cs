using System.Collections.Generic;
using EnglishQuest.QuestSystem;
using NUnit.Framework;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishQuest.Tests.QuestSystem
{
    public class MiniGameWorldInteractableTests
    {
        private GameObject _root;
        private readonly List<ScriptableObject> _ownedAssets = new();

        [TearDown]
        public void TearDown()
        {
            QuestSystemTestSupport.ClearGlobalLocator();

            foreach (ScriptableObject asset in _ownedAssets)
            {
                if (asset != null)
                    Object.DestroyImmediate(asset);
            }
            _ownedAssets.Clear();

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

        [Test]
        public void Interact_WithSoloActiveQuestBinding_LaunchesMiniGameWithoutNetworkRunner()
        {
            _root = new GameObject("mini-game-test");
            ServiceLocator locator = QuestSystemTestSupport.CreateServiceLocator(_root);

            var binder = new QuestMiniGameBinder();
            locator.Register<IQuestMiniGameBinder>(binder);

            var config = ScriptableObject.CreateInstance<TestQuestMiniGameConfig>();
            config.Configure("active_game");
            _ownedAssets.Add(config);

            QuestDefinitionSO definition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            _ownedAssets.Add(definition);
            definition.id = "quest_minigame";
            definition.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition
                {
                    type = QuestObjectiveType.CompleteMiniGame,
                    targetId = "active_game",
                    miniGameConfig = config
                }
            };

            QuestInfo quest = QuestSystemTestSupport.CreateObjectiveQuestWithDefinition(definition);
            quest.SetState(QuestState.IN_PROGRESS);

            var service = new StubQuestService();
            service.Quests.Add(quest);
            binder.Refresh(service);

            var host = _root.AddComponent<MiniGameWorldLaunchHost>();
            var interactable = _root.AddComponent<MiniGameWorldInteractable>();
            QuestSystemTestSupport.SetPrivateField(interactable, "gameId", "active_game");
            QuestSystemTestSupport.SetPrivateField(interactable, "launchHost", host);

            bool handled = interactable.Interact(null);

            Assert.IsTrue(handled);
            Assert.IsTrue(config.WasLaunched, "The lesson station should launch for a solo player without any network session dependency.");

            Object.DestroyImmediate(quest.gameObject);
        }

        private sealed class TestQuestMiniGameConfig : QuestMiniGameConfigSO
        {
            private string _gameId;

            public bool WasLaunched { get; private set; }

            public override string GameId => _gameId;

            public void Configure(string gameId)
            {
                _gameId = gameId;
            }

            public override bool TryLaunch(
                MiniGameWorldLaunchHost host,
                PlayerInteraction interactor,
                System.Action<int> onCompleted,
                System.Action onClosed)
            {
                WasLaunched = host != null;
                return WasLaunched;
            }
        }
    }
}

