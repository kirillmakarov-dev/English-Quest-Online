using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Puzzle.Gameplay.MiniGames.LetterConnection
{
    public class WordSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TMP_Text wordText;
        [SerializeField] private Image illustrationImage;
        [SerializeField] private RectTransform dropZone;
        [SerializeField] private RectTransform anchorPoint;
        [SerializeField] private GameObject highlightObject;
        [SerializeField] private Color normalTextColor = Color.white;
        [SerializeField] private Color wrongFeedbackColor = new Color(1f, 0.35f, 0.35f);
        [SerializeField] private float wrongFeedbackDuration = 0.2f;

        private Coroutine wrongFeedbackRoutine;

        public string Id { get; private set; }
        public RectTransform RectTransform => transform as RectTransform;
        public RectTransform DropZone => dropZone != null ? dropZone : RectTransform;
        public RectTransform AnchorPoint => anchorPoint != null ? anchorPoint : DropZone;

        public event Action<WordSlotView> PointerEntered;
        public event Action<WordSlotView> PointerExited;

        public void Bind(string id, string maskedWord, Sprite image)
        {
            Id = id;
            ShowMaskedWord(maskedWord);

            if (illustrationImage != null)
            {
                illustrationImage.sprite = image;
                illustrationImage.enabled = image != null;
            }

            SetHighlighted(false);
        }

        public void ShowMaskedWord(string maskedWord)
        {
            if (wordText != null)
            {
                wordText.text = maskedWord;
            }
        }

        public void InsertLetterVisual(string resolvedWord)
        {
            if (wordText != null)
            {
                wordText.text = resolvedWord;
            }
        }

        public void SetWordText(string displayWord)
        {
            ShowMaskedWord(displayWord);
        }

        public bool IsPointerOverDropZone(Vector2 screenPosition, Camera eventCamera = null)
        {
            return DropZone != null && RectTransformUtility.RectangleContainsScreenPoint(DropZone, screenPosition, eventCamera);
        }

        public void SetHighlighted(bool isHighlighted)
        {
            if (highlightObject != null)
            {
                highlightObject.SetActive(isHighlighted);
            }
        }

        public void PlayWrongFeedback()
        {
            if (wrongFeedbackRoutine != null)
            {
                StopCoroutine(wrongFeedbackRoutine);
            }

            wrongFeedbackRoutine = StartCoroutine(PlayWrongFeedbackRoutine());
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PointerEntered?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PointerExited?.Invoke(this);
        }

        private IEnumerator PlayWrongFeedbackRoutine()
        {
            if (wordText != null)
            {
                wordText.color = wrongFeedbackColor;
            }

            yield return new WaitForSeconds(wrongFeedbackDuration);

            if (wordText != null)
            {
                wordText.color = normalTextColor;
            }

            wrongFeedbackRoutine = null;
        }
    }
}
