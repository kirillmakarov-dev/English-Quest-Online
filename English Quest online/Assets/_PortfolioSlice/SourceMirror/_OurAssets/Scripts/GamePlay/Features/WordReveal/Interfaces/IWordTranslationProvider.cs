namespace Puzzle.Gameplay.Features.WordReveal
{
    /// <summary>
    /// Provides translation lookups for individual words.
    /// Implement this to swap data sources (ScriptableObject, REST API, etc.)
    /// without touching any other layer.
    /// </summary>
    public interface IWordTranslationProvider
    {
        /// <returns>True if a translation exists; <paramref name="translation"/> is set to the result.</returns>
        bool TryGetTranslation(string word, out string translation);
    }
}
