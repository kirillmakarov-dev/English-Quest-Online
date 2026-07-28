using System;
using UnityEngine;
using UnityServiceLocator;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Scene-level bootstrap for the word-ordering game. Inherits GameplayUIBase so it can
    /// acquire and release the player interaction lock automatically.
    ///
    /// Mirrors the lifecycle of the Line Match bootstrap:
    ///   1. Caller invokes Open(mode, interactor, onCompleted, onClosed).
    ///   2. Bootstrap builds the session from the mode, creates the presenter, shows the panel.
    ///   3. On correct answer → fires onCompleted callback and closes.
    ///   4. On incorrect answer → fires onIncorrect (optional feedback hook) and stays open.
    ///   5. On hide (player closes without finishing) → fires onClosed.
    /// </summary>
    public class WordGameBootstrap : GameplayUIBase
    {
        [SerializeField] private WordGamePanelView _panelView;
        [SerializeField] private WordGameViewFactory _viewFactory;

        private WordGamePresenter _presenter;
        private WordGameSession _session;
        private PlayerInteraction _wordGameInteractor;

        private Action _onCompleted;
        private Action _onClosed;

        public WordGamePresenter Presenter => _presenter;

        private void Awake()
        {
            _panelView?.Close();
        }

        private void OnDestroy()
        {
            ReleaseInteractionLock();
            DisposePresenter();
        }

        /// <summary>
        /// Opens the word-ordering game for the given mode. Rebuilds the session and presenter
        /// if the mode has changed. Safe to call multiple times.
        /// </summary>
        public void Open(IWordGameMode mode, PlayerInteraction interactor, Action onCompleted, Action onClosed)
        {
            _onCompleted = onCompleted;
            _onClosed = onClosed;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            InitializeGame(mode);
            if (_presenter == null)
            {
                ReleaseInteractionLock();
                return;
            }

            AcquireInteractionLock(interactor);
            _presenter.Show();
        }

        /// <summary>Closes the panel and releases the player lock without invoking callbacks.</summary>
        public void Close()
        {
            _presenter?.Hide();
        }

        // ── Private: lifecycle ────────────────────────────────────────────────

        private void InitializeGame(IWordGameMode mode)
        {
            DisposePresenter();

            if (_panelView == null || _viewFactory == null || mode == null)
            {
                AppLog.Error("[WordOrderingBootstrap] Missing required references (panelView, viewFactory, or mode).", this);
                return;
            }

            SlotDefinition[] slots = mode.BuildSlots();
            TileDefinition[] tiles = mode.BuildTiles();

            _session = new WordGameSession(slots, tiles);

            IAnswerValidator validator = new ExactOrderingValidator();

            _presenter = new WordGamePresenter(
                _panelView,
                _viewFactory,
                validator,
                _session,
                mode.Prompt,
                slots,
                tiles);

            _presenter.GameCompleted += HandleGameCompleted;
            _presenter.Hidden += HandlePresenterHidden;

            _presenter.Initialize();
            _panelView.SetWordRevealDatabase(mode.WordRevealDatabase);
            _panelView.Close(); // start closed; Show() is called by Open()
        }

        private void DisposePresenter()
        {
            if (_presenter != null)
            {
                _presenter.GameCompleted -= HandleGameCompleted;
                _presenter.Hidden -= HandlePresenterHidden;
                _presenter.Dispose();
                _presenter = null;
            }

            _session?.Dispose();
            _session = null;
        }

        // ── Private: player lock ──────────────────────────────────────────────

        private void AcquireInteractionLock(PlayerInteraction interactor)
        {
            if (interactor == null || interactor == _wordGameInteractor) return;

            ReleaseInteractionLock();
            _wordGameInteractor = interactor;
            BeginInteraction(interactor);
        }

        private void ReleaseInteractionLock()
        {
            EndInteraction();
            _wordGameInteractor = null;
        }

        // ── Private: event handlers ───────────────────────────────────────────

        private void HandleGameCompleted(bool isCorrect)
        {
            if (!isCorrect)
            {
                // Incorrect answer: stay open, let the player adjust tiles.
                // Visual feedback (shake, flash) can be added here or in a separate component.
                return;
            }

            // Correct: clear callbacks before Hide() so HandlePresenterHidden doesn't double-fire.
            Action completed = _onCompleted;
            _onCompleted = null;
            _onClosed = null;

            _presenter?.Hide();
            ReleaseInteractionLock();
            completed?.Invoke();
        }

        private void HandlePresenterHidden()
        {
            ReleaseInteractionLock();

            Action closed = _onClosed;
            _onCompleted = null;
            _onClosed = null;
            closed?.Invoke();
        }
    }
}
