using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Puzzle.Gameplay.Features.WordReveal
{
    /// <summary>
    /// Duolingo-style word-reveal popup.
    ///
    /// Prefab hierarchy expected:
    ///
    ///   WordRevealPopup  (this component lives here)
    ///   └── PopupContainer   — RectTransform, positioned at runtime
    ///       ├── Background   — Image (rounded tooltip)
    ///       ├── Arrow        — Image (downward triangle, anchored bottom-center)
    ///       └── TranslationText — TextMeshProUGUI
    ///
    /// The popup is always a direct child of the root Canvas so it renders on top of everything.
    /// </summary>
    public class WordRevealPopupView : MonoBehaviour, IWordRevealPopup
    {
        [Header("References")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _popupContainer;
        [SerializeField] private TextMeshProUGUI _translationLabel;

        [Header("Animation")]
        [SerializeField, Range(0.05f, 0.5f)] private float _fadeDuration = 0.15f;

        [Header("Layout")]
        [Tooltip("Vertical offset (pixels) above the word's top edge.")]
        [SerializeField] private float _verticalOffset = 12f;

        [Tooltip("Horizontal padding to keep the popup inside the canvas (pixels).")]
        [SerializeField] private float _horizontalPadding = 16f;

        private Canvas _canvas;

        // ── IWordRevealPopup ──────────────────────────────────────────────────

        public bool IsVisible { get; private set; }

        public void Show(string translation, Vector2 screenPosition)
        {
            // Update text first so the layout is ready for position clamping.
            _translationLabel.text = translation;
            RtlDetector.Apply(_translationLabel, translation);

            PositionAbove(screenPosition);

            StopAllCoroutines();
            StartCoroutine(FadeRoutine(0f, 1f, () => IsVisible = true));
        }

        public void Hide()
        {
            if (!IsVisible && _canvasGroup.alpha <= 0f)
                return;

            StopAllCoroutines();
            StartCoroutine(FadeRoutine(1f, 0f, () => IsVisible = false));
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            _canvas = GetComponentInParent<Canvas>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            IsVisible = false;
        }

        private void Update()
        {
            if (!IsVisible || !TryGetTapPosition(out Vector2 tapPosition))
                return;

            Camera cam = _canvas != null ? _canvas.worldCamera : null;
            if (!RectTransformUtility.RectangleContainsScreenPoint(_popupContainer, tapPosition, cam))
                Hide();
        }

        private static bool TryGetTapPosition(out Vector2 position)
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen?.primaryTouch.press.wasPressedThisFrame == true)
            {
                position = touchscreen.primaryTouch.position.ReadValue();
                return true;
            }

            Mouse mouse = Mouse.current;
            if (mouse?.leftButton.wasPressedThisFrame == true)
            {
                position = mouse.position.ReadValue();
                return true;
            }

            position = default;
            return false;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void PositionAbove(Vector2 screenPosition)
        {
            if (_canvas == null || _popupContainer == null)
                return;

            RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
            Camera cam = _canvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPosition, cam, out Vector2 localPoint);

            // Offset the popup so its bottom sits above the word.
            float popupHeight = _popupContainer.rect.height;
            localPoint.y += _verticalOffset + popupHeight * 0.5f;

            // Clamp horizontally so the popup never overflows the canvas edges.
            float canvasHalfWidth = canvasRect.rect.width * 0.5f;
            float popupHalfWidth = _popupContainer.rect.width * 0.5f;
            float minX = -canvasHalfWidth + popupHalfWidth + _horizontalPadding;
            float maxX = canvasHalfWidth - popupHalfWidth - _horizontalPadding;
            localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);

            _popupContainer.anchoredPosition = localPoint;
        }

        private IEnumerator FadeRoutine(float from, float to, System.Action onComplete)
        {
            _canvasGroup.blocksRaycasts = to > 0f;

            float elapsed = 0f;
            _canvasGroup.alpha = from;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / _fadeDuration);
                yield return null;
            }

            _canvasGroup.alpha = to;
            onComplete?.Invoke();
        }
    }
}
