using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Puzzle.Gameplay.MiniGames.LetterConnection
{
    public class LetterItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IInitializePotentialDragHandler
    {
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform anchorPoint;
        [SerializeField] private Image backgroundImage;

        public string Id { get; private set; }
        public string Value { get; private set; }
        public RectTransform RectTransform => transform as RectTransform;
        public RectTransform AnchorPoint => anchorPoint != null ? anchorPoint : RectTransform;

        public event Action<LetterItemView> Pressed;
        public event Action<LetterItemView, PointerEventData> DragStarted;
        public event Action<LetterItemView, PointerEventData> Dragged;
        public event Action<LetterItemView, PointerEventData> DragEnded;

        private void Awake()
        {
            if (valueText != null)
            {
                valueText.raycastTarget = false;
            }

            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();
        }

        public void Bind(string id, string value, bool isUsed)
        {
            Id = id;
            Value = value;

            if (valueText != null)
            {
                valueText.text = value;
            }

            SetUsed(isUsed);
        }

        public void SetUsed(bool isUsed)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = isUsed ? 0.45f : 1f;
            canvasGroup.interactable = !isUsed;
            canvasGroup.blocksRaycasts = !isUsed;
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            eventData.useDragThreshold = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Pressed?.Invoke(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            DragStarted?.Invoke(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            Dragged?.Invoke(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            DragEnded?.Invoke(this, eventData);
        }
    }
}
