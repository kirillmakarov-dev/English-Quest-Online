using Puzzle.Gameplay.Features.WordReveal;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Pluggable mode component that provides game data to the word game system.
    /// Add exactly one implementation (LetterOrderingMode or WordOrderingMode)
    /// to the same GameObject as WordGameQuestStep.
    /// </summary>
    public interface IWordGameMode
    {
        /// <summary>Text shown above the answer slots (e.g., a Hebrew sentence or "Arrange the letters").</summary>
        string Prompt { get; }

        /// <summary>
        /// Optional database for tap-to-translate on the prompt text.
        /// Returns null when word reveal is not configured for this step.
        /// </summary>
        WordTranslationDatabaseSO WordRevealDatabase { get; }

        /// <summary>Builds the ordered slot definitions the player must fill.</summary>
        SlotDefinition[] BuildSlots();

        /// <summary>Builds the shuffled tile definitions (including distractors) available to the player.</summary>
        TileDefinition[] BuildTiles();
    }
}
