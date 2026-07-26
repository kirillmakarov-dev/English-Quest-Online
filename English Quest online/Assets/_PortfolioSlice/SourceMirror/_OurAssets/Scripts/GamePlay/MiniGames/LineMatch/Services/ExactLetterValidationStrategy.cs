namespace Puzzle.Gameplay.MiniGames.LetterConnection
{
    public class ExactLetterValidationStrategy : IConnectionValidationStrategy
    {
        public bool IsValid(LetterState letterState, WordSlotState wordSlotState)
        {
            if (letterState == null || wordSlotState == null)
            {
                return false;
            }

            if (letterState.IsUsed)
            {
                return false;
            }

            return wordSlotState.CanAssignLetter(letterState.Value);
        }
    }
}
