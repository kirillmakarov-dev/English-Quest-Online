using System;
using UnityEngine;

namespace Puzzle.Gameplay.MiniGames.LetterConnection
{
    [Serializable]
    public class WordTaskData
    {
        [SerializeField] private string id;
        [SerializeField] private Sprite image;
        [SerializeField] private string fullWord;
        [SerializeField] private int[] missingIndices = new int[0];

        public string Id => id;
        public Sprite Image => image;
        public string FullWord => fullWord;
        public int[] MissingIndices => missingIndices ?? new int[0];

        // Backward compatibility — first missing index
        public int MissingIndex => missingIndices != null && missingIndices.Length > 0 ? missingIndices[0] : -1;
        public int MissingLetterIndex => MissingIndex;
        public string ExpectedLetter => ExtractLetterAt(MissingIndex);
        public string MissingLetter => ExpectedLetter;

        public string GetMaskedWord(string placeholder = "_")
        {
            if (string.IsNullOrEmpty(fullWord) || missingIndices == null || missingIndices.Length == 0)
            {
                return fullWord;
            }

            char mask = string.IsNullOrEmpty(placeholder) ? '_' : placeholder[0];
            char[] characters = fullWord.ToCharArray();

            for (int i = 0; i < missingIndices.Length; i++)
            {
                int idx = missingIndices[i];
                if (idx >= 0 && idx < characters.Length)
                {
                    characters[idx] = mask;
                }
            }

            return new string(characters);
        }

        private string ExtractLetterAt(int index)
        {
            if (string.IsNullOrEmpty(fullWord) || index < 0 || index >= fullWord.Length)
            {
                return string.Empty;
            }

            return fullWord[index].ToString();
        }
    }
}
