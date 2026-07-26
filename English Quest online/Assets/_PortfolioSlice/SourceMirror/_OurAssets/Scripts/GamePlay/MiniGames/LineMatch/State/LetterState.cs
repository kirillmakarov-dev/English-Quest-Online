using System;

namespace Puzzle.Gameplay.MiniGames.LetterConnection
{
    public class LetterState
    {
        public LetterState(string letterId, string value)
        {
            LetterId = letterId;
            Value = value;
        }

        public string LetterId { get; }
        public string Id => LetterId;
        public string Value { get; }
        public bool IsUsed { get; private set; }

        public event Action<LetterState> StateChanged;

        public void MarkUsed()
        {
            if (IsUsed)
            {
                return;
            }

            IsUsed = true;
            StateChanged?.Invoke(this);
        }

        public void Reset()
        {
            if (!IsUsed)
            {
                return;
            }

            IsUsed = false;
            StateChanged?.Invoke(this);
        }

        public void ResetUsage()
        {
            Reset();
        }
    }
}
