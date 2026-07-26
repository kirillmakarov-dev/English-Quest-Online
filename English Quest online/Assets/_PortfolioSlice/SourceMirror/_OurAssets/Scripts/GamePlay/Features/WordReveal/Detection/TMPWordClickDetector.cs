using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Puzzle.Gameplay.Features.WordReveal
{
    /// <summary>
    /// Attach to any GameObject that has a <see cref="TMP_Text"/> component.
    /// Detects which word in the text the player clicked and fires <see cref="WordClicked"/>
    /// with the raw word string and its screen-space top-center position.
    ///
    /// Requirements:
    ///   • "Raycast Target" must be enabled on the <see cref="TMP_Text"/> component.
    ///   • The parent Canvas needs a <see cref="GraphicRaycaster"/>.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TMPWordClickDetector : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>
        /// Fired when a word is successfully detected.
        /// <c>allWords</c> contains every word in the TMP text (in order);
        /// <c>clickedWordIndex</c> is the index of the tapped word inside that array;
        /// <c>screenPosition</c> is the screen-space top-center of the tapped word's
        /// bounding box (use this to anchor the popup).
        /// </summary>
        public event Action<string[], int, Vector2> WordClicked;

        private TMP_Text _text;
        private Camera _uiCamera;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();

            // Resolve the camera used for this canvas.
            // Screen-Space Overlay → camera is null (TMP_TextUtilities expects null).
            // Screen-Space Camera / World Space → use the canvas render camera.
            var canvas = GetComponentInParent<Canvas>();
            _uiCamera = canvas != null ? canvas.worldCamera : null;
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            // Force TMP to update its layout/mesh so characterInfo is accurate.
            _text.ForceMeshUpdate();

            int wordIndex = TMP_TextUtilities.FindIntersectingWord(_text, eventData.position, _uiCamera);
            if (wordIndex < 0)
                return;

            TMP_TextInfo textInfo = _text.textInfo;
            if (wordIndex >= textInfo.wordCount)
                return;

            TMP_WordInfo wordInfo = textInfo.wordInfo[wordIndex];

            // Skip words that are just whitespace.
            string rawWord = wordInfo.GetWord();
            if (string.IsNullOrWhiteSpace(rawWord))
                return;

            // Collect all words from the text so the presenter can try multi-word phrases.
            string[] allWords = new string[textInfo.wordCount];
            for (int i = 0; i < textInfo.wordCount; i++)
                allWords[i] = textInfo.wordInfo[i].GetWord() ?? string.Empty;

            Vector2 screenPos = GetWordTopCenterScreenPosition(wordInfo, textInfo);
            WordClicked?.Invoke(allWords, wordIndex, screenPos);
        }

        /// <summary>
        /// Calculates the screen-space position at the top-center of the word's bounding box.
        /// </summary>
        private Vector2 GetWordTopCenterScreenPosition(TMP_WordInfo wordInfo, TMP_TextInfo textInfo)
        {
            // Collect the local-space corners of every character in the word.
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            for (int i = wordInfo.firstCharacterIndex; i <= wordInfo.lastCharacterIndex; i++)
            {
                if (i >= textInfo.characterCount)
                    break;

                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible)
                    continue;

                minX = Mathf.Min(minX, charInfo.bottomLeft.x);
                maxX = Mathf.Max(maxX, charInfo.topRight.x);
                maxY = Mathf.Max(maxY, charInfo.topRight.y);
            }

            // Fall back to the click position if no visible characters were found.
            if (minX == float.MaxValue)
                return Vector2.zero;

            Vector3 localTopCenter = new Vector3((minX + maxX) * 0.5f, maxY, 0f);
            Vector3 worldTopCenter = _text.transform.TransformPoint(localTopCenter);
            return RectTransformUtility.WorldToScreenPoint(_uiCamera, worldTopCenter);
        }

        /// <summary>
        /// Calculates the screen-space top-center position that spans all words
        /// in the range [<paramref name="startWordIndex"/>, <paramref name="startWordIndex"/> + <paramref name="wordCount"/>).
        /// Used to center the popup above a matched multi-word phrase.
        /// </summary>
        public Vector2 GetScreenPositionForWordRange(int startWordIndex, int wordCount)
        {
            _text.ForceMeshUpdate();
            TMP_TextInfo textInfo = _text.textInfo;

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            int endWordIndex = Mathf.Min(startWordIndex + wordCount, textInfo.wordCount);
            for (int wi = startWordIndex; wi < endWordIndex; wi++)
            {
                TMP_WordInfo wordInfo = textInfo.wordInfo[wi];
                for (int i = wordInfo.firstCharacterIndex; i <= wordInfo.lastCharacterIndex; i++)
                {
                    if (i >= textInfo.characterCount)
                        break;

                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                    if (!charInfo.isVisible)
                        continue;

                    minX = Mathf.Min(minX, charInfo.bottomLeft.x);
                    maxX = Mathf.Max(maxX, charInfo.topRight.x);
                    maxY = Mathf.Max(maxY, charInfo.topRight.y);
                }
            }

            if (minX == float.MaxValue)
                return Vector2.zero;

            Vector3 localTopCenter = new Vector3((minX + maxX) * 0.5f, maxY, 0f);
            Vector3 worldTopCenter = _text.transform.TransformPoint(localTopCenter);
            return RectTransformUtility.WorldToScreenPoint(_uiCamera, worldTopCenter);
        }
    }
}
