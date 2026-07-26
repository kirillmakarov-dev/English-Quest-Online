using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

/// <summary>
/// Handles Unity Authentication Service sign-in and Cloud Save.
///
/// Only compiled when UNITY_AUTHENTICATION_ENABLED is defined.
/// To enable:
///   1. Install com.unity.services.authentication (Package Manager)
///   2. Add UNITY_AUTHENTICATION_ENABLED to
///      Project Settings → Player → Scripting Define Symbols
/// </summary>
public class UnityAuthManager : MonoBehaviour
{
    public static UnityAuthManager Instance { get; private set; }

    /// <summary>
    /// Must match the OIDC provider name configured in Unity Dashboard.
    /// Typical format: "oidc-firebase" (check your Dashboard → Authentication → Providers).
    /// </summary>
    public const string OIDC_PROVIDER_NAME = "oidc-firebase";

    /// <summary>Fired when sign-in completes successfully. Passes the player email.</summary>
    public static event Action<string> OnLoginSuccess;

    /// <summary>Fired when sign-in fails. Passes the error message.</summary>
    public static event Action<string> OnLoginFailed;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    // ─────────────────────────────────────────────────────────────
    // ANONYMOUS — Sign in without any external credential
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Signs into Unity Authentication anonymously.
    /// Produces a real PlayerId and session token so all Unity Gaming Services
    /// (CloudSave, CloudCode, etc.) work — no Firebase token required.
    ///
    /// Only called in editor dev mode (DevAuthMode.AnonymousUnityAuth).
    /// The whitelist check is intentionally skipped by the caller (GameBootstrap).
    /// </summary>
    public async void LoginAnonymously()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (AuthenticationService.Instance.IsSignedIn)
            {
                AppLog.Info("[Auth] Already signed in. Reusing session for anonymous dev mode.");
                OnLoginSuccess?.Invoke(string.Empty);
                return;
            }

            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            AppLog.Info($"[Auth] Anonymous sign-in succeeded. Player ID: {AuthenticationService.Instance.PlayerId}");
            // Pass empty string — GameBootstrap checks for this and skips the whitelist.
            OnLoginSuccess?.Invoke(string.Empty);
        }
        catch (AuthenticationException ex)
        {
            AppLog.Error($"[Auth] Anonymous auth failed: {ex.Message}");
            OnLoginFailed?.Invoke(ex.Message);
        }
        catch (RequestFailedException ex)
        {
            AppLog.Error($"[Auth] Request failed: {ex.Message}");
            OnLoginFailed?.Invoke(ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // OPENID CONNECT — Sign in with Firebase ID token
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Signs into Unity Authentication using the Firebase ID token as an
    /// OpenID Connect credential.  The provider must be configured in the
    /// Unity Dashboard before calling this.
    /// </summary>
    public async void LoginWithOpenIdConnect(string firebaseIdToken)
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (AuthenticationService.Instance.IsSignedIn)
            {
                string cachedEmail = PlayerPrefs.GetString("playerEmail", "");
                AppLog.Info("[Auth] Already signed in. Reusing session.");
                OnLoginSuccess?.Invoke(cachedEmail);
                return;
            }

            await AuthenticationService.Instance.SignInWithOpenIdConnectAsync(
                OIDC_PROVIDER_NAME, firebaseIdToken);

            string email = GameBootstrap.DecodeEmailFromJwt(firebaseIdToken);
            PlayerPrefs.SetString("playerEmail", email);
            PlayerPrefs.Save();

            AppLog.Info($"[Auth] OIDC sign-in succeeded. Player ID: {AuthenticationService.Instance.PlayerId}");
            // Pass the original Firebase ID token so CloudCodeWhitelistChecker can verify it.
            OnLoginSuccess?.Invoke(firebaseIdToken);
        }
        catch (AuthenticationException ex)
        {
            AppLog.Error($"[Auth] Auth failed: {ex.Message}");
            OnLoginFailed?.Invoke(ex.Message);
        }
        catch (RequestFailedException ex)
        {
            AppLog.Error($"[Auth] Request failed: {ex.Message}");
            OnLoginFailed?.Invoke(ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // CLOUD SAVE — Save / Load keyed player data
    // ─────────────────────────────────────────────────────────────

    public async UniTask SavePlayerData(string key, string value)
    {
        var data = new Dictionary<string, object> { { key, value } };
        await CloudSaveService.Instance.Data.Player.SaveAsync(data);
        AppLog.Info($"[CloudSave] Saved: {key} = {value}");
    }

    public async UniTask<string> LoadPlayerData(string key)
    {
        var keys   = new HashSet<string> { key };
        var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

        if (result.TryGetValue(key, out var item))
            return item.Value.GetAs<string>();

        return null;
    }

}
