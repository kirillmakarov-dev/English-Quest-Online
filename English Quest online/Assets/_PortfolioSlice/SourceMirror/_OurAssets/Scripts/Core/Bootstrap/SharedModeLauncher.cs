using System;
using Fusion;
using UnityEngine;

#if !UNITY_EDITOR
[Obsolete("Use GameNetworkManager via INetworkSessionService for production session startup.")]
#endif
public class SharedModeLauncher : MonoBehaviour
{
#if UNITY_EDITOR
    private NetworkRunner _runner;

    public void OnEnterSharedModeButtonClicked()
    {
        AppLog.Warning(
            "[SharedModeLauncher] Legacy test launcher. Prefer GameNetworkManager / INetworkSessionService in production.");
        StartSharedMode();
    }

    private async void StartSharedMode()
    {
        if (_runner == null)
            _runner = gameObject.AddComponent<NetworkRunner>();

        var sceneManager = _runner.GetComponent<EnglishQuestNetworkSceneManager>();
        if (sceneManager == null)
            sceneManager = _runner.gameObject.AddComponent<EnglishQuestNetworkSceneManager>();

        EnsureRunnerObjectProvider(_runner.gameObject);

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = "SharedSession",
            Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex),
            SceneManager = sceneManager
        });

        if (result.Ok)
        {
            AppLog.Info("Shared Mode Started Successfully");
            NetworkRunnerCallbackHub.BindRunner(_runner);
            PlayerSpawnCoordinator.BindRunner(_runner);
        }
        else
        {
            NetworkSessionErrorService.ReportStartGameFailure(result.ShutdownReason);
            AppLog.Error($"Failed to start Shared Mode: {result.ShutdownReason}");
        }
    }

    private static void EnsureRunnerObjectProvider(GameObject runnerGo)
    {
        NetworkObjectProviderDefault existingDefault = runnerGo.GetComponent<NetworkObjectProviderDefault>();
        if (existingDefault != null && existingDefault is not EnglishQuestNetworkObjectProvider)
            UnityEngine.Object.Destroy(existingDefault);

        EnglishQuestNetworkObjectProvider provider = runnerGo.GetComponent<EnglishQuestNetworkObjectProvider>();
        if (provider == null)
            provider = runnerGo.AddComponent<EnglishQuestNetworkObjectProvider>();

        provider.DelayIfSceneManagerIsBusy = true;
    }
#else
    public void OnEnterSharedModeButtonClicked()
    {
        AppLog.Error("[SharedModeLauncher] Disabled outside the Unity Editor. Use GameNetworkManager instead.");
    }
#endif
}

