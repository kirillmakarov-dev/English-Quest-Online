using UnityEngine;

namespace Puzzle.Gameplay.Features.WordReveal
{
    /// <summary>
    /// Controls the word-reveal popup panel.
    /// Implement this to change popup appearance or animation without touching logic.
    /// </summary>
    public interface IWordRevealPopup
    {
        bool IsVisible { get; }

        /// <summary>Show the popup above <paramref name="worldPosition"/> with <paramref name="translation"/>.</summary>
        void Show(string translation, Vector2 worldPosition);

        void Hide();
    }
}
