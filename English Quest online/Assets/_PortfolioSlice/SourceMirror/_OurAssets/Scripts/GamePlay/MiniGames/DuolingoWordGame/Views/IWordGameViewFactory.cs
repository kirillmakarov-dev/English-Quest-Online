using UnityEngine;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Creates TileView and SlotView instances from prefabs.
    /// Implement on a MonoBehaviour and assign prefabs in the inspector.
    /// </summary>
    public interface IWordGameViewFactory
    {
        TileView CreateTile(TileDefinition definition, Transform parent);
        SlotView CreateSlot(SlotDefinition definition, int index, Transform parent);
    }
}
