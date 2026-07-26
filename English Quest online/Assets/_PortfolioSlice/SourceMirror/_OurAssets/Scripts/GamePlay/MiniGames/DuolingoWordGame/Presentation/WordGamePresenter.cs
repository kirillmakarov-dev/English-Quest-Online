using System;
using System.Collections.Generic;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Pure C# presenter that orchestrates the word game.
    /// Wires session state changes to view refreshes and handles all player input events
    /// (tile click, slot click, drag-and-drop).
    ///
    /// Interaction rules:
    ///   • Click a free tile  → place it in the next empty non-pre-filled slot.
    ///   • Click a filled slot → remove the tile back to the pool.
    ///   • Drop a tile on a slot → place it in that specific slot (evicts current occupant if any).
    ///
    /// Validation fires automatically after every placement when all fillable slots are occupied.
    /// A correct answer raises GameCompleted(true) once and cannot be re-raised.
    /// An incorrect answer raises GameCompleted(false) and allows the player to keep editing.
    /// </summary>
    public sealed class WordGamePresenter : IDisposable
    {
        private readonly WordGamePanelView _panelView;
        private readonly IWordGameViewFactory _factory;
        private readonly IAnswerValidator _validator;
        private readonly WordGameSession _session;
        private readonly string _prompt;
        private readonly SlotDefinition[] _slotDefinitions;
        private readonly TileDefinition[] _tileDefinitions;

        private readonly Dictionary<string, TileView> _tileViewsById =
            new Dictionary<string, TileView>(StringComparer.Ordinal);

        private readonly Dictionary<int, SlotView> _slotViewsByIndex =
            new Dictionary<int, SlotView>();

        private bool _isSuccessful;
        private bool _isDisposed;

        /// <summary>
        /// Fired when all fillable slots are occupied.
        /// True = correct answer; False = incorrect answer (player may continue editing).
        /// </summary>
        public event Action<bool> GameCompleted;

        /// <summary>Fired when the panel is hidden (whether by success or manual close).</summary>
        public event Action Hidden;

        public WordGamePresenter(
            WordGamePanelView panelView,
            IWordGameViewFactory factory,
            IAnswerValidator validator,
            WordGameSession session,
            string prompt,
            SlotDefinition[] slotDefinitions,
            TileDefinition[] tileDefinitions)
        {
            _panelView = panelView;
            _factory = factory;
            _validator = validator;
            _session = session;
            _prompt = prompt;
            _slotDefinitions = slotDefinitions;
            _tileDefinitions = tileDefinitions;
        }

        public void Initialize()
        {
            _isSuccessful = false;

            _panelView.Build(_prompt, _slotDefinitions, _tileDefinitions, _factory);

            CacheViews();
            BindViewEvents();
            _panelView.CloseRequested += Hide;
            _session.SessionChanged += RefreshAllViews;

            RefreshAllViews();
        }

        public void Show()
        {
            _panelView.Open();
            RefreshAllViews();
        }

        public void Hide()
        {
            UnbindViewEvents();
            _panelView.CloseRequested -= Hide;
            _session.SessionChanged -= RefreshAllViews;
            _panelView.Close();
            Hidden?.Invoke();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            UnbindViewEvents();
            _session.SessionChanged -= RefreshAllViews;

            _tileViewsById.Clear();
            _slotViewsByIndex.Clear();
        }

        // ── Private: view caching ─────────────────────────────────────────────

        private void CacheViews()
        {
            _tileViewsById.Clear();
            _slotViewsByIndex.Clear();

            foreach (TileView tileView in _panelView.TileViews)
            {
                if (tileView != null && !string.IsNullOrEmpty(tileView.TileId))
                    _tileViewsById[tileView.TileId] = tileView;
            }

            foreach (SlotView slotView in _panelView.SlotViews)
            {
                if (slotView != null)
                    _slotViewsByIndex[slotView.SlotIndex] = slotView;
            }
        }

        // ── Private: event binding ────────────────────────────────────────────

        private void BindViewEvents()
        {
            foreach (TileView tileView in _panelView.TileViews)
            {
                if (tileView == null) continue;
                tileView.Clicked += OnTileClicked;
            }

            foreach (SlotView slotView in _panelView.SlotViews)
            {
                if (slotView == null) continue;
                slotView.Clicked += OnSlotClicked;
                slotView.TileDropped += OnTileDropped;
            }
        }

        private void UnbindViewEvents()
        {
            foreach (TileView tileView in _panelView.TileViews)
            {
                if (tileView == null) continue;
                tileView.Clicked -= OnTileClicked;
            }

            foreach (SlotView slotView in _panelView.SlotViews)
            {
                if (slotView == null) continue;
                slotView.Clicked -= OnSlotClicked;
                slotView.TileDropped -= OnTileDropped;
            }
        }

        // ── Private: input handlers ───────────────────────────────────────────

        private void OnTileClicked(string tileId)
        {
            if (_isSuccessful) return;

            int targetSlot = FindFirstEmptyFillableSlot();
            if (targetSlot == -1) return;

            _session.TryPlaceTile(tileId, targetSlot);
            CheckCompletion();
        }

        private void OnSlotClicked(int slotIndex)
        {
            if (_isSuccessful) return;
            _session.TryRemoveTile(slotIndex);
        }

        private void OnTileDropped(string tileId, int slotIndex)
        {
            if (_isSuccessful) return;

            _session.TryPlaceTile(tileId, slotIndex);
            CheckCompletion();
        }

        // ── Private: view refresh ─────────────────────────────────────────────

        private void RefreshAllViews()
        {
            IReadOnlyList<TileState> tiles = _session.Tiles;
            for (int i = 0; i < tiles.Count; i++)
            {
                TileState tileState = tiles[i];
                if (_tileViewsById.TryGetValue(tileState.Id, out TileView tileView))
                    tileView.SetUsed(tileState.IsUsed);
            }

            IReadOnlyList<SlotState> slots = _session.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                SlotState slotState = slots[i];
                if (!_slotViewsByIndex.TryGetValue(slotState.Index, out SlotView slotView)) continue;

                if (slotState.IsPreFilled) continue; // pre-filled slots are set once during Initialize

                string displayValue = string.Empty;
                if (!slotState.IsEmpty)
                {
                    TileState tile = _session.GetTile(slotState.PlacedTileId);
                    displayValue = tile?.DisplayValue ?? string.Empty;
                }

                slotView.SetDisplay(displayValue);
            }
        }

        // ── Private: completion ───────────────────────────────────────────────

        private int FindFirstEmptyFillableSlot()
        {
            IReadOnlyList<SlotState> slots = _session.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                SlotState slot = slots[i];
                if (!slot.IsPreFilled && slot.IsEmpty)
                    return slot.Index;
            }
            return -1;
        }

        private void CheckCompletion()
        {
            if (_isSuccessful) return;

            // Only validate once every fillable slot is occupied
            IReadOnlyList<SlotState> slots = _session.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                SlotState slot = slots[i];
                if (!slot.IsPreFilled && slot.IsEmpty) return;
            }

            bool isCorrect = _validator.Validate(_session);
            if (isCorrect)
                _isSuccessful = true;

            GameCompleted?.Invoke(isCorrect);
        }
    }
}
