using System;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Runtime state of a single answer slot. Owned by WordGameSession.
    /// Pre-filled slots are immutable after construction.
    /// </summary>
    public sealed class SlotState
    {
        public int Index { get; }
        public string ExpectedValue { get; }
        public bool IsPreFilled { get; }

        /// <summary>ID of the tile currently placed in this slot. Null when empty.</summary>
        public string PlacedTileId { get; private set; }

        public bool IsEmpty => PlacedTileId == null;

        public event Action StateChanged;

        public SlotState(SlotDefinition definition, int index)
        {
            Index = index;
            ExpectedValue = definition.ExpectedValue;
            IsPreFilled = definition.IsPreFilled;
            PlacedTileId = null;
        }

        internal void Place(string tileId)
        {
            PlacedTileId = tileId;
            StateChanged?.Invoke();
        }

        internal void Clear()
        {
            PlacedTileId = null;
            StateChanged?.Invoke();
        }
    }
}
