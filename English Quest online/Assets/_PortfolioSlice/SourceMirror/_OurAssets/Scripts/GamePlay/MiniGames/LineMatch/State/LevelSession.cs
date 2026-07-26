using System;
using System.Collections.Generic;

namespace Puzzle.Gameplay.MiniGames.LetterConnection
{
    public class LevelSession : IDisposable
    {
        private readonly List<LetterState> letters;
        private readonly List<WordSlotState> wordSlots;
        private readonly Dictionary<string, LetterState> lettersById;
        private readonly Dictionary<string, WordSlotState> wordSlotsById;

        private bool levelCompletedRaised;

        public LevelSession(LetterConnectionLevelConfigSO levelConfig)
            : this(CreateLetters(levelConfig), CreateWordSlots(levelConfig))
        {
        }

        public LevelSession(IEnumerable<LetterState> letters, IEnumerable<WordSlotState> wordSlots)
        {
            this.letters = letters != null ? new List<LetterState>(letters) : new List<LetterState>();
            this.wordSlots = wordSlots != null ? new List<WordSlotState>(wordSlots) : new List<WordSlotState>();
            lettersById = new Dictionary<string, LetterState>(StringComparer.OrdinalIgnoreCase);
            wordSlotsById = new Dictionary<string, WordSlotState>(StringComparer.OrdinalIgnoreCase);

            IndexStates();
            Subscribe();
        }

        public IReadOnlyList<LetterState> Letters => letters;
        public IReadOnlyList<WordSlotState> WordSlots => wordSlots;

        public event Action<LetterState, WordSlotState> OnConnectionSucceeded;
        public event Action<LetterState, WordSlotState> OnConnectionFailed;
        public event Action OnLevelCompleted;
        public event Action SessionChanged;

        public LetterState GetLetterState(string letterId)
        {
            if (string.IsNullOrWhiteSpace(letterId))
            {
                return null;
            }

            lettersById.TryGetValue(letterId, out LetterState state);
            return state;
        }

        public WordSlotState GetWordSlotState(string slotId)
        {
            if (string.IsNullOrWhiteSpace(slotId))
            {
                return null;
            }

            wordSlotsById.TryGetValue(slotId, out WordSlotState state);
            return state;
        }

        public bool TryConnect(string letterId, string slotId)
        {
            LetterState letterState = GetLetterState(letterId);
            WordSlotState wordSlotState = GetWordSlotState(slotId);

            if (letterState == null || wordSlotState == null || letterState.IsUsed)
            {
                NotifyConnectionFailed(letterState, wordSlotState);
                return false;
            }

            if (!wordSlotState.TryAssignLetter(letterState.Value))
            {
                NotifyConnectionFailed(letterState, wordSlotState);
                return false;
            }

            letterState.MarkUsed();
            NotifyConnectionSucceeded(letterState, wordSlotState);
            return true;
        }

        public bool IsLevelFilled()
        {
            if (wordSlots.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < wordSlots.Count; i++)
            {
                if (!wordSlots[i].IsFilled)
                {
                    return false;
                }
            }

            return true;
        }

        public void NotifyConnectionSucceeded(LetterState letterState, WordSlotState wordSlotState)
        {
            OnConnectionSucceeded?.Invoke(letterState, wordSlotState);
            SessionChanged?.Invoke();

            if (levelCompletedRaised || !IsLevelFilled())
            {
                return;
            }

            levelCompletedRaised = true;
            OnLevelCompleted?.Invoke();
        }

        public void NotifyConnectionFailed(LetterState letterState, WordSlotState wordSlotState)
        {
            OnConnectionFailed?.Invoke(letterState, wordSlotState);
            SessionChanged?.Invoke();
        }

        public void Reset()
        {
            levelCompletedRaised = false;

            for (int i = 0; i < letters.Count; i++)
            {
                letters[i].Reset();
            }

            for (int i = 0; i < wordSlots.Count; i++)
            {
                wordSlots[i].ClearAssignedLetter();
            }

            SessionChanged?.Invoke();
        }

        public void Dispose()
        {
            Unsubscribe();
        }

        private void IndexStates()
        {
            for (int i = 0; i < letters.Count; i++)
            {
                LetterState letter = letters[i];
                if (letter == null || string.IsNullOrWhiteSpace(letter.LetterId) || lettersById.ContainsKey(letter.LetterId))
                {
                    continue;
                }

                lettersById.Add(letter.LetterId, letter);
            }

            for (int i = 0; i < wordSlots.Count; i++)
            {
                WordSlotState slot = wordSlots[i];
                if (slot == null || string.IsNullOrWhiteSpace(slot.SlotId) || wordSlotsById.ContainsKey(slot.SlotId))
                {
                    continue;
                }

                wordSlotsById.Add(slot.SlotId, slot);
            }
        }

        private void Subscribe()
        {
            for (int i = 0; i < letters.Count; i++)
            {
                if (letters[i] != null)
                {
                    letters[i].StateChanged += HandleStateChanged;
                }
            }

            for (int i = 0; i < wordSlots.Count; i++)
            {
                if (wordSlots[i] != null)
                {
                    wordSlots[i].StateChanged += HandleStateChanged;
                }
            }
        }

        private void Unsubscribe()
        {
            for (int i = 0; i < letters.Count; i++)
            {
                if (letters[i] != null)
                {
                    letters[i].StateChanged -= HandleStateChanged;
                }
            }

            for (int i = 0; i < wordSlots.Count; i++)
            {
                if (wordSlots[i] != null)
                {
                    wordSlots[i].StateChanged -= HandleStateChanged;
                }
            }
        }

        private void HandleStateChanged(LetterState _)
        {
            SessionChanged?.Invoke();
        }

        private void HandleStateChanged(WordSlotState _)
        {
            SessionChanged?.Invoke();
        }

        private static IEnumerable<LetterState> CreateLetters(LetterConnectionLevelConfigSO levelConfig)
        {
            List<LetterState> result = new List<LetterState>();
            if (levelConfig == null)
            {
                return result;
            }

            for (int i = 0; i < levelConfig.Letters.Count; i++)
            {
                LetterData data = levelConfig.Letters[i];
                if (data == null)
                {
                    continue;
                }

                result.Add(new LetterState(data.Id, data.Value));
            }

            return result;
        }

        private static IEnumerable<WordSlotState> CreateWordSlots(LetterConnectionLevelConfigSO levelConfig)
        {
            List<WordSlotState> result = new List<WordSlotState>();
            if (levelConfig == null)
            {
                return result;
            }

            for (int i = 0; i < levelConfig.WordTasks.Count; i++)
            {
                WordTaskData data = levelConfig.WordTasks[i];
                if (data == null)
                {
                    continue;
                }

                result.Add(new WordSlotState(data.Id, data.FullWord, data.MissingIndices));
            }

            return result;
        }
    }
}
