using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Services.CloudCode;
using UnityEngine;

public class CloudCodeWhitelistChecker : MonoBehaviour
{
    // The name of the script in your Unity Dashboard
    private const string CloudCodeEndpoint = "CheckEmail";

    public static event Action<string> OnWhitelistGranted;
    public static event Action<string> OnWhitelistDenied;

    private void OnEnable()
    {
        // Ensure your UnityAuthManager passes the Firebase ID Token string!
        UnityAuthManager.OnLoginSuccess += HandleLoginSuccess;
    }

    private void OnDisable()
    {
        UnityAuthManager.OnLoginSuccess -= HandleLoginSuccess;
    }

    private async void HandleLoginSuccess(string firebaseIdToken)
    {
#if UNITY_EDITOR
        // In AnonymousUnityAuth dev mode, GameBootstrap subscribes its own handler
        // and fires OnAccessGranted directly — the whitelist check must not run.
        // An empty token is the signal that this is an anonymous dev session.
        if (EditorPreloadInjector.DevBypassActive)
        {
            AppLog.Info("[Whitelist] DEV MODE: whitelist check skipped.");
            return;
        }
#endif
        // IMPORTANT: firebaseIdToken must be the actual JWT token from Firebase
        await CheckWhitelistAsync(firebaseIdToken);
    }

    public async UniTask CheckWhitelistAsync(string idToken)
    {
        if (string.IsNullOrEmpty(idToken))
        {
            OnWhitelistDenied?.Invoke("No authentication token available.");
            return;
        }

        AppLog.Info("[Whitelist] Sending verification token to Cloud Code...");

        try
        {
            // The key "firebaseIdToken" must match params["firebaseIdToken"] in JS
            var args = new Dictionary<string, object> { { "firebaseIdToken", idToken } };

            // We use CallEndpointAsync for standard Scripts
            string result = await CloudCodeService.Instance.CallEndpointAsync<string>(CloudCodeEndpoint, args);

            HandleCloudCodeResponse(result?.Trim());
        }
        catch (CloudCodeException ex)
        {
            AppLog.Error($"[Whitelist] Cloud Code error ({ex.ErrorCode}): {ex.Message}");
            OnWhitelistDenied?.Invoke("Whitelist service unavailable (500).");
        }
        catch (Exception ex)
        {
            AppLog.Error($"[Whitelist] Unexpected error: {ex.Message}");
            OnWhitelistDenied?.Invoke("Connection error. Please try again.");
        }
    }

    private static void HandleCloudCodeResponse(string response)
    {
        switch (WhitelistAccessResultMapper.Map(response))
        {
            case WhitelistAccessOutcome.Student:
                AppLog.Info("[Whitelist] Access granted (student).");
                PlayerRoleProfile.SetGuider(false);
                OnWhitelistGranted?.Invoke("Success");
                break;

            case WhitelistAccessOutcome.Guider:
                AppLog.Info("[Whitelist] Access granted (guider).");
                PlayerRoleProfile.SetGuider(true);
                OnWhitelistGranted?.Invoke("Success");
                break;

            case WhitelistAccessOutcome.NotFound:
                AppLog.Warning("[Whitelist] Email not found in Firestore.");
                PlayerRoleProfile.Clear();
                OnWhitelistDenied?.Invoke("Email not found in the course roster.");
                break;

            default:
                AppLog.Warning($"[Whitelist] Access Denied. Response: {response}");
                PlayerRoleProfile.Clear();
                OnWhitelistDenied?.Invoke("Access denied. Please contact your instructor.");
                break;
        }
    }
}
