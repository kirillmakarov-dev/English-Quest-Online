namespace Puzzle.Gameplay.MiniGames.LetterConnection
{
    public interface IConnectionValidationStrategy
    {
        bool IsValid(LetterState letterState, WordSlotState wordSlotState);
    }
}
