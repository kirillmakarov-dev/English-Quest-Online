using UnityEngine;

namespace Puzzle.Gameplay.Features.WordReveal
{
    /// <summary>
    /// Adapts <see cref="WordTranslationDatabaseSO"/> to <see cref="IWordTranslationProvider"/>.
    /// Keeps the database asset decoupled from the presenter.
    /// </summary>
    public class SOWordTranslationProvider : IWordTranslationProvider
    {
        private readonly WordTranslationDatabaseSO _database;

        public SOWordTranslationProvider(WordTranslationDatabaseSO database)
        {
            _database = database;
        }

        public bool TryGetTranslation(string word, out string translation)
        {
            if (_database == null)
            {
                translation = null;
                return false;
            }

            return _database.TryGetTranslation(word, out translation);
        }
    }
}
