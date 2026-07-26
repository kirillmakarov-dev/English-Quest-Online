namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Immutable data describing a single answer slot.
    /// Created by an IWordGameMode and consumed by WordGameSession and WordGamePanelView.
    /// </summary>
    public readonly struct SlotDefinition
    {
        public readonly string ExpectedValue;
        public readonly bool IsPreFilled;
        public readonly string PreFilledDisplayValue;

        public SlotDefinition(string expectedValue, bool isPreFilled = false, string preFilledDisplayValue = null)
        {
            ExpectedValue = expectedValue;
            IsPreFilled = isPreFilled;
            PreFilledDisplayValue = isPreFilled ? (preFilledDisplayValue ?? expectedValue) : null;
        }
    }
}
