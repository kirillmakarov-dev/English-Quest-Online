using System;
using System.Collections.Generic;
using Puzzle.Gameplay.Features.WordReveal;
using UnityEngine;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Mode component: player arranges scrambled letters to spell a target word.
    /// Example: word="apple" → 5 letter slots + shuffled {a,p,p,l,e} tiles + distractors.
    /// </summary>
    public class LetterOrderingMode : MonoBehaviour, IWordGameMode
    {
        private static readonly char[] DistractorAlphabet = "abcdefghijklmnopqrstuvwxyz".ToCharArray();

        [SerializeField] private LetterOrderingDataSO _data;

        /// <summary>Overrides the inspector data at runtime. Called by LetterOrderingQuestStep.</summary>
        public void SetData(LetterOrderingDataSO data) => _data = data;

        public string Prompt => _data != null ? _data.Prompt : string.Empty;
        public WordTranslationDatabaseSO WordRevealDatabase => _data != null ? _data.WordRevealDatabase : null;

        public SlotDefinition[] BuildSlots()
        {
            if (_data == null || string.IsNullOrEmpty(_data.TargetWord))
                return Array.Empty<SlotDefinition>();

            string word = _data.TargetWord.ToLower();
            var slots = new SlotDefinition[word.Length];
            for (int i = 0; i < word.Length; i++)
                slots[i] = new SlotDefinition(word[i].ToString());

            return slots;
        }

        public TileDefinition[] BuildTiles()
        {
            if (_data == null || string.IsNullOrEmpty(_data.TargetWord))
                return Array.Empty<TileDefinition>();

            string word = _data.TargetWord.ToLower();
            var tiles = new List<TileDefinition>(word.Length + _data.ExtraDistractorCount);

            // One tile per letter, including duplicates (e.g. "apple" → two 'p' tiles with distinct IDs)
            foreach (char c in word)
                tiles.Add(new TileDefinition(Guid.NewGuid().ToString(), c.ToString(), isDistractor: false));

            // Distractor tiles
            foreach (char d in PickDistractors(word, _data.ExtraDistractorCount))
                tiles.Add(new TileDefinition(Guid.NewGuid().ToString(), d.ToString(), isDistractor: true));

            Shuffle(tiles);
            return tiles.ToArray();
        }

        private char[] PickDistractors(string word, int count)
        {
            if (count <= 0) return Array.Empty<char>();

            char[] custom = _data.CustomDistractors;
            if (custom != null && custom.Length > 0)
            {
                int take = Math.Min(count, custom.Length);
                char[] result = new char[take];
                Array.Copy(custom, result, take);
                return result;
            }

            // Auto-generate from a–z, excluding letters already in the word
            var usedLetters = new HashSet<char>(word.ToCharArray());
            var pool = new List<char>(DistractorAlphabet.Length);
            foreach (char c in DistractorAlphabet)
            {
                if (!usedLetters.Contains(c))
                    pool.Add(c);
            }

            Shuffle(pool);
            int takeCount = Math.Min(count, pool.Count);
            char[] distractors = new char[takeCount];
            for (int i = 0; i < takeCount; i++)
                distractors[i] = pool[i];
            return distractors;
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
