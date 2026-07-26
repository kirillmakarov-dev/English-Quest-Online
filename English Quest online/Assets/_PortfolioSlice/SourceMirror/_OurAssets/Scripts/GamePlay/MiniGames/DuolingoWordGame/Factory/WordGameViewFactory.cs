using UnityEngine;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Instantiates TileView and SlotView prefabs and initializes them.
    /// </summary>
    public class WordGameViewFactory : MonoBehaviour, IWordGameViewFactory
    {
        [SerializeField] private TileView _tilePrefab;
        [SerializeField] private SlotView _slotPrefab;

        public TileView CreateTile(TileDefinition definition, Transform parent)
        {
            TileView instance = Instantiate(_tilePrefab, parent);
            instance.Initialize(definition);
            return instance;
        }

        public SlotView CreateSlot(SlotDefinition definition, int index, Transform parent)
        {
            SlotView instance = Instantiate(_slotPrefab, parent);
            instance.Initialize(definition, index);
            return instance;
        }
    }
}
