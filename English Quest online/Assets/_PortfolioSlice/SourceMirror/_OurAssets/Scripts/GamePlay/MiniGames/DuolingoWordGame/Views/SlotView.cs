using System;
using EnglishQuest.PortfolioDemo;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Represents a single answer slot in the word game.
    /// Pre-filled slots display their value immediately and ignore all input.
    /// Empty slots accept clicks (to request removal of a previously placed tile)
    /// and accept drag-and-drop from TileViews.
    /// </summary>
    public class SlotView : MonoBehaviour, IPointerClickHandler, IDropHandler
    {
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private GameObject _emptyIndicator;
        [SerializeField] private Image _backgroundImage;

        public int SlotIndex { get; private set; }
        public bool IsPreFilled { get; private set; }

        /// <summary>Fired when the player clicks a filled, non-pre-filled slot (intent: remove tile).</summary>
        public event Action<int> Clicked;

        /// <summary>Fired when a TileView is drag-dropped onto this slot. Passes (tileId, slotIndex).</summary>
        public event Action<string, int> TileDropped;

        public void Initialize(SlotDefinition definition, int index)
        {
            SlotIndex = index;
            IsPreFilled = definition.IsPreFilled;

            if (_backgroundImage == null)
                _backgroundImage = GetComponent<Image>();

            if (IsPreFilled)
                SetDisplay(definition.PreFilledDisplayValue);
            else
                SetDisplay(string.Empty);
        }

        /// <summary>Updates the displayed text. Pass an empty string to show the slot as empty.</summary>
        public void SetDisplay(string value)
        {
            bool isEmpty = string.IsNullOrEmpty(value);

            if (_label != null)
                _label.text = isEmpty ? string.Empty : value;

            if (_emptyIndicator != null)
                _emptyIndicator.SetActive(isEmpty && !IsPreFilled);

            PortfolioThemeResources.ApplySlotSurface(_backgroundImage, _label, isFilled: !isEmpty, isPreFilled: IsPreFilled);
        }

        // ── IPointerClickHandler ─────────────────────────────────────────────

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (IsPreFilled) return;
            Clicked?.Invoke(SlotIndex);
        }

        // ── IDropHandler ─────────────────────────────────────────────────────

        void IDropHandler.OnDrop(PointerEventData eventData)
        {
            if (IsPreFilled) return;

            TileView tile = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<TileView>()
                : null;

            if (tile == null || tile.IsUsed) return;

            TileDropped?.Invoke(tile.TileId, SlotIndex);
        }
    }
}
