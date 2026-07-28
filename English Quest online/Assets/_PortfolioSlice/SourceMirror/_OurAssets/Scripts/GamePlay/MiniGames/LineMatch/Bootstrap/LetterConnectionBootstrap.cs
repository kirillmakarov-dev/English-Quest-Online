using System;
using UnityEngine;

namespace Puzzle.Gameplay.MiniGames.LetterConnection
{
    public class LetterConnectionBootstrap : GameplayUIBase
    {
        [SerializeField] private LetterConnectionScreenView screenView;
        [SerializeField] private LetterConnectionViewFactory viewFactory;
        [SerializeField] private bool initializeOnAwake = true;
        private LetterConnectionLevelConfigSO levelConfig;
        private PlayerInteraction activeInteractingPlayer;
        private Action _onCompleted;
        private Action _onClosed;
        public LetterConnectionPresenter Presenter { get; private set; }
        public LevelSession Session { get; private set; }

        private void Awake()
        {
            screenView?.Close();

            if (initializeOnAwake)
            {
                InitializeMiniGame();
            }
        }

        private void OnDestroy()
        {
            ReleaseInteractionLock();

            if (Presenter != null)
            {
                Presenter.Hidden -= HandleMiniGameHidden;
                Presenter.LevelCompleted -= HandleLevelCompleted;
            }

            Presenter?.Dispose();
            Presenter = null;
            Session = null;
        }

        public bool InitializeMiniGame()
        {
            if (Presenter != null)
            {
                Presenter.Hidden -= HandleMiniGameHidden;
                Presenter.LevelCompleted -= HandleLevelCompleted;
                Presenter.Dispose();
            }

            if (screenView == null || levelConfig == null || viewFactory == null)
            {
                AppLog.Warning(
                    $"[LetterConnectionBootstrap] Cannot initialize mini-game. " +
                    $"screenView={(screenView != null)}, levelConfig={(levelConfig != null)}, viewFactory={(viewFactory != null)}.",
                    this);
                Presenter = null;
                Session = null;
                return false;
            }

            Session = new LevelSession(levelConfig);

            IConnectionValidationStrategy validationStrategy = new ExactLetterValidationStrategy();
            ICompletionChecker completionChecker = new AllWordsFilledCompletionChecker();

            Presenter = new LetterConnectionPresenter(
                screenView,
                viewFactory,
                validationStrategy,
                completionChecker,
                Session,
                levelConfig);

            Presenter.Hidden += HandleMiniGameHidden;
            Presenter.LevelCompleted += HandleLevelCompleted;
            Presenter.Initialize();
            screenView.Close();
            return true;
        }

        public bool OpenMiniGame(PlayerInteraction interactor = null)
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (Presenter == null && !InitializeMiniGame())
            {
                ReleaseInteractionLock();
                return false;
            }

            AcquireInteractionLock(interactor);
            if (Presenter == null)
            {
                ReleaseInteractionLock();
                return false;
            }

            Presenter.Show();
            return true;
        }

        public void CloseMiniGame()
        {          
            Presenter?.Hide();
        }

        public void SetLevelConfig(LetterConnectionLevelConfigSO newConfig, PlayerInteraction interactor = null)
        {
            bool levelChanged = levelConfig != newConfig;
            levelConfig = newConfig;

            if (levelChanged)
            {
                InitializeMiniGame();
            }

            OpenMiniGame(interactor);
        }

        public bool Open(LetterConnectionLevelConfigSO config, PlayerInteraction interactor, Action onCompleted, Action onClosed)
        {
            _onCompleted = onCompleted;
            _onClosed = onClosed;
            bool levelChanged = levelConfig != config;
            levelConfig = config;

            if (levelChanged)
                InitializeMiniGame();

            return OpenMiniGame(interactor);
        }

        private void AcquireInteractionLock(PlayerInteraction interactor)
        {
            PlayerInteraction resolvedInteractor = interactor != null ? interactor : activeInteractingPlayer;
            if (resolvedInteractor == null)
            {
                return;
            }

            if (activeInteractingPlayer == resolvedInteractor)
            {
                return;
            }

            ReleaseInteractionLock();
            activeInteractingPlayer = resolvedInteractor;
            BeginInteraction(resolvedInteractor);
        }

        private void ReleaseInteractionLock()
        {
            EndInteraction();
            activeInteractingPlayer = null;
        }

        private void HandleMiniGameHidden()
        {
            ReleaseInteractionLock();
            Action closed = _onClosed;
            _onCompleted = null;
            _onClosed = null;
            closed?.Invoke();
        }

        private void HandleLevelCompleted()
        {
            Action completed = _onCompleted;
            _onCompleted = null;
            _onClosed = null;

            Presenter?.Hide();
            ReleaseInteractionLock();
            completed?.Invoke();
        }
    }

}
