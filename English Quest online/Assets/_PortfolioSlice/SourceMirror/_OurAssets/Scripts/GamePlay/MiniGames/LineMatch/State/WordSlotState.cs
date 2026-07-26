using System;
using System.Collections.Generic;

namespace Puzzle.Gameplay.MiniGames.LetterConnection
{
    public class WordSlotState
    {
        private sealed class MissingPosition
        {
            public int Index;
            public string Expected;
            public string Assigned;
            public bool IsFilled => !string.IsNullOrEmpty(Assigned);
        }

        private readonly List<MissingPosition> positions;

        public WordSlotState(string slotId, string fullWord, int[] missingIndices)
        {
            SlotId = slotId;
            FullWord = fullWord ?? string.Empty;
            positions = new List<MissingPosition>();

            if (missingIndices != null)
            {
                for (int i = 0; i < missingIndices.Length; i++)
                {
                    int idx = missingIndices[i];
                    string expected = (idx >= 0 && idx < FullWord.Length)
                        ? FullWord[idx].ToString()
                        : string.Empty;

                    positions.Add(new MissingPosition { Index = idx, Expected = expected, Assigned = string.Empty });
                }
            }
        }

        // Backward compat constructor (single index)
        public WordSlotState(string slotId, string fullWord, int missingIndex, string expectedLetter)
            : this(slotId, fullWord, missingIndex >= 0 ? new int[] { missingIndex } : new int[0])
        {
        }

        public string SlotId { get; }
        public string Id => SlotId;
        public string FullWord { get; }

        // Backward compat — first position
        public int MissingIndex => positions.Count > 0 ? positions[0].Index : -1;
        public int MissingLetterIndex => MissingIndex;
        public string ExpectedLetter => positions.Count > 0 ? positions[0].Expected : string.Empty;
        public string AssignedLetter => positions.Count > 0 ? positions[0].Assigned : string.Empty;
        public string CurrentLetter => AssignedLetter;

        public bool IsFilled
        {
            get
            {
                if (positions.Count == 0)
                {
                    return true;
                }

                for (int i = 0; i < positions.Count; i++)
                {
                    if (!positions[i].IsFilled)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public event Action<WordSlotState> StateChanged;

        public bool CanAssignLetter(string letter)
        {
            if (string.IsNullOrWhiteSpace(letter))
            {
                return false;
            }

            for (int i = 0; i < positions.Count; i++)
            {
                if (!positions[i].IsFilled && string.Equals(letter, positions[i].Expected, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryAssignLetter(string letter)
        {
            if (string.IsNullOrWhiteSpace(letter))
            {
                return false;
            }

            for (int i = 0; i < positions.Count; i++)
            {
                if (!positions[i].IsFilled && string.Equals(letter, positions[i].Expected, StringComparison.OrdinalIgnoreCase))
                {
                    positions[i].Assigned = letter;
                    StateChanged?.Invoke(this);
                    return true;
                }
            }

            return false;
        }

        public void Fill(string letter)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                if (!positions[i].IsFilled)
                {
                    positions[i].Assigned = letter ?? string.Empty;
                    StateChanged?.Invoke(this);
                    return;
                }
            }
        }

        public void ClearAssignedLetter()
        {
            bool changed = false;

            for (int i = 0; i < positions.Count; i++)
            {
                if (positions[i].IsFilled)
                {
                    positions[i].Assigned = string.Empty;
                    changed = true;
                }
            }

            if (changed)
            {
                StateChanged?.Invoke(this);
            }
        }

        public void Clear()
        {
            ClearAssignedLetter();
        }

        public string GetDisplayWord(string placeholder = "_")
        {
            if (string.IsNullOrEmpty(FullWord))
            {
                return FullWord;
            }

            char mask = string.IsNullOrEmpty(placeholder) ? '_' : placeholder[0];
            char[] characters = FullWord.ToCharArray();

            for (int i = 0; i < positions.Count; i++)
            {
                MissingPosition pos = positions[i];
                if (pos.Index < 0 || pos.Index >= characters.Length)
                {
                    continue;
                }

                characters[pos.Index] = pos.IsFilled ? pos.Assigned[0] : mask;
            }

            return new string(characters);
        }
    }
}
