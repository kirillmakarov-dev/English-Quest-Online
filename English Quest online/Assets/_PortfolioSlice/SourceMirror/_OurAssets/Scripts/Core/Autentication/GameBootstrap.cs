using System;
using System.Text;
using UnityEngine;

/// <summary>
/// Entry point: reads the Firebase ID token written by the launcher via
/// GameHandoffService (DPAPI-encrypted file on Windows), then signs into
/// Unity Authentication using Firebase as an OpenID Connect provider.
///
/// Flow:
///   1. GameHandoffService.ReadAndDestroy() → Firebase ID token
///   2. UnityAuthManager.LoginWithOpenIdConnect(idToken) → Unity sign-in (OIDC)
///   3. CloudCodeWhitelistChecker receives OnLoginSuccess → checks whitelist
///   4. OnAccessGranted → fires scene name for AccessUIManager to load
///      OnErrorOccurred → fires reason for AccessUIManager to display
///
/// UI is intentionally absent — subscribe to the static events below from
/// Assembly-CSharp (AccessUIManager) to keep this assembly dependency-free.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Scene to load after successful authentication and whitelist check")]
    public string gameSceneName = "Menu";

    // ─────────────────────────────────────────────────────────────
    // Static events — consumed by AccessUIManager in Assembly-CSharp
    // ─────────────────────────────────────────────────────────────

    /// <summary>Fired when the auth/whitelist flow emits an informational status update.</summary>
    public static event Action<string> OnStatusChanged;

    /// <summary>Fired when authentication or whitelist check fails. Passes the reason.</summary>
    public static event Action<string> OnErrorOccurred;

    /// <summary>Fired when whitelist is granted. Passes the scene name to load.</summary>
    public static event Action<string> OnAccessGranted;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (EditorPreloadInjector.DevBypassActive)
        {
            PlayerPrefs.SetString("playerEmail", EditorPreloadInjector.DevEmail);
            PlayerPrefs.SetString("username",    EditorPreloadInjector.DevUsername);
            PlayerPrefs.Save();
            SeedDevPlayerRole();

            if (EditorPreloadInjector.AuthMode == DevAuthMode.AnonymousUnityAuth)
            {
                // Real Unity Auth session — CloudSave / CloudCode will work.
                // Subscribe to OnLoginSuccess but NOT to the whitelist events —
                // we bypass whitelist entirely and fire OnAccessGranted directly.
                AppLog.Warning("[Bootstrap] DEV MODE (AnonymousUnityAuth): signing in anonymously. CloudSave/Code enabled. Whitelist skipped.");
                UnityAuthManager.OnLoginSuccess += HandleDevAnonLoginSuccess;
                UnityAuthManager.OnLoginFailed  += HandleLoginFailed;
                UnityAuthManager.Instance.LoginAnonymously();
                return;
            }

            // DevAuthMode.SkipAll — no network, just fire access granted immediately.
            AppLog.Warning("[Bootstrap] DEV MODE (SkipAll): no auth calls made. This never runs in a production build.");
            if (!EditorPreloadInjector.WasInjectedMidSession)
                OnAccessGranted?.Invoke(gameSceneName);
            else
                AppLog.Info("[Bootstrap] DEV MODE: Mid-session inject — auth seeded in place, staying in current scene.");
            return;
        }
#endif

        UnityAuthManager.OnLoginSuccess              += HandleLoginSuccess;
        UnityAuthManager.OnLoginFailed               += HandleLoginFailed;
        CloudCodeWhitelistChecker.OnWhitelistGranted += HandleWhitelistGranted;
        CloudCodeWhitelistChecker.OnWhitelistDenied  += HandleWhitelistDenied;

        string version = LauncherVersionResolver.FromCommandLine();
        var handoff = GameHandoffService.ReadAndDestroyAll();
        if (!string.IsNullOrEmpty(handoff.GameVersion))
            version = handoff.GameVersion;
        if (!string.IsNullOrEmpty(version))
            PlayerPrefs.SetString("gameVersion", version);

        if (string.IsNullOrEmpty(handoff.IdToken))
        {
            FireError("Please launch the game through the course launcher.");
            return;
        }

        // Persist username so any in-game UI can read PlayerPrefs.GetString("username")
        AppLog.Info($"[Bootstrap] Received handoff. Username: {handoff.Username}, ID Token length: {handoff.IdToken.Length}");
        if (!string.IsNullOrEmpty(handoff.Username))
            PlayerPrefs.SetString("username", handoff.Username);

        FireStatus("Authenticating...");
        UnityAuthManager.Instance.LoginWithOpenIdConnect(handoff.IdToken);
    }

    private void OnDestroy()
    {
        UnityAuthManager.OnLoginSuccess              -= HandleLoginSuccess;
        UnityAuthManager.OnLoginFailed               -= HandleLoginFailed;
        CloudCodeWhitelistChecker.OnWhitelistGranted -= HandleWhitelistGranted;
        CloudCodeWhitelistChecker.OnWhitelistDenied  -= HandleWhitelistDenied;
#if UNITY_EDITOR
        UnityAuthManager.OnLoginSuccess              -= HandleDevAnonLoginSuccess;
#endif
    }

    // ─────────────────────────────────────────────────────────────
    // Auth / whitelist event handlers
    // ─────────────────────────────────────────────────────────────

    private void HandleLoginSuccess(string token)
    {
        FireStatus("Checking access...");
        AppLog.Info("[Bootstrap] OIDC sign-in succeeded. Waiting for whitelist check.");
    }

    private void HandleLoginFailed(string error)
    {
        FireError("Authentication failed. Please restart the launcher.");
    }

#if UNITY_EDITOR
    /// <summary>
    /// Called after a successful anonymous Unity Auth sign-in in dev mode.
    /// Skips the whitelist check and navigates directly to the game scene
    /// (or stays put if PreLoad was injected mid-session).
    /// </summary>
    private void HandleDevAnonLoginSuccess(string _)
    {
        UnityAuthManager.OnLoginSuccess -= HandleDevAnonLoginSuccess;
        UnityAuthManager.OnLoginFailed  -= HandleLoginFailed;
        AppLog.Info($"[Bootstrap] DEV MODE: Anonymous sign-in complete. Player ID: {Unity.Services.Authentication.AuthenticationService.Instance.PlayerId}");
        if (!EditorPreloadInjector.WasInjectedMidSession)
            OnAccessGranted?.Invoke(gameSceneName);
        else
            AppLog.Info("[Bootstrap] DEV MODE: Mid-session inject — staying in current scene. Cloud services are ready.");
    }
#endif

    private void HandleWhitelistGranted(string token)
    {
        AppLog.Info("[Bootstrap] Whitelist granted. Loading game scene.");
        OnAccessGranted?.Invoke(gameSceneName);
    }

    private void HandleWhitelistDenied(string reason)
    {
        FireError($"Access denied: {reason}");
    }

#if UNITY_EDITOR
    private static void SeedDevPlayerRole()
    {
        PlayerRoleProfile.SetGuider(EditorPreloadInjector.DevIsGuider);
    }
#endif

    // ─────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────

    private static void FireStatus(string message)
    {
        AppLog.Info($"[Bootstrap] {message}");
        OnStatusChanged?.Invoke(message);
    }

    private static void FireError(string message)
    {
        AppLog.Error($"[Bootstrap] {message}");
        OnErrorOccurred?.Invoke(message);
    }

    // ─────────────────────────────────────────────────────────────
    // Decode the "email" claim from a Firebase JWT payload.
    // The signature is NOT verified here — Unity Auth backend does
    // the real verification server-side via OIDC.
    // ─────────────────────────────────────────────────────────────

    public static string DecodeEmailFromJwt(string jwt)
    {
        if (string.IsNullOrEmpty(jwt)) return string.Empty;
        try
        {
            string[] parts = jwt.Split('.');
            if (parts.Length != 3) return string.Empty;

            string payload = parts[1];
            int    pad     = (4 - payload.Length % 4) % 4;
            payload        = payload.Replace('-', '+').Replace('_', '/') + new string('=', pad);

            string json  = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            const string key = "\"email\":\"";
            int start = json.IndexOf(key, StringComparison.Ordinal);
            if (start < 0) return string.Empty;
            start += key.Length;
            int end = json.IndexOf('"', start);
            return end > start ? json.Substring(start, end - start) : string.Empty;
        }
        catch
        {
            AppLog.Warning("[Bootstrap] Could not decode email from Firebase JWT.");
            return string.Empty;
        }
    }
}