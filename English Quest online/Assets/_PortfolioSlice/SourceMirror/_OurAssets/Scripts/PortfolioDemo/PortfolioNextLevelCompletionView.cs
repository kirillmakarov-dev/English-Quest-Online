using System.Collections;
using Cysharp.Threading.Tasks;
using EnglishQuest.QuestSystem;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityServiceLocator;

namespace EnglishQuest.PortfolioDemo
{
    [DisallowMultipleComponent]
    [AddComponentMenu("English Quest/Portfolio Demo/Pause And Completion Menu")]
    public sealed class PortfolioNextLevelCompletionView : GameplayUIBase
    {
        [Header("Authored UI")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI eyebrowText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI supportText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button exitButton;

        [Header("Completion")]
        [SerializeField, Min(0f)] private float completionMenuDelay = 5f;
        [SerializeField] private string restartScenePath = "Assets/_PortfolioSlice/Demo/Scenes/PortfolioDemo.unity";

        private Coroutine presentationRoutine;
        private bool isOpen;
        private bool levelCompleted;
        private bool transitionRequested;

        private void Awake()
        {
            if (rootCanvas != null)
            {
                rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                rootCanvas.overrideSorting = true;
                rootCanvas.sortingOrder = 10000;
            }

            continueButton?.onClick.AddListener(ContinueGame);
            restartButton?.onClick.AddListener(RestartLevel);
            nextLevelButton?.onClick.AddListener(LoadNextLevel);
            exitButton?.onClick.AddListener(ExitGame);

            HideImmediately();
        }

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true &&
                !levelCompleted &&
                !transitionRequested)
            {
                if (isOpen)
                    ContinueGame();
                else
                    ShowPauseMenu();
            }
        }

        private void OnDisable()
        {
            RestoreGameplayState();
        }

        private void OnDestroy()
        {
            continueButton?.onClick.RemoveListener(ContinueGame);
            restartButton?.onClick.RemoveListener(RestartLevel);
            nextLevelButton?.onClick.RemoveListener(LoadNextLevel);
            exitButton?.onClick.RemoveListener(ExitGame);
        }

        public void Show(string eyebrow, string title, string support, float requestedFadeDuration)
        {
            BeginLevelCompletion(eyebrow, title, support, Mathf.Max(0.05f, requestedFadeDuration));
        }

        public void ShowPauseMenu()
        {
            StopPresentationRoutine();
            levelCompleted = false;
            SetCopy("PAUSED", "GAME MENU", "Continue, restart the level, or exit the game.");
            SetButtonVisibility(showContinue: true, showNextLevel: false);
            Open(alpha: 1f, buttonsInteractable: true);
        }

        public void ContinueGame()
        {
            if (!isOpen || levelCompleted)
                return;

            HideImmediately();
            RestoreGameplayState();
        }

        public void RestartLevel()
        {
            RestartCurrentLevel();
        }

        public void LoadNextLevel()
        {
            RestartCurrentLevel();
        }

        public void ExitGame()
        {
            transitionRequested = true;
            RestoreGameplayState();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void BeginLevelCompletion(string eyebrow, string title, string support, float duration)
        {
            if (levelCompleted)
                return;

            levelCompleted = true;
            SetCopy(eyebrow, title, support);
            SetAllButtonsVisible(false);
            Open(alpha: 0f, buttonsInteractable: false);

            StopPresentationRoutine();
            presentationRoutine = StartCoroutine(ShowCompletionRoutine(duration));
        }

        private IEnumerator ShowCompletionRoutine(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = t * t * (3f - 2f * t);
                yield return null;
            }

            canvasGroup.alpha = 1f;

            if (completionMenuDelay > 0f)
                yield return new WaitForSecondsRealtime(completionMenuDelay);

            SetButtonVisibility(showContinue: false, showNextLevel: true);
            SetButtonsInteractable(true);
            presentationRoutine = null;
        }

        private void Open(float alpha, bool buttonsInteractable)
        {
            if (canvasGroup == null)
                return;

            isOpen = true;
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            SetButtonsInteractable(buttonsInteractable);
            AudioListener.pause = true;
            BeginInteraction();
        }

        private void HideImmediately()
        {
            StopPresentationRoutine();
            isOpen = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            SetButtonsInteractable(false);
        }

        private void RestoreGameplayState()
        {
            AudioListener.pause = false;
            EndInteraction();
        }

        private void SetCopy(string eyebrow, string title, string support)
        {
            if (eyebrowText != null)
                eyebrowText.text = eyebrow;
            if (titleText != null)
                titleText.text = title;
            if (supportText != null)
                supportText.text = support;
        }

        private void SetButtonVisibility(bool showContinue, bool showNextLevel)
        {
            if (continueButton != null)
                continueButton.gameObject.SetActive(showContinue);
            if (restartButton != null)
                restartButton.gameObject.SetActive(true);
            if (nextLevelButton != null)
                nextLevelButton.gameObject.SetActive(showNextLevel);
            if (exitButton != null)
                exitButton.gameObject.SetActive(true);
        }

        private void SetAllButtonsVisible(bool value)
        {
            if (continueButton != null)
                continueButton.gameObject.SetActive(value);
            if (restartButton != null)
                restartButton.gameObject.SetActive(value);
            if (nextLevelButton != null)
                nextLevelButton.gameObject.SetActive(value);
            if (exitButton != null)
                exitButton.gameObject.SetActive(value);
        }

        private void SetButtonsInteractable(bool value)
        {
            if (continueButton != null)
                continueButton.interactable = value;
            if (restartButton != null)
                restartButton.interactable = value;
            if (nextLevelButton != null)
                nextLevelButton.interactable = value;
            if (exitButton != null)
                exitButton.interactable = value;
        }

        private async UniTask BeginSceneTransitionAsync(string scenePath)
        {
            RestoreGameplayState();

            TryResolveRunner(out NetworkRunner runner);

            if (TryResolveLevelManager(out ILevelManager levelManager))
            {
                await levelManager.TransitionToSceneAsync(scenePath);
                if (runner != null && runner.IsRunning)
                    PlayerSpawnCoordinator.EnsureLocalPlayerAfterSceneLoad(runner);
                return;
            }

            int buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);
            if (runner != null && runner.IsRunning && buildIndex >= 0)
            {
                bool loaded = await FusionSceneTransitionService.LoadSceneAsync(
                    runner,
                    buildIndex,
                    expectedScenePath: scenePath);
                if (loaded)
                    PlayerSpawnCoordinator.EnsureLocalPlayerAfterSceneLoad(runner);
                return;
            }

            SceneManager.LoadScene(scenePath);
        }

        private bool TryResolveLevelManager(out ILevelManager levelManager)
        {
            levelManager = null;
            ServiceLocator sceneLocator = ServiceLocator.For(this);
            if (sceneLocator != null && sceneLocator.TryGet(out levelManager))
                return true;

            ServiceLocator globalLocator = ServiceLocator.Global;
            return globalLocator != null && globalLocator.TryGet(out levelManager);
        }

        private bool TryResolveRunner(out NetworkRunner runner)
        {
            runner = null;

            ServiceLocator sceneLocator = ServiceLocator.For(this);
            if (sceneLocator != null &&
                sceneLocator.TryGet(out INetworkSessionService sceneSession) &&
                sceneSession.Runner != null &&
                sceneSession.Runner.IsRunning)
            {
                runner = sceneSession.Runner;
                return true;
            }

            ServiceLocator globalLocator = ServiceLocator.Global;
            if (globalLocator != null &&
                globalLocator.TryGet(out INetworkSessionService globalSession) &&
                globalSession.Runner != null &&
                globalSession.Runner.IsRunning)
            {
                runner = globalSession.Runner;
                return true;
            }

            foreach (NetworkRunner candidate in NetworkRunner.Instances)
            {
                if (candidate == null || !candidate.IsRunning)
                    continue;

                runner = candidate;
                return true;
            }

            return false;
        }

        private void RestartCurrentLevel()
        {
            if (transitionRequested)
                return;

            transitionRequested = true;
            QuestManager manager = QuestManager.HasInstance ? QuestManager.Instance : null;
            manager?.ResetAllProgress();

            string scenePath = restartScenePath;
            if (string.IsNullOrWhiteSpace(scenePath) || SceneUtility.GetBuildIndexByScenePath(scenePath) < 0)
                scenePath = gameObject.scene.path;
            if (string.IsNullOrWhiteSpace(scenePath))
                scenePath = SceneManager.GetActiveScene().path;

            BeginSceneTransitionAsync(scenePath).Forget();
        }

        private void StopPresentationRoutine()
        {
            if (presentationRoutine == null)
                return;

            StopCoroutine(presentationRoutine);
            presentationRoutine = null;
        }
    }
}
