using System;
using System.Collections.Generic;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Validates that every non-pre-filled slot contains a tile whose display value
    /// matches the slot's expected value (case-insensitive).
    /// </summary>
    public sealed class ExactOrderingValidator : IAnswerValidator
    {
        public bool Validate(WordGameSession session)
        {
            IReadOnlyList<SlotState> slots = session.Slots;

            for (int i = 0; i < slots.Count; i++)
            {
                SlotState slot = slots[i];

                // Pre-filled slots are always considered correct
                if (slot.IsPreFilled) continue;

                if (slot.IsEmpty) return false;

                TileState tile = session.GetTile(slot.PlacedTileId);
                if (tile == null) return false;

                if (!string.Equals(tile.DisplayValue, slot.ExpectedValue, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }
    }
}
