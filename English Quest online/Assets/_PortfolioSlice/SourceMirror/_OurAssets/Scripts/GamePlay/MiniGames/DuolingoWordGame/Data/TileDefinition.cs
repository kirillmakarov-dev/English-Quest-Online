namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Immutable data describing a single selectable tile (letter or word).
    /// Created by an IWordGameMode and consumed by WordGameSession and WordGamePanelView.
    /// </summary>
    public readonly struct TileDefinition
    {
        public readonly string Id;
        public readonly string DisplayValue;
        public readonly bool IsDistractor;

        public TileDefinition(string id, string displayValue, bool isDistractor = false)
        {
            Id = id;
            DisplayValue = displayValue;
            IsDistractor = isDistractor;
        }
    }
}
