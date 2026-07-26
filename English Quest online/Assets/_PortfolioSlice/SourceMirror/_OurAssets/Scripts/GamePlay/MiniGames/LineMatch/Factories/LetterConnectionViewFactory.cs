using UnityEngine;

namespace Puzzle.Gameplay.MiniGames.LetterConnection
{
    public class LetterConnectionViewFactory : MonoBehaviour, ILetterConnectionViewFactory
    {
        [SerializeField] private LetterItemView letterItemPrefab;
        [SerializeField] private WordSlotView wordSlotPrefab;
        [SerializeField] private ConnectionLineView connectionLinePrefab;

        public LetterItemView CreateLetterItem(Transform parent)
        {
            return CreateInstance(letterItemPrefab, parent, "LetterItemView");
        }

        public LetterItemView CreateLetterItem(Transform parent, string id, string value, bool isUsed)
        {
            LetterItemView view = CreateLetterItem(parent);
            if (view != null)
            {
                view.Bind(id, value, isUsed);
                view.gameObject.name = string.IsNullOrWhiteSpace(id) ? "LetterItemView" : $"Letter_{id}";
            }

            return view;
        }

        public WordSlotView CreateWordSlot(Transform parent)
        {
            return CreateInstance(wordSlotPrefab, parent, "WordSlotView");
        }

        public WordSlotView CreateWordSlot(Transform parent, string id, string maskedWord, Sprite image)
        {
            WordSlotView view = CreateWordSlot(parent);
            if (view != null)
            {
                view.Bind(id, maskedWord, image);
                view.gameObject.name = string.IsNullOrWhiteSpace(id) ? "WordSlotView" : $"WordSlot_{id}";
            }

            return view;
        }

        public ConnectionLineView CreateConnectionLine(Transform parent)
        {
            return CreateInstance(connectionLinePrefab, parent, "ConnectionLineView");
        }

        private static T CreateInstance<T>(T prefab, Transform parent, string objectName) where T : Component
        {
            if (prefab == null)
            {
                return null;
            }

            T instance = Instantiate(prefab, parent, false);
            instance.gameObject.name = objectName;
            ResetRectTransform(instance.transform as RectTransform);
            return instance;
        }

        private static void ResetRectTransform(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
        }
    }
}
