namespace Puzzle.Gameplay.MiniGames.LetterConnection
{
    public interface ICompletionChecker
    {
        bool IsCompleted(LevelSession levelSession);
    }
}
