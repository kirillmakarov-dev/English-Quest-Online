using System;
using UnityEngine;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Scene-level bootstrap for the word-ordering game.
    /// Uses the same open/close/completion runtime contract as the other quest mini-games.
    /// </summary>
    public class WordGameBootstrap : QuestMiniGameRuntimeBase
    {
        [SerializeField] private WordGamePanelView panelView;
        [SerializeField] private WordGameViewFactory viewFactory;

        private WordGamePresenter presenter;
        private WordGameSession session;

        public WordGamePresenter Presenter => presenter;
        public override string RuntimeTypeId => "word_game";

        private void Awake()
        {
            panelView?.Close();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            DisposePresenter();
        }

        /// <summary>
        /// Opens the word game for the given mode. Safe to call multiple times.
        /// </summary>
        public void Open(IWordGameMode mode, PlayerInteraction interactor, Action onCompleted, Action onClosed)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            InitializeGame(mode);
            if (presenter == null)
                return;

            BeginMiniGameSession(interactor, onCompleted, onClosed);
            presenter.Show();
        }

        /// <summary>Closes the panel using the shared mini-game close lifecycle.</summary>
        public void Close()
        {
            presenter?.Hide();
        }

        private void InitializeGame(IWordGameMode mode)
        {
            DisposePresenter();

            if (panelView == null || viewFactory == null || mode == null)
            {
                AppLog.Error("[WordGameBootstrap] Missing required references (panelView, viewFactory, or mode).", this);
                return;
            }

            SlotDefinition[] slots = mode.BuildSlots();
            TileDefinition[] tiles = mode.BuildTiles();

            session = new WordGameSession(slots, tiles);

            IAnswerValidator validator = new ExactOrderingValidator();

            presenter = new WordGamePresenter(
                panelView,
                viewFactory,
                validator,
                session,
                mode.Prompt,
                slots,
                tiles);

            presenter.GameCompleted += HandleGameCompleted;
            presenter.Hidden += HandlePresenterHidden;

            presenter.Initialize();
            panelView.SetWordRevealDatabase(mode.WordRevealDatabase);
            panelView.Close();
        }

        private void DisposePresenter()
        {
            if (presenter != null)
            {
                presenter.GameCompleted -= HandleGameCompleted;
                presenter.Hidden -= HandlePresenterHidden;
                presenter.Dispose();
                presenter = null;
            }

            session?.Dispose();
            session = null;
        }

        private void HandleGameCompleted(bool isCorrect)
        {
            if (!isCorrect)
                return;

            presenter?.Hide();
            NotifyMiniGameCompleted();
        }

        private void HandlePresenterHidden()
        {
            NotifyMiniGameClosed();
        }
    }
}
