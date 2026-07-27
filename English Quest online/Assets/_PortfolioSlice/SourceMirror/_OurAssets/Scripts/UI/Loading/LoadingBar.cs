using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace EnglishQuest.UI.Loading
{
    /// <summary>
    /// Handles the visual representation of a loading progress bar.
    /// Single Responsibility: Only manages the bar's visual state.
    /// </summary>
    public class LoadingBar : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Image backgroundImage;

        [Header("Animation Settings")]
        [SerializeField] private float smoothSpeed = 3f;
        [SerializeField] private bool useSmoothing = true;

        [Header("Visual Settings")]
        [SerializeField] private Gradient progressGradient;
        [SerializeField] private bool useGradientColor = true;

        private float targetProgress;
        private float currentProgress;
        private Coroutine progressCoroutine;

        /// <summary>
        /// Current progress value (0 to 1).
        /// </summary>
        public float Progress => currentProgress;

        /// <summary>
        /// Whether the bar has reached its target progress.
        /// </summary>
        public bool IsComplete => Mathf.Approximately(currentProgress, 1f);

        private void Awake()
        {
            InitializeBar();
        }

        private void OnDestroy()
        {
            if (progressCoroutine != null) StopCoroutine(progressCoroutine);
        }

        /// <summary>
        /// Initializes the bar to zero progress.
        /// </summary>
        public void InitializeBar()
        {
            currentProgress = 0f;
            targetProgress = 0f;
            UpdateBarVisual();
        }

        /// <summary>
        /// Sets the progress value (0 to 1).
        /// </summary>
        /// <param name="progress">Progress value between 0 and 1.</param>
        public void SetProgress(float progress)
        {
            targetProgress = Mathf.Clamp01(progress);

            if (!useSmoothing)
            {
                currentProgress = targetProgress;
                UpdateBarVisual();
            }
            else
            {
                if (progressCoroutine == null)
                    progressCoroutine = StartCoroutine(SmoothProgress());
            }
        }

        /// <summary>
        /// Instantly sets the progress without smoothing.
        /// </summary>
        /// <param name="progress">Progress value between 0 and 1.</param>
        public void SetProgressImmediate(float progress)
        {
            if (progressCoroutine != null) 
            {
                StopCoroutine(progressCoroutine);
                progressCoroutine = null;
            }
            targetProgress = Mathf.Clamp01(progress);
            currentProgress = targetProgress;
            UpdateBarVisual();
        }

        /// <summary>
        /// Animates the progress bar to a target value over time.
        /// </summary>
        /// <param name="progress">Target progress value.</param>
        /// <param name="duration">Animation duration.</param>
        public void AnimateToProgress(float progress, float duration)
        {
            if (progressCoroutine != null) StopCoroutine(progressCoroutine);
            progressCoroutine = StartCoroutine(AnimateToProgressRoutine(progress, duration));
        }

        private IEnumerator AnimateToProgressRoutine(float progress, float duration)
        {
            targetProgress = Mathf.Clamp01(progress);
            float startProgress = currentProgress;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                currentProgress = Mathf.Lerp(startProgress, targetProgress, timer / duration);
                UpdateBarVisual();
                yield return null;
            }
            currentProgress = targetProgress;
            UpdateBarVisual();
            progressCoroutine = null;
        }

        /// <summary>
        /// Resets the bar to zero progress.
        /// </summary>
        public void Reset()
        {
            if (progressCoroutine != null) 
            {
                StopCoroutine(progressCoroutine);
                progressCoroutine = null;
            }
            InitializeBar();
        }
        
        private IEnumerator SmoothProgress()
        {
            while (!Mathf.Approximately(currentProgress, targetProgress))
            {
                currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, smoothSpeed * Time.unscaledDeltaTime);
                UpdateBarVisual();
                yield return null;
            }
            progressCoroutine = null; // Done smoothing
        }

        private void UpdateBarVisual()
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = currentProgress;

                if (useGradientColor && progressGradient != null)
                {
                    fillImage.color = progressGradient.Evaluate(currentProgress);
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Create default gradient if none exists
            if (progressGradient == null || progressGradient.colorKeys.Length == 0)
            {
                progressGradient = new Gradient();
                progressGradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(new Color(0.2f, 0.6f, 1f), 0f),   // Blue
                        new GradientColorKey(new Color(0.4f, 0.8f, 1f), 0.5f), // Light Blue
                        new GradientColorKey(new Color(0.3f, 1f, 0.5f), 1f)    // Green
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 1f)
                    }
                );
            }
        }
#endif
    }
}

