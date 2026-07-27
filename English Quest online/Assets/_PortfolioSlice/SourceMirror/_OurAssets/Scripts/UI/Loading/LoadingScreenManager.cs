using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityServiceLocator;

namespace EnglishQuest.UI.Loading
{
    /// <summary>
    /// Manages the loading screen display and scene loading process.
    /// Single Responsibility: Orchestrates the loading screen UI and async scene loading.
    /// </summary>
    public class LoadingScreenManager : Singleton<LoadingScreenManager>, ILoadingScreenService
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup loadingCanvasGroup;
        [SerializeField] private LoadingBar loadingBar;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private TextMeshProUGUI percentageText;
        [SerializeField] private TextMeshProUGUI tipText;

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float minimumLoadingTime = 1.5f;
        [SerializeField] private float loadingTimeout = 30f; // Maximum time before forcing scene activation
        
        [Header("Loading Text Animation")]
        [SerializeField] private string loadingBaseText = "Loading";
        [SerializeField] private float dotAnimationSpeed = 0.4f;

        [Header("Tips")]
        [SerializeField] private string[] loadingTips = new string[]
        {
            "Tip: Practice your pronunciation carefully!",
            "Tip: Explore the kingdom to find new animals.",
            "Tip: Talk to the villagers to get quests.",
            "Tip: Collect stars to unlock special items.",
            "Tip: Review your vocabulary in the menu."
        };

        private Coroutine dotAnimationCoroutine;
        private bool isLoading;
        public bool IsLoading => isLoading;

        /// <summary>
        /// Event triggered when loading starts.
        /// </summary>
        public event Action OnLoadingStarted;

        /// <summary>
        /// Event triggered when loading completes.
        /// </summary>
        public event Action OnLoadingCompleted;

        /// <summary>
        /// Event triggered when progress updates (0 to 1).
        /// </summary>
        public event Action<float> OnProgressUpdated;

        protected override void Awake()
        {
            if (transform.parent != null && GetComponentInParent<Canvas>() != null && GetComponent<Canvas>() == null)
            {
                AppLog.Warning($"[LoadingScreenManager] WARNING: '{name}' is a child of a Canvas but has no Canvas component. " +
                                 "DontDestroyOnLoad will detach it from the parent Canvas, likely making it invisible!");
            }

            base.Awake();
            if (Instance == this)
                ServiceLocator.For(this).Register<ILoadingScreenService>(this);
            InitializeLoadingScreen();
            isLoading = false; // Force reset on Awake
        }

        private void OnDestroy()
        {
            AppLog.Info($"[LoadingScreenManager] OnDestroy executing for '{gameObject.name}' (InstanceID: {GetInstanceID()}).");

            if (Instance == this)
                ServiceLocator.DeregisterFor<ILoadingScreenService>(this);

            // Clean up any running coroutines
            if (dotAnimationCoroutine != null)
            {
                StopCoroutine(dotAnimationCoroutine);
                dotAnimationCoroutine = null;
            }

            isLoading = false;
        }

        private void InitializeLoadingScreen()
        {
            if (loadingCanvasGroup != null)
            {
                loadingCanvasGroup.blocksRaycasts = false;
                loadingCanvasGroup.interactable = false;
            }
        }

        /// <summary>
        /// Loads a scene with the loading screen.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load.</param>
        public void LoadScene(string sceneName)
        {
            if (isLoading)
            {
                AppLog.Warning("Already loading a scene. Please wait.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                AppLog.Error($"Scene '{sceneName}' cannot be loaded. Please ensure it is added to Build Settings.");
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        /// <summary>
        /// Loads a scene by build index with the loading screen.
        /// </summary>
        /// <param name="sceneIndex">Build index of the scene to load.</param>
        public void LoadScene(int sceneIndex)
        {
            if (isLoading)
            {
                AppLog.Warning("Already loading a scene. Please wait.");
                return;
            }

            if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
            {
                AppLog.Error($"Scene index '{sceneIndex}' is out of range.");
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneIndex));
        }

        /// <summary>
        /// Shows the loading screen, executes the provided network task (e.g. connecting/loading), and then hides the screen.
        /// Use this for networked loading where you want the visual feedback but the logic is handled by network code.
        /// </summary>
        public async UniTask LoadSceneNetworkAsync(Func<UniTask> networkLoadAction)
        {
            if (isLoading)
            {
                AppLog.Warning("Already loading. Please wait.");
                return;
            }

            isLoading = true;
            OnLoadingStarted?.Invoke();

            // Show loading screen
            StartCoroutine(ShowLoadingScreen());
            
            // Wait for fade in
            await UniTask.Delay((int)(fadeInDuration * 1000));

            // Start dot animation
            dotAnimationCoroutine = StartCoroutine(AnimateLoadingDots());

            // Show random tip
            ShowRandomTip();

            // Reset progress bar (fake progress for connection)
            loadingBar?.SetProgressImmediate(0.1f);
            UpdatePercentageText(0.1f);
            
            // Simulate some progress or just wait for the task
            var fakeProgress = StartCoroutine(FakeProgressRoutine());

            try
            {
                AppLog.Info("[LoadingScreenManager] Starting network load action...");
                await networkLoadAction();
                AppLog.Info("[LoadingScreenManager] Network load action completed.");
            }
            catch (Exception ex)
            {
                AppLog.Error($"[LoadingScreenManager] Network load failed: {ex}");
                isLoading = false;
                await HideLoadingScreenAsync();
                throw;
            }
            finally
            {
                if (fakeProgress != null) StopCoroutine(fakeProgress);

                AppLog.Info("[LoadingScreenManager] Finalizing loading screen...");
                // Animate to 100%
                loadingBar?.AnimateToProgress(1f, 0.3f);
                UpdatePercentageText(1f);
                OnProgressUpdated?.Invoke(1f);

                await UniTask.Delay(500); // Show 100% for a moment

                // Stop dot animation
                if (dotAnimationCoroutine != null)
                {
                    StopCoroutine(dotAnimationCoroutine);
                    dotAnimationCoroutine = null;
                }

                // Hide loading screen
                await HideLoadingScreenAsync(); // Wait for the fade out to finish

                isLoading = false;
                OnLoadingCompleted?.Invoke();
                AppLog.Info("[LoadingScreenManager] Loading screen hidden and finished.");
            }
        }

        private async UniTask HideLoadingScreenAsync()
        {
            if (loadingCanvasGroup == null) return;

            float timer = 0f;
            float startAlpha = loadingCanvasGroup.alpha;
            while (timer < fadeOutDuration)
            {
                 timer += Time.unscaledDeltaTime;
                 loadingCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / fadeOutDuration);
                 await UniTask.Yield();
            }
            loadingCanvasGroup.alpha = 0f;

            loadingCanvasGroup.blocksRaycasts = false;
            loadingCanvasGroup.interactable = false;

            // Reset the bar for next load
            loadingBar?.Reset();
        }

        private IEnumerator FakeProgressRoutine()
        {
            float p = 0.1f;
            while (p < 0.9f)
            {
                p += Time.unscaledDeltaTime * 0.1f; // Very slow crawl
                loadingBar?.SetProgress(p);
                UpdatePercentageText(p);
                yield return null;
            }
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            yield return StartCoroutine(LoadSceneAsyncRoutine(() => SceneManager.LoadSceneAsync(sceneName)));
        }

        private IEnumerator LoadSceneRoutine(int sceneIndex)
        {
            yield return StartCoroutine(LoadSceneAsyncRoutine(() => SceneManager.LoadSceneAsync(sceneIndex)));
        }

        private IEnumerator LoadSceneAsyncRoutine(Func<AsyncOperation> loadOperation)
        {
            // Show loading screen
            yield return StartCoroutine(ShowLoadingScreen());

            // Start dot animation
            dotAnimationCoroutine = StartCoroutine(AnimateLoadingDots());

            // Show random tip
            ShowRandomTip();

            // Reset progress bar
            loadingBar?.SetProgressImmediate(0f);
            UpdatePercentageText(0f);
            yield return null;

            // Start async loading
            AsyncOperation asyncOperation = loadOperation();
            // Check if asyncOperation is null (invalid scene)
            if (asyncOperation == null)
            {
                AppLog.Error("Failed to start scene loading async operation.");
                StartCoroutine(HideLoadingScreen());
                isLoading = false; // Reset here in case it was set
                yield break;
            }

            asyncOperation.allowSceneActivation = false;

            float elapsedTime = 0f;
            float displayProgress = 0f;

            // Loading loop with timeout protection
            while (!asyncOperation.isDone)
            {
                elapsedTime += Time.unscaledDeltaTime;

                // Unity's progress goes from 0 to 0.9
                float targetProgress = Mathf.Clamp01(asyncOperation.progress / 0.9f);

                // Smooth progress display
                displayProgress = Mathf.MoveTowards(displayProgress, targetProgress, Time.unscaledDeltaTime * 2f);

                // Update UI
                loadingBar?.SetProgress(displayProgress);
                UpdatePercentageText(displayProgress);
                OnProgressUpdated?.Invoke(displayProgress);

                // Check if loading is complete and minimum time has passed
                if (asyncOperation.progress >= 0.9f && elapsedTime >= minimumLoadingTime)
                {
                    // Animate to 100%
                    loadingBar?.AnimateToProgress(1f, 0.3f);
                    UpdatePercentageText(1f);
                    OnProgressUpdated?.Invoke(1f);

                    yield return new WaitForSecondsRealtime(0.4f);

                    // Activate the scene
                    asyncOperation.allowSceneActivation = true;
                }
                // Timeout protection
                else if (elapsedTime >= loadingTimeout)
                {
                    AppLog.Warning($"[LoadingScreenManager] Loading timeout reached ({loadingTimeout}s). Forcing scene activation.");
                    loadingBar?.SetProgressImmediate(1f);
                    UpdatePercentageText(1f);
                    asyncOperation.allowSceneActivation = true;
                }

                yield return null;
            }

            // Stop dot animation
            if (dotAnimationCoroutine != null)
            {
                StopCoroutine(dotAnimationCoroutine);
                dotAnimationCoroutine = null;
            }

            // Hide loading screen
            yield return StartCoroutine(HideLoadingScreen());

            isLoading = false;
            OnLoadingCompleted?.Invoke();
        }

        private IEnumerator ShowLoadingScreen()
        {
            if (loadingCanvasGroup == null) yield break;

            loadingCanvasGroup.blocksRaycasts = true;
            loadingCanvasGroup.interactable = true;

            float timer = 0f;
            float startAlpha = loadingCanvasGroup.alpha;
            while (timer < fadeInDuration)
            {
                timer += Time.unscaledDeltaTime;
                loadingCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, timer / fadeInDuration);
                yield return null;
            }
            loadingCanvasGroup.alpha = 1f;
        }

        private IEnumerator HideLoadingScreen()
        {
            if (loadingCanvasGroup == null) yield break;

            float timer = 0f;
            float startAlpha = loadingCanvasGroup.alpha;
            while (timer < fadeOutDuration)
            {
                 timer += Time.unscaledDeltaTime;
                 loadingCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / fadeOutDuration);
                 yield return null;
            }
            loadingCanvasGroup.alpha = 0f;

            loadingCanvasGroup.blocksRaycasts = false;
            loadingCanvasGroup.interactable = false;

            // Reset the bar for next load
            loadingBar?.Reset();
        }

        private IEnumerator AnimateLoadingDots()
        {
            int dotCount = 0;

            while (true)
            {
                if (loadingText != null)
                {
                    string dots = new string('.', dotCount);
                    loadingText.text = loadingBaseText + dots;
                }

                dotCount = (dotCount + 1) % 4; // 0, 1, 2, 3, then back to 0
                yield return new WaitForSecondsRealtime(dotAnimationSpeed);
            }
        }

        private void ShowRandomTip()
        {
            if (tipText != null && loadingTips != null && loadingTips.Length > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, loadingTips.Length);
                tipText.text = loadingTips[randomIndex];
            }
        }

        private void UpdatePercentageText(float progress)
        {
            if (percentageText != null)
            {
                int percentage = Mathf.RoundToInt(progress * 100f);
                percentageText.text = $"{percentage}%";
            }
        }
    }
}

