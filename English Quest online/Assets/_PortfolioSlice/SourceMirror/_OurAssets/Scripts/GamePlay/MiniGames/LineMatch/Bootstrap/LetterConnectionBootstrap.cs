using System;
using UnityEngine;

namespace Puzzle.Gameplay.MiniGames.LetterConnection
{
    public class LetterConnectionBootstrap : QuestMiniGameRuntimeBase
    {
        [SerializeField] private LetterConnectionScreenView screenView;
        [SerializeField] private LetterConnectionViewFactory viewFactory;
        [SerializeField] private bool initializeOnAwake = true;
        private LetterConnectionLevelConfigSO levelConfig;
        public LetterConnectionPresenter Presenter { get; private set; }
        public LevelSession Session { get; private set; }
        public override string RuntimeTypeId => "line_match";

        private void Awake()
        {
            screenView?.Close();

            if (initializeOnAwake)
            {
                InitializeMiniGame();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

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
                return false;

            BeginMiniGameSession(interactor, null, null);
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
            bool levelChanged = levelConfig != config;
            levelConfig = config;

            if (levelChanged)
                InitializeMiniGame();

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (Presenter == null && !InitializeMiniGame())
                return false;

            BeginMiniGameSession(interactor, onCompleted, onClosed);
            Presenter.Show();
            return true;
        }

        private void HandleMiniGameHidden()
        {
            NotifyMiniGameClosed();
        }

        private void HandleLevelCompleted()
        {
            NotifyMiniGameCompleted();
            Presenter?.Hide();
        }
    }

}
