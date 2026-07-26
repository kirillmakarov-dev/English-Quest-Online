#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Per-developer editor auth bypass config. Each developer creates and keeps
/// their own copy — never commit this asset to version control.
///
/// Setup (one-time per developer):
///   1. Right-click in Project → Create → English Kingdom → Dev → Dev Bootstrap Config
///   2. Move the asset to:  Assets/_OurAssets/Resources/Editor/DevBootstrapConfig.asset
///   3. Add that path to your personal .gitignore or the repo's .gitignore
///
/// Why Resources/Editor/?
///   Unity strips the entire Resources/Editor/ folder from production builds,
///   so this asset can never ship to players.
/// </summary>

/// <summary>Controls how the editor bypasses the launcher → Firebase → whitelist flow.</summary>
public enum DevAuthMode
{
    /// <summary>
    /// No network calls at all. PlayerPrefs seeded with fake values instantly.
    /// Use for pure UI / frontend work with zero cloud dependency.
    /// CloudSave, CloudCode, and any Unity Gaming Service will NOT work.
    /// </summary>
    SkipAll,

    /// <summary>
    /// Signs into Unity Authentication Service anonymously (real network call).
    /// Gives a valid PlayerId and session token — CloudSave, CloudCode, and all
    /// other UGS services work exactly as in production.
    /// The Firebase token and whitelist check are skipped.
    /// Use whenever you need to test anything that touches cloud features.
    /// </summary>
    AnonymousUnityAuth,
}

[CreateAssetMenu(menuName = "English Kingdom/Dev/Dev Bootstrap Config", fileName = "DevBootstrapConfig")]
public class DevBootstrapConfig : ScriptableObject
{
    [Tooltip("Additively inject the PreLoad scene when entering Play Mode from any mid-game scene.\n" +
             "Ensures all persistent services (Save, Audio, Network…) are available regardless of starting scene.")]
    public bool injectPreloadScene = true;

    [Tooltip("When enabled, scenes under Assets/_OurAssets/Scenes/TestScenes/ skip PreLoad injection.\n" +
             "Use this for Fusion Multi-Peer editor testing with FusionBootstrap so DDOL GameNetworkManager\n" +
             "does not conflict with multiple local NetworkRunner instances.")]
    public bool skipPreloadForTestScenes = true;

    [Tooltip(
        "SkipAll           — no network calls, instant start. Good for UI/frontend work.\n" +
        "AnonymousUnityAuth — real Unity Auth session via anonymous sign-in.\n" +
        "                     CloudSave / CloudCode / all UGS services work normally.")]
    public DevAuthMode authMode = DevAuthMode.AnonymousUnityAuth;

    [Tooltip("Written to PlayerPrefs(\"playerEmail\") during the dev bypass.")]
    public string devEmail = "dev@test.com";

    [Tooltip("Written to PlayerPrefs(\"username\") during the dev bypass.")]
    public string devUsername = "DevPlayer";

    [FormerlySerializedAs("devIsTeacher")]
    [Tooltip("When enabled, PlayerRoleProfile.IsGuider is true during dev bypass (guider tools in editor).")]
    public bool devIsGuider;

    // Path is relative to any Resources/ folder — must match the asset placement above.
    private const string ResourcePath = "Editor/DevBootstrapConfig";

    /// <summary>
    /// Loads the config from Resources/Editor/.
    /// Returns null if the developer hasn't created the asset yet.
    /// </summary>
    public static DevBootstrapConfig Load() =>
        Resources.Load<DevBootstrapConfig>(ResourcePath);
}
#endif
