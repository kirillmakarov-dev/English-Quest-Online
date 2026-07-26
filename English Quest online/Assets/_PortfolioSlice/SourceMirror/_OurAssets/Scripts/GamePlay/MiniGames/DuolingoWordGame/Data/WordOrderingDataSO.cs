using Puzzle.Gameplay.Features.WordReveal;
using UnityEngine;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// ScriptableObject that holds data for the word-ordering game mode.
    /// The player arranges English words to match a given Hebrew sentence.
    /// Place on the world object alongside WordOrderingMode.
    /// </summary>
    [CreateAssetMenu(fileName = "WordOrderingData", menuName = ScriptableObjectMenuPaths.MiniGamesWordOrdering + "/Data")]
    public class WordOrderingDataSO : ScriptableObject
    {
        [Tooltip("The sentence shown as the prompt (any language).")]
        [SerializeField] private string prompt;

        [Tooltip("The English words in their correct order.")]
        [SerializeField] private string[] englishWordsInOrder;

        [Tooltip("Zero-based indices of words that are pre-filled and locked. The player only arranges the rest.")]
        [SerializeField] private int[] preFilledIndices;

        [Tooltip("Additional distractor words added to the tile pool.")]
        [SerializeField] private string[] distractorWords;

        [Header("Word Reveal (Optional)")]
        [Tooltip("Database used by the prompt's WordRevealSetup to show tap-to-translate tooltips. Leave empty to disable word reveal for this step.")]
        [SerializeField] private WordTranslationDatabaseSO wordRevealDatabase;

        public string Prompt => prompt;
        public string[] EnglishWordsInOrder => englishWordsInOrder;
        public int[] PreFilledIndices => preFilledIndices;
        public string[] DistractorWords => distractorWords;
        public WordTranslationDatabaseSO WordRevealDatabase => wordRevealDatabase;
    }
}
