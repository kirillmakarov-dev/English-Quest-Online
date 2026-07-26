using Puzzle.Gameplay.Features.WordReveal;
using UnityEngine;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// ScriptableObject that holds data for the letter-ordering game mode.
    /// Place on the world object alongside LetterOrderingMode.
    /// </summary>
    [CreateAssetMenu(fileName = "LetterOrderingData", menuName = ScriptableObjectMenuPaths.MiniGamesLetterOrdering + "/Data")]
    public class LetterOrderingDataSO : ScriptableObject
    {
        [Tooltip("Text shown above the slots, e.g. a Hebrew word or 'Arrange the letters'.")]
        [SerializeField] private string prompt = "Arrange the letters";

        [Tooltip("The word the player must spell, e.g. 'apple'.")]
        [SerializeField] private string targetWord;

        [Tooltip("Number of extra random distractor letters to add to the tile pool.")]
        [SerializeField] [Min(0)] private int extraDistractorCount = 2;

        [Tooltip("Optional fixed distractor characters. If empty, random letters are generated automatically.")]
        [SerializeField] private char[] customDistractors;

        [Header("Word Reveal (Optional)")]
        [Tooltip("Database used by the prompt's WordRevealSetup to show tap-to-translate tooltips. Leave empty to disable word reveal for this step.")]
        [SerializeField] private WordTranslationDatabaseSO wordRevealDatabase;

        public string Prompt => prompt;
        public string TargetWord => targetWord;
        public int ExtraDistractorCount => extraDistractorCount;
        public char[] CustomDistractors => customDistractors;
        public WordTranslationDatabaseSO WordRevealDatabase => wordRevealDatabase;
    }
}
