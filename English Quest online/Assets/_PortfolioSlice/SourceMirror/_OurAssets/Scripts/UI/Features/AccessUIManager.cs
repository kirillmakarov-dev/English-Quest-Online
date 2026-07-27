using EnglishQuest.UI.Loading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityServiceLocator;

/// <summary>
/// Listens to GameBootstrap's auth/whitelist events and drives the boot-screen UI.
/// Separated from GameBootstrap so it lives in Assembly-CSharp and can reference
/// LoadingScreenManager without creating a circular assembly dependency.
///
/// Scene Setup:
///   • TextMeshProUGUI → statusText   (status / error messages)
///   • Button          → exitButton   (shown only on error — quits the app)
/// </summary>
public class AccessUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button exitButton;

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Awake()
    {
#if UNITY_EDITOR
        // When EditorPreloadInjector injects PreLoad additively into a mid-game scene,
        // the auth canvas is not needed — the developer is already in their target scene.
        // Hide it so it doesn't overlay the game view.
        if (EditorPreloadInjector.WasInjectedMidSession)
        {
            gameObject.SetActive(false);
            return;
        }
        else
        {
            // If not injected mid-session, we're in the Editor's Play Mode starting scene.
            // In this case, we want to hide the loading screen canvas since AccessUIManager will handle all boot-screen UI.
            var loadingScreenManager = FindFirstObjectByType<LoadingScreenManager>();
            if (loadingScreenManager != null)
            {
                var canvasGroup = loadingScreenManager.GetComponentInChildren<CanvasGroup>();
                if (canvasGroup != null)
                    canvasGroup.alpha = 1f;
            }
        }
#else
    var loadingScreenManager = FindFirstObjectByType<LoadingScreenManager>();
    if (loadingScreenManager != null)
    {
        var canvasGroup = loadingScreenManager.GetComponentInChildren<CanvasGroup>();
        if (canvasGroup != null)
            canvasGroup.alpha = 1f; // Show the loading screen canvas in production builds, since AccessUIManager handles all boot-screen UI.
    }
#endif


        if (exitButton != null)
        {
            exitButton.gameObject.SetActive(false);
            exitButton.onClick.AddListener(Application.Quit);
        }
    }

    private void OnEnable()
    {
        GameBootstrap.OnStatusChanged += HandleStatusChanged;
        GameBootstrap.OnErrorOccurred += HandleError;
        GameBootstrap.OnAccessGranted += HandleAccessGranted;
    }

    private void OnDisable()
    {
        GameBootstrap.OnStatusChanged -= HandleStatusChanged;
        GameBootstrap.OnErrorOccurred -= HandleError;
        GameBootstrap.OnAccessGranted -= HandleAccessGranted;
    }

    // ---------------------------------------------------------------
    // Event handlers
    // ---------------------------------------------------------------

    private void HandleStatusChanged(string message)
    {
        SetStatus(message, isError: false);
    }

    private void HandleError(string message)
    {
        SetStatus(message, isError: true);
        if (exitButton != null)
            exitButton.gameObject.SetActive(true);
    }

    private void HandleAccessGranted(string sceneName)
    {
        SetStatus("Access granted! Loading...", isError: false);
        if (ServiceLocator.For(this).TryGet<ILoadingScreenService>(out var loadingScreen))
            loadingScreen.LoadScene(sceneName);
        else
            AppLog.Error("[AccessUIManager] ILoadingScreenService not found via ServiceLocator.");
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private void SetStatus(string message, bool isError)
    {
        if (statusText == null) return;
        statusText.text  = message;
        statusText.color = isError ? new Color(0.9f, 0.2f, 0.2f) : Color.white;
    }
}
