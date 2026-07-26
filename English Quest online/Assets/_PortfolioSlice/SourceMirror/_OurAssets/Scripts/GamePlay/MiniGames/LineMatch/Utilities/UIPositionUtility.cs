using UnityEngine;

namespace Puzzle.Gameplay.MiniGames.LetterConnection
{
    public static class UIPositionUtility
    {
        public static Vector2 GetScreenPosition(RectTransform rectTransform, Camera camera = null)
        {
            if (rectTransform == null)
            {
                return Vector2.zero;
            }

            return RectTransformUtility.WorldToScreenPoint(camera, rectTransform.position);
        }

        public static bool TryScreenPointToLocalPoint(RectTransform container, Vector2 screenPoint, Camera camera, out Vector2 localPoint)
        {
            if (container == null)
            {
                localPoint = Vector2.zero;
                return false;
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screenPoint, camera, out localPoint);
        }

        public static Vector2 ScreenToLocalPoint(RectTransform container, Vector2 screenPoint, Camera camera = null)
        {
            return TryScreenPointToLocalPoint(container, screenPoint, camera, out Vector2 localPoint)
                ? localPoint
                : Vector2.zero;
        }

        public static Vector2 WorldToLocalPoint(RectTransform container, Vector3 worldPosition, Camera camera = null)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);
            return ScreenToLocalPoint(container, screenPoint, camera);
        }
    }
}
