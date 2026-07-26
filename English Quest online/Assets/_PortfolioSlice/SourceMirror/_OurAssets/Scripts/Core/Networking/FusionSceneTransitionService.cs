using System;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sole entry point for Fusion <see cref="NetworkRunner.LoadScene"/> calls.
/// Serializes in-flight loads per runner and surfaces load failures.
/// </summary>
public static class FusionSceneTransitionService
{
    private static readonly System.Collections.Generic.HashSet<NetworkRunner> _loadingRunners = new();

    public static bool IsLoading(NetworkRunner runner) =>
        runner != null && _loadingRunners.Contains(runner);

    public static bool TryBeginLoad(NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning)
            return false;

        if (!SceneLoadHandle.CanLoadScene(runner))
            return false;

        if (!_loadingRunners.Add(runner))
        {
            AppLog.Warning($"[FusionSceneTransitionService] Scene load already in progress on '{runner.name}'.");
            return false;
        }

        return true;
    }

    public static void EndLoad(NetworkRunner runner)
    {
        if (runner != null)
            _loadingRunners.Remove(runner);
    }

    public static void ClearRunner(NetworkRunner runner) => EndLoad(runner);

    public static async UniTask<bool> LoadSceneAsync(
        NetworkRunner runner,
        int buildIndex,
        string expectedSceneName = null,
        string expectedScenePath = null)
    {
        if (buildIndex < 0)
        {
            AppLog.Error("[FusionSceneTransitionService] Invalid scene build index.");
            return false;
        }

        string sceneName = SceneLoadWaitUtility.ResolveSceneName(buildIndex, expectedSceneName);
        string scenePath = SceneLoadWaitUtility.ResolveScenePath(buildIndex, expectedScenePath);

        if (!TryBeginLoad(runner))
        {
#if UNITY_EDITOR
            return await TryEditorMultiPeerLocalSceneFallbackAsync(
                runner,
                buildIndex,
                sceneName,
                scenePath);
#else
            return false;
#endif
        }

        int sceneLoadVersionBefore = PlayerSpawnCoordinator.SceneLoadDoneVersion;

        try
        {
            await runner.LoadScene(SceneRef.FromIndex(buildIndex));

            if (await SceneLoadWaitUtility.ConfirmFusionDestinationReadyAsync(
                    runner,
                    buildIndex,
                    sceneName,
                    scenePath,
                    sceneLoadVersionBefore))
            {
                return true;
            }

#if UNITY_EDITOR
            return await TryEditorMultiPeerLocalSceneFallbackAsync(
                runner,
                buildIndex,
                sceneName,
                scenePath);
#else
            return false;
#endif
        }
        catch (Exception ex)
        {
            AppLog.Error($"[FusionSceneTransitionService] LoadScene failed for build index {buildIndex}: {ex.Message}");
#if UNITY_EDITOR
            return await TryEditorMultiPeerLocalSceneFallbackAsync(
                runner,
                buildIndex,
                sceneName,
                scenePath);
#else
            return false;
#endif
        }
        finally
        {
            EndLoad(runner);
        }
    }

#if UNITY_EDITOR
    private static async UniTask<bool> TryEditorMultiPeerLocalSceneFallbackAsync(
        NetworkRunner runner,
        int buildIndex,
        string expectedSceneName,
        string expectedScenePath)
    {
        if (NetworkProjectConfig.Global.PeerMode != NetworkProjectConfig.PeerModes.Multiple)
            return false;

        if (SceneLoadWaitUtility.IsDestinationSceneLoaded(
                buildIndex,
                runner,
                expectedSceneName,
                expectedScenePath))
        {
            return true;
        }

        AppLog.Warning(
            $"[FusionSceneTransitionService] Fusion load did not surface '{expectedSceneName}' for '{runner?.name}'. " +
            "Attempting editor additive scene fallback.");

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Additive);
        if (loadOperation == null)
            return false;

        while (!loadOperation.isDone)
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

        return SceneLoadWaitUtility.IsDestinationSceneLoaded(
            buildIndex,
            runner,
            expectedSceneName,
            expectedScenePath);
    }
#endif
}
