using System;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Runtime state of a single selectable tile. Owned by WordGameSession.
    /// </summary>
    public sealed class TileState
    {
        public string Id { get; }
        public string DisplayValue { get; }
        public bool IsDistractor { get; }
        public bool IsUsed { get; private set; }

        public event Action StateChanged;

        public TileState(TileDefinition definition)
        {
            Id = definition.Id;
            DisplayValue = definition.DisplayValue;
            IsDistractor = definition.IsDistractor;
        }

        internal void MarkUsed()
        {
            if (IsUsed) return;
            IsUsed = true;
            StateChanged?.Invoke();
        }

        internal void MarkFree()
        {
            if (!IsUsed) return;
            IsUsed = false;
            StateChanged?.Invoke();
        }
    }
}
