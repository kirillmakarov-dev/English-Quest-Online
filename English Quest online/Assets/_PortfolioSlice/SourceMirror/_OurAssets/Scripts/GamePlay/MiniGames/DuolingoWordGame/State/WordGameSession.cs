using System;
using System.Collections.Generic;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Owns all runtime state for one word game round: slots and tiles.
    /// Provides atomic mutation operations (TryPlaceTile, TryRemoveTile) and
    /// raises SessionChanged after each operation so the presenter can refresh views.
    /// </summary>
    public sealed class WordGameSession : IDisposable
    {
        private readonly List<SlotState> _slots;
        private readonly List<TileState> _tiles;
        private readonly Dictionary<string, TileState> _tilesById;
        private bool _isDisposed;

        public event Action SessionChanged;

        public IReadOnlyList<SlotState> Slots => _slots;
        public IReadOnlyList<TileState> Tiles => _tiles;

        public WordGameSession(SlotDefinition[] slots, TileDefinition[] tiles)
        {
            _slots = new List<SlotState>(slots.Length);
            _tiles = new List<TileState>(tiles.Length);
            _tilesById = new Dictionary<string, TileState>(tiles.Length, StringComparer.Ordinal);

            for (int i = 0; i < slots.Length; i++)
            {
                var slotState = new SlotState(slots[i], i);
                slotState.StateChanged += OnStateChanged;
                _slots.Add(slotState);
            }

            foreach (TileDefinition def in tiles)
            {
                var tileState = new TileState(def);
                tileState.StateChanged += OnStateChanged;
                _tiles.Add(tileState);
                _tilesById[def.Id] = tileState;
            }
        }

        /// <summary>
        /// Places a tile into a slot. If the slot is already occupied, the previous tile
        /// is freed and returned to the pool automatically.
        /// Returns false if the tile or slot is invalid, the tile is already used in another
        /// way, or the slot is pre-filled.
        /// </summary>
        public bool TryPlaceTile(string tileId, int slotIndex)
        {
            if (!_tilesById.TryGetValue(tileId, out TileState tile)) return false;
            if (slotIndex < 0 || slotIndex >= _slots.Count) return false;

            SlotState slot = _slots[slotIndex];
            if (slot.IsPreFilled) return false;

            // If the tile is already occupying this same slot, do nothing
            if (slot.PlacedTileId == tileId) return false;

            // Evict the tile currently in this slot (if any)
            if (!slot.IsEmpty && _tilesById.TryGetValue(slot.PlacedTileId, out TileState evicted))
                evicted.MarkFree();

            // If this tile is already in a different slot, clear that slot first
            foreach (SlotState otherSlot in _slots)
            {
                if (!otherSlot.IsPreFilled && otherSlot.PlacedTileId == tileId)
                {
                    otherSlot.Clear();
                    break;
                }
            }

            slot.Place(tileId);
            tile.MarkUsed();
            return true;
        }

        /// <summary>
        /// Removes the tile from a slot and frees it back to the pool.
        /// Returns false if the slot is pre-filled, empty, or out of range.
        /// </summary>
        public bool TryRemoveTile(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count) return false;

            SlotState slot = _slots[slotIndex];
            if (slot.IsPreFilled || slot.IsEmpty) return false;

            if (_tilesById.TryGetValue(slot.PlacedTileId, out TileState tile))
                tile.MarkFree();

            slot.Clear();
            return true;
        }

        /// <summary>Returns the TileState for a given tile ID, or null if not found.</summary>
        public TileState GetTile(string tileId)
        {
            if (string.IsNullOrEmpty(tileId)) return null;
            _tilesById.TryGetValue(tileId, out TileState tile);
            return tile;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            foreach (SlotState slot in _slots)
                slot.StateChanged -= OnStateChanged;

            foreach (TileState tile in _tiles)
                tile.StateChanged -= OnStateChanged;

            _tilesById.Clear();
        }

        private void OnStateChanged() => SessionChanged?.Invoke();
    }
}
