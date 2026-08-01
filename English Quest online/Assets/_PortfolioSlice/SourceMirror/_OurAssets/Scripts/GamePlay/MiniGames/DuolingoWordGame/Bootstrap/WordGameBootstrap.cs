using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Scene-level bootstrap for the word-ordering game.
    /// Uses the same open/close/completion runtime contract as the other quest mini-games.
    /// </summary>
    public class WordGameBootstrap : QuestMiniGameRuntimeBase
    {
        [FormerlySerializedAs("_panelView")]
        [SerializeField] private WordGamePanelView panelView;

        [FormerlySerializedAs("_viewFactory")]
        [SerializeField] private WordGameViewFactory viewFactory;

        private WordGamePresenter presenter;
        private WordGameSession session;

        public WordGamePresenter Presenter => presenter;
        public override string RuntimeTypeId => "word_game";

        private void Awake()
        {
            ResolveReferences();
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
            EnsureHierarchyActive();
            ResolveReferences();

            InitializeGame(mode);
            if (presenter == null)
            {
                panelView?.Close();
                return;
            }

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
            ResolveReferences();

            if (panelView == null || viewFactory == null || mode == null)
            {
                AppLog.Error("[WordGameBootstrap] Missing required references (panelView, viewFactory, or mode).", this);
                return;
            }

            SlotDefinition[] slots = mode.BuildSlots();
            TileDefinition[] tiles = mode.BuildTiles();
            AppLog.Info(
                $"[WordGameBootstrap] Opening word game prompt='{mode.Prompt}' slots={slots.Length} tiles={tiles.Length}.",
                this);

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

            if (!presenter.Initialize())
            {
                AppLog.Error("[WordGameBootstrap] Word game view failed to build. Closing the panel instead of showing prefab defaults.", this);
                DisposePresenter();
                panelView.Close();
                return;
            }

            panelView.SetWordRevealDatabase(mode.WordRevealDatabase);
            panelView.Close();
        }

        private void ResolveReferences()
        {
            if (panelView == null)
                panelView = GetComponent<WordGamePanelView>();

            if (viewFactory == null)
                viewFactory = GetComponent<WordGameViewFactory>();
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

            NotifyMiniGameCompletedDelayed(() => presenter?.Hide());
        }

        private void HandlePresenterHidden()
        {
            NotifyMiniGameClosed();
        }

        private void EnsureHierarchyActive()
        {
            Transform current = transform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                    current.gameObject.SetActive(true);

                current = current.parent;
            }
        }
    }
}
