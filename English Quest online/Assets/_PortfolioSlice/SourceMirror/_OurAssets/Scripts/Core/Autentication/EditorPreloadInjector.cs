#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor-only bootstrapper that runs before any scene loads in Play Mode.
///
/// Solves two problems developers face when entering Play Mode from a mid-game scene:
///
///   Problem 1 — Missing services
///     Persistent managers (SaveSystem, AudioManager, NetworkManager…) only
///     exist when the game starts from the PreLoad scene.  Starting from Level2
///     or OpenWorld means they're never instantiated and ServiceLocator calls fail.
///     Fix: Additively load PreLoad.unity so all its DontDestroyOnLoad managers
///     are injected before any scene Awake runs.
///
///   Problem 2 — Auth wall
///     GameBootstrap requires a DPAPI-encrypted handoff file written by the
///     launcher.  Developers running from the editor never have this file.
///     Fix: Expose DevAuthBypass so GameBootstrap can seed PlayerPrefs and skip
///     the entire launcher → OIDC → whitelist flow.
///
/// IMPORTANT: This class uses [RuntimeInitializeOnLoadMethod] which only works
/// in a non-Editor assembly.  It lives in the _Project assembly (not Editor/)
/// and is guarded by  #if UNITY_EDITOR  to remain invisible in production builds.
///
/// Domain Reload note:
///   When "Reload Domain" is disabled in Editor → Project Settings → Editor,
///   static fields survive between Play sessions.  Bootstrap() explicitly resets
///   ALL statics at the top of every run to prevent stale state.
/// </summary>
/// 
[DefaultExecutionOrder(-100)]

public static class EditorPreloadInjector
{
    // ── Dev-bypass state — written once here, read by GameBootstrap.Start ────────

    /// <summary>
    /// The auth mode chosen by the developer in their DevBootstrapConfig asset.
    /// <see cref="DevAuthMode.SkipAll"/> means no network calls are made.
    /// <see cref="DevAuthMode.AnonymousUnityAuth"/> means UnityAuthManager will
    /// sign in anonymously so all cloud services (CloudSave, CloudCode…) work.
    /// </summary>
    public static DevAuthMode AuthMode        { get; private set; }

    /// <summary>True when ANY dev bypass mode is active (AuthMode is set by config).</summary>
    public static bool   DevBypassActive      { get; private set; }

    /// <summary>Fake email written to PlayerPrefs("playerEmail") during dev bypass.</summary>
    public static string DevEmail             { get; private set; }

    /// <summary>Fake username written to PlayerPrefs("username") during dev bypass.</summary>
    public static string DevUsername          { get; private set; }

    /// <summary>When true, dev bypass seeds PlayerRoleProfile as guider.</summary>
    public static bool   DevIsGuider         { get; private set; }

    /// <summary>
    /// True when PreLoad was injected because the developer started from a mid-game
    /// scene.  GameBootstrap reads this to skip OnAccessGranted navigation — the
    /// developer is already in their target scene and should stay there.
    /// </summary>
    public static bool   WasInjectedMidSession { get; private set; }

    private const string PreloadSceneName = "PreLoad";

    // ─────────────────────────────────────────────────────────────────────────────
    // Entry point — fires before any scene's Awake, guaranteeing all seeded
    // state is available when GameBootstrap.Start() is called.
    // ─────────────────────────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        // ── Reset all statics first ───────────────────────────────────────────────
        // With "Reload Domain" OFF, statics survive between Play sessions.
        // Without this reset, WasInjectedMidSession / DevBypassActive etc. would
        // carry over stale values from the previous run and produce wrong behaviour.
        DevBypassActive       = false;
        AuthMode              = default;
        DevEmail              = string.Empty;
        DevUsername           = string.Empty;
        DevIsGuider          = false;
        WasInjectedMidSession = false;
        // ─────────────────────────────────────────────────────────────────────────

        var config = DevBootstrapConfig.Load();
        if (config == null)
        {
            AppLog.Info(
                "[EditorPreloadInjector] No DevBootstrapConfig found.\n" +
                "Create one via: right-click in Project → Create → English Kingdom → Dev → Dev Bootstrap Config\n" +
                "Then place it at: Assets/_OurAssets/Resources/Editor/DevBootstrapConfig.asset");
            return;
        }

        // Cache credentials so GameBootstrap can read them synchronously in Start().
        DevBypassActive = true;
        AuthMode        = config.authMode;
        DevEmail        = config.devEmail;
        DevUsername     = config.devUsername;
        DevIsGuider    = config.devIsGuider;

        // ── Domain Reload OFF: ALWAYS destroy stale DDOL managers ────────────────
        // Must run unconditionally — even when starting from PreLoad itself — so the
        // fresh scene objects (ServiceLocatorGlobal, Singletons…) can re-register
        // without hitting "Another ServiceLocator is already configured as global".
        // If this is skipped when active scene == PreLoad, the stale DDOL object from
        // the previous session collides with the newly-awakened PreLoad instance.
        DestroyStaleDoNotDestroyOnLoadManagers();
        // ─────────────────────────────────────────────────────────────────────────

        if (!config.injectPreloadScene) return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (config.skipPreloadForTestScenes && IsTestScene(activeScene))
        {
            WasInjectedMidSession = true;
            AppLog.Info(
                "[EditorPreloadInjector] Test scene detected — skipping PreLoad injection for Fusion Multi-Peer testing.");
            return;
        }

        // BeforeSceneLoad fires before any scene object's Awake — the active scene
        // is already set to whatever the developer pressed Play from, but its objects
        // aren't initialised yet.  GetActiveScene().name is reliable at this point;
        // SceneManager.sceneCount is NOT (can return 0 depending on Unity version).
        if (activeScene.name == PreloadSceneName) return;

        // Also check for any already-additive scenes (redundant guard for safety).
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).name == PreloadSceneName) return;

        // PreLoad is missing — flag it and inject additively.
        // Use the scene NAME (not path) so SceneManager resolves it from Build Settings
        // reliably at runtime. The path form is editor-only and not valid here.
        WasInjectedMidSession = true;
        AppLog.Warning(
            "[EditorPreloadInjector] PreLoad scene not detected — injecting it additively.\n" +
            "All persistent services (SaveSystem, Audio, Network…) will be available.\n" +
            "This NEVER runs in a production build.");

        SceneManager.LoadScene(PreloadSceneName, LoadSceneMode.Additive); 
    }

    private static bool IsTestScene(Scene scene)
    {
        string scenePath = scene.path.Replace('\\', '/');
        if (!string.IsNullOrEmpty(scenePath) && scenePath.Contains("/Scenes/TestScenes/"))
            return true;

        return scene.name is "CombatTest" or "GameplayTestScene" or "1Test" or "numbers1.2";
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // With Domain Reload disabled, DDOL objects from the previous Play session
    // survive into the next one. Destroy them before PreLoad injects fresh copies
    // so we never get duplicate ServiceLocators, Singletons, managers, etc.
    // ─────────────────────────────────────────────────────────────────────────────
    private static void DestroyStaleDoNotDestroyOnLoadManagers()
    {
        // Destroy all root GameObjects that live in the DontDestroyOnLoad pseudo-scene.
        // This covers ServiceLocatorGlobal, GameBootstrap, UnityAuthManager, every
        // Singleton<T> etc. — anything that called DontDestroyOnLoad in the previous session.
        //
        // We match by scene.name == "DontDestroyOnLoad" — Unity's guaranteed name for
        // the DDOL pseudo-scene. Using buildIndex == -1 is NOT safe here because scenes
        // that are not registered in Build Settings also return buildIndex == -1, which
        // would wrongly destroy objects in the developer's active non-registered scene.
        foreach (var go in Object.FindObjectsByType<GameObject>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go.scene.name == "DontDestroyOnLoad" && go.transform.parent == null)
            {
                AppLog.Info($"[EditorPreloadInjector] Destroying stale DDOL root object: {go.name}");
                Object.DestroyImmediate(go);
            }
        }
    }
}
#endif
