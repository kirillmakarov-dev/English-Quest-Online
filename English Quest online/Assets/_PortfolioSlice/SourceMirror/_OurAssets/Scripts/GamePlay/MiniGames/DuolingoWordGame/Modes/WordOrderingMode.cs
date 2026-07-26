using System;
using System.Collections.Generic;
using Puzzle.Gameplay.Features.WordReveal;
using UnityEngine;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Mode component: player arranges English words to match a Hebrew prompt.
    /// Supports pre-filled (locked) slots and optional distractor words.
    /// Example: Hebrew="הכלב גדול" → slots: [the][dog][is][big], with index 0 pre-filled.
    /// </summary>
    public class WordOrderingMode : MonoBehaviour, IWordGameMode
    {
        [SerializeField] private WordOrderingDataSO _data;

        /// <summary>Overrides the inspector data at runtime. Called by WordOrderingQuestStep.</summary>
        public void SetData(WordOrderingDataSO data) => _data = data;

        public string Prompt => _data != null ? _data.Prompt : string.Empty;
        public WordTranslationDatabaseSO WordRevealDatabase => _data != null ? _data.WordRevealDatabase : null;

        public SlotDefinition[] BuildSlots()
        {
            if (_data == null || _data.EnglishWordsInOrder == null || _data.EnglishWordsInOrder.Length == 0)
                return Array.Empty<SlotDefinition>();

            string[] words = _data.EnglishWordsInOrder;
            var preFilledSet = BuildPreFilledSet();
            var slots = new SlotDefinition[words.Length];

            for (int i = 0; i < words.Length; i++)
            {
                bool isFilled = preFilledSet.Contains(i);
                slots[i] = new SlotDefinition(words[i], isFilled, words[i]);
            }

            return slots;
        }

        public TileDefinition[] BuildTiles()
        {
            if (_data == null || _data.EnglishWordsInOrder == null)
                return Array.Empty<TileDefinition>();

            string[] words = _data.EnglishWordsInOrder;
            var preFilledSet = BuildPreFilledSet();
            var tiles = new List<TileDefinition>();

            // Non-pre-filled words become movable tiles
            for (int i = 0; i < words.Length; i++)
            {
                if (!preFilledSet.Contains(i))
                    tiles.Add(new TileDefinition(Guid.NewGuid().ToString(), words[i], isDistractor: false));
            }

            // Distractor words
            if (_data.DistractorWords != null)
            {
                foreach (string distractor in _data.DistractorWords)
                {
                    if (!string.IsNullOrWhiteSpace(distractor))
                        tiles.Add(new TileDefinition(Guid.NewGuid().ToString(), distractor, isDistractor: true));
                }
            }

            Shuffle(tiles);
            return tiles.ToArray();
        }

        private HashSet<int> BuildPreFilledSet()
        {
            var set = new HashSet<int>();
            if (_data.PreFilledIndices != null)
            {
                foreach (int idx in _data.PreFilledIndices)
                    set.Add(idx);
            }
            return set;
        }

        private static void Shuffle<T>(List<T> list)
        {
            var rng = new System.Random();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
