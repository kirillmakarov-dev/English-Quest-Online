using System;
using EnglishQuest.QuestSystem;
using Fusion;
using Puzzle.Gameplay.MiniGames.DuolingoWordGame;
using Puzzle.Gameplay.MiniGames.LetterConnection;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishQuest.PortfolioDemo
{
    public enum PortfolioGameFlowState
    {
        NetworkConnecting = 0,
        OpenWorld = 1,
        Dialogue = 2,
        MiniGame = 3,
        QuestCompleted = 4,
        LevelCompleted = 5
    }

    public enum PortfolioNetworkFlowState
    {
        Offline = 0,
        Connecting = 1,
        InSession = 2
    }

    public sealed class PortfolioGameFlowCoordinator : MonoBehaviour
    {
        [SerializeField] private DialogueManager dialogueManager;
        [SerializeField] private float questCompletedStateDuration = 2f;

        private IQuestService questService;
        private bool questEventsSubscribed;
        private float transientQuestCompletedUntil = -1f;

        public event Action<PortfolioGameFlowState> OnStateChanged;

        public PortfolioGameFlowState CurrentState { get; private set; }
        public PortfolioNetworkFlowState CurrentNetworkState { get; private set; }

        private void Awake()
        {
            if (dialogueManager == null)
                dialogueManager = FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
        }

        private void OnEnable()
        {
            ResolveQuestService();
            SubscribeToQuestEvents();
            RefreshNow(force: true);
        }

        private void OnDisable()
        {
            UnsubscribeFromQuestEvents();
        }

        private void Update()
        {
            RefreshNow();
        }

        public void RefreshNow(bool force = false)
        {
            ResolveQuestService();

            PortfolioNetworkFlowState nextNetworkState = ResolveNetworkState();
            PortfolioGameFlowState nextState = ResolveGameState(nextNetworkState);

            if (force || nextNetworkState != CurrentNetworkState)
                CurrentNetworkState = nextNetworkState;

            if (!force && nextState == CurrentState)
                return;

            CurrentState = nextState;
            OnStateChanged?.Invoke(CurrentState);
        }

        private void HandleQuestCompleted(QuestInfo quest)
        {
            if (questService == null || questService.IsLevelCompleted)
                return;

            transientQuestCompletedUntil = Time.unscaledTime + questCompletedStateDuration;
            RefreshNow(force: true);
        }

        private void ResolveQuestService()
        {
            if (questService != null)
                return;

            ServiceLocator.For(this)?.TryGet(out questService);
            if (questService == null && QuestManager.HasInstance)
                questService = QuestManager.Instance;

            if (questService != null && !questEventsSubscribed)
            {
                questService.OnQuestCompleted += HandleQuestCompleted;
                questEventsSubscribed = true;
            }
        }

        private void SubscribeToQuestEvents()
        {
            ResolveQuestService();
        }

        private void UnsubscribeFromQuestEvents()
        {
            if (questService != null && questEventsSubscribed)
                questService.OnQuestCompleted -= HandleQuestCompleted;

            questEventsSubscribed = false;
        }

        private PortfolioNetworkFlowState ResolveNetworkState()
        {
            if (ServiceLocator.For(this)?.TryGet(out INetworkSessionService sessionService) == true)
            {
                NetworkRunner runner = sessionService.Runner;
                if (runner != null && runner.IsRunning)
                    return PortfolioNetworkFlowState.InSession;

                return PortfolioNetworkFlowState.Connecting;
            }

            foreach (NetworkRunner runner in NetworkRunner.Instances)
            {
                if (runner != null && runner.IsRunning)
                    return PortfolioNetworkFlowState.InSession;
            }

            return PortfolioNetworkFlowState.Offline;
        }

        private PortfolioGameFlowState ResolveGameState(PortfolioNetworkFlowState networkState)
        {
            if (questService != null && questService.IsLevelCompleted)
                return PortfolioGameFlowState.LevelCompleted;

            if (IsAnyMiniGameOpen())
                return PortfolioGameFlowState.MiniGame;

            if (dialogueManager != null && dialogueManager.IsDialogueActive)
                return PortfolioGameFlowState.Dialogue;

            if (transientQuestCompletedUntil > Time.unscaledTime)
                return PortfolioGameFlowState.QuestCompleted;

            return networkState == PortfolioNetworkFlowState.InSession
                ? PortfolioGameFlowState.OpenWorld
                : PortfolioGameFlowState.NetworkConnecting;
        }

        private static bool IsAnyMiniGameOpen()
        {
            foreach (LetterConnectionScreenView view in FindObjectsByType<LetterConnectionScreenView>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (view != null && view.gameObject.activeInHierarchy)
                    return true;
            }

            foreach (WordGamePanelView view in FindObjectsByType<WordGamePanelView>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (view != null && view.gameObject.activeInHierarchy)
                    return true;
            }

            return false;
        }
    }
}
