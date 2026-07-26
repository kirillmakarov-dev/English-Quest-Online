using UnityEngine;

namespace Puzzle.Gameplay.MiniGames.LetterConnection
{
    public interface ILetterConnectionViewFactory
    {
        LetterItemView CreateLetterItem(Transform parent);
        LetterItemView CreateLetterItem(Transform parent, string id, string value, bool isUsed);
        WordSlotView CreateWordSlot(Transform parent);
        WordSlotView CreateWordSlot(Transform parent, string id, string maskedWord, Sprite image);
        ConnectionLineView CreateConnectionLine(Transform parent);
    }
}
