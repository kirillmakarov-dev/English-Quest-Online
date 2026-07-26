namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Strategy for validating the player's current answer against the session state.
    /// </summary>
    public interface IAnswerValidator
    {
        /// <summary>
        /// Returns true when every non-pre-filled slot contains a tile whose display value
        /// matches the slot's expected value.
        /// </summary>
        bool Validate(WordGameSession session);
    }
}
