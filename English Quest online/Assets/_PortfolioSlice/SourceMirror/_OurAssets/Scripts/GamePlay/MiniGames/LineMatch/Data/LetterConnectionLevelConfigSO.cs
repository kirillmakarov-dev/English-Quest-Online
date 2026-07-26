using System.Collections.Generic;
using UnityEngine;

namespace Puzzle.Gameplay.MiniGames.LetterConnection
{
    [CreateAssetMenu(fileName = "LetterConnectionLevelConfig", menuName = ScriptableObjectMenuPaths.MiniGamesLetterOrdering + "/Level Config")]
    public class LetterConnectionLevelConfigSO : ScriptableObject
    {
        [SerializeField] private List<LetterData> letters = new List<LetterData>();
        [SerializeField] private List<WordTaskData> wordTasks = new List<WordTaskData>();

        public IReadOnlyList<LetterData> Letters => letters;
        public IReadOnlyList<WordTaskData> WordTasks => wordTasks;

        public IReadOnlyList<LetterData> AvailableLetters => letters;
    }
}
