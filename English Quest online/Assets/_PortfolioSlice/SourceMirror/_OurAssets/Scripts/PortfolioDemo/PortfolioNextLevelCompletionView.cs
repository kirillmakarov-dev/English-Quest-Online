using System.Collections;
using TMPro;
using UnityEngine;

namespace EnglishQuest.PortfolioDemo
{
    [DisallowMultipleComponent]
    public sealed class PortfolioNextLevelCompletionView : MonoBehaviour
    {
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI eyebrowText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI supportText;

        private Coroutine fadeRoutine;

        private void Awake()
        {
            if (rootCanvas != null)
            {
                rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                rootCanvas.overrideSorting = true;
                rootCanvas.sortingOrder = 10000;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        public void Show(string eyebrow, string title, string support, float fadeDuration)
        {
            if (eyebrowText != null)
                eyebrowText.text = eyebrow;
            if (titleText != null)
                titleText.text = title;
            if (supportText != null)
                supportText.text = support;

            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(FadeIn(Mathf.Max(0.05f, fadeDuration)));
        }

        private IEnumerator FadeIn(float duration)
        {
            if (canvasGroup == null)
                yield break;

            canvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = t * t * (3f - 2f * t);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            fadeRoutine = null;
        }
    }
}
