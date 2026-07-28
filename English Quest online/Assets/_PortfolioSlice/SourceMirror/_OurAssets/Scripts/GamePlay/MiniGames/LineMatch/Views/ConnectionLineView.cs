using UnityEngine;
using UnityEngine.UI;

namespace Puzzle.Gameplay.MiniGames.LetterConnection
{
    public class ConnectionLineView : MonoBehaviour
    {
        [SerializeField] private RectTransform lineRectTransform;
        [SerializeField] private Image lineImage;
        [SerializeField] private float thickness = 8f;

        private Vector2 startPoint;
        private Vector2 endPoint;

        private void Awake()
        {
            if (lineRectTransform == null)
            {
                lineRectTransform = transform as RectTransform;
            }

            if (lineRectTransform != null)
            {
                lineRectTransform.pivot = new Vector2(0f, 0.5f);
            }

            if (lineImage != null)
            {
                lineImage.raycastTarget = false;
            }
        }

        public void SetStart(Vector2 start)
        {
            startPoint = start;
            Refresh();
        }

        public void SetEnd(Vector2 end)
        {
            endPoint = end;
            Refresh();
        }

        public void SetPositions(Vector2 start, Vector2 end)
        {
            startPoint = start;
            endPoint = end;
            Refresh();
        }

        public void SetColor(Color color)
        {
            if (lineImage != null)
            {
                lineImage.color = color;
            }
        }

        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }

        private void Refresh()
        {
            if (lineRectTransform == null)
            {
                return;
            }

            Vector2 direction = endPoint - startPoint;
            float length = direction.magnitude;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            lineRectTransform.anchoredPosition = startPoint;
            lineRectTransform.sizeDelta = new Vector2(length, thickness);
            lineRectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
