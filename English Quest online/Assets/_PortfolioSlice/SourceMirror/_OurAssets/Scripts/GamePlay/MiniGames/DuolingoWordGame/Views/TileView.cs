using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Represents a single selectable letter or word tile in the pool.
    /// Primary interaction: click-to-place. Secondary: drag-and-drop onto a SlotView.
    ///
    /// When used (placed in a slot) the tile becomes visually dimmed and non-interactive.
    /// During drag the tile follows the cursor; on drag end it returns to its pool position.
    /// Assumes the panel canvas is Screen Space – Overlay. For other modes, override OnDrag.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class TileView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private float _usedAlpha = 0.35f;
        [SerializeField] private Image _backgroundImage;

        private CanvasGroup _canvasGroup;
        private Canvas _rootCanvas;
        private Transform _originalParent;
        private int _originalSiblingIndex;
        private bool _isDragging;

        public string TileId { get; private set; }
        public string DisplayValue { get; private set; }
        public bool IsUsed { get; private set; }

        /// <summary>Fired when the player clicks this tile. Passes the tile ID.</summary>
        public event Action<string> Clicked;

        /// <summary>Fired when a drag ends and this tile was NOT captured by a SlotView.IDropHandler.</summary>
        public event Action<string> DragCancelled;

        public void Initialize(TileDefinition definition)
        {
            ResolveReferences();

            TileId = definition.Id;
            DisplayValue = definition.DisplayValue;

            _canvasGroup = GetComponent<CanvasGroup>();
            _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;

            if (_label != null)
                _label.text = DisplayValue;
        }

        public void SetUsed(bool isUsed)
        {
            ResolveReferences();

            IsUsed = isUsed;
            _canvasGroup.alpha = isUsed ? _usedAlpha : 1f;
            _canvasGroup.interactable = !isUsed;
            _canvasGroup.blocksRaycasts = !isUsed;
        }

        // ── IPointerClickHandler ─────────────────────────────────────────────

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (_isDragging || IsUsed) return;
            Clicked?.Invoke(TileId);
        }

        // ── Drag interfaces ──────────────────────────────────────────────────

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (IsUsed)
            {
                // Cancel the drag — tell EventSystem to ignore further drag events for this pointer
                eventData.pointerDrag = null;
                return;
            }

            _isDragging = true;
            _originalParent = transform.parent;
            _originalSiblingIndex = transform.GetSiblingIndex();

            // Reparent to root canvas so tile renders on top of everything
            if (_rootCanvas != null)
                transform.SetParent(_rootCanvas.transform, worldPositionStays: true);

            transform.SetAsLastSibling();

            // Allow raycasts to pass through so underlying SlotViews can receive OnDrop
            _canvasGroup.blocksRaycasts = false;
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            // Screen Space – Overlay: position == screen position
            transform.position = eventData.position;
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;

            // Return to pool container. Layout group will re-position it.
            if (_originalParent != null)
            {
                transform.SetParent(_originalParent, worldPositionStays: false);
                transform.SetSiblingIndex(_originalSiblingIndex);
            }

            _canvasGroup.blocksRaycasts = !IsUsed;

            // If the tile is still free it means no SlotView captured the drop
            if (!IsUsed)
                DragCancelled?.Invoke(TileId);
        }

        private void ResolveReferences()
        {
            if (_label == null)
                _label = GetComponentInChildren<TextMeshProUGUI>(true);

            if (_backgroundImage == null)
                _backgroundImage = GetComponent<Image>() ?? GetComponentInChildren<Image>(true);

            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
        }
    }
}
