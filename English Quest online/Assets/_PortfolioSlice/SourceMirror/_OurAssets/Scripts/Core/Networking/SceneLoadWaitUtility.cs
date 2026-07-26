using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shared scene-loaded wait with timeout and guaranteed handler cleanup.
/// Fusion multi-peer loads merge scenes into <see cref="NetworkRunner.SimulationUnityScene"/>,
/// so detection also checks merged scene roots and in-flight Fusion load state.
/// </summary>
public static class SceneLoadWaitUtility
{
    public const float DefaultTimeoutSeconds = 60f;
    public const float TravelPlayableTimeoutSeconds = 12f;

    public static async UniTask<bool> WaitForSceneActiveAsync(
        int targetBuildIndex,
        float timeoutSeconds = DefaultTimeoutSeconds)
    {
        return await WaitForDestinationReadyAsync(
            targetBuildIndex,
            runner: null,
            expectedSceneName: null,
            expectedScenePath: null,
            timeoutSeconds);
    }

    public static async UniTask<bool> WaitForDestinationReadyAsync(
        int targetBuildIndex,
        NetworkRunner runner = null,
        string expectedSceneName = null,
        string expectedScenePath = null,
        float timeoutSeconds = DefaultTimeoutSeconds,
        int sceneLoadDoneVersionBefore = -1)
    {
        if (targetBuildIndex < 0)
            return true;

        if (IsDestinationSceneLoaded(targetBuildIndex, runner, expectedSceneName, expectedScenePath))
            return true;

        if (runner != null && IsFusionLoadSceneAccepted(runner, targetBuildIndex, expectedSceneName, expectedScenePath))
            return true;

        var tcs = new UniTaskCompletionSource<bool>();
        bool completed = false;

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!DoesSceneMatchDestination(scene, targetBuildIndex, expectedSceneName, expectedScenePath))
                return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (completed)
                return;

            completed = true;
            tcs.TrySetResult(true);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;

        try
        {
            float idleElapsed = 0f;
            const float pollIntervalSeconds = 0.1f;

            while (true)
            {
                if (IsDestinationSceneLoaded(targetBuildIndex, runner, expectedSceneName, expectedScenePath))
                    return true;

                if (runner != null
                    && IsFusionLoadSceneAccepted(runner, targetBuildIndex, expectedSceneName, expectedScenePath))
                {
                    return true;
                }

                if (runner != null
                    && sceneLoadDoneVersionBefore >= 0
                    && PlayerSpawnCoordinator.SceneLoadDoneVersion > sceneLoadDoneVersionBefore)
                {
                    return IsDestinationSceneLoaded(targetBuildIndex, runner, expectedSceneName, expectedScenePath)
                           || IsFusionLoadSceneAccepted(runner, targetBuildIndex, expectedSceneName, expectedScenePath);
                }

                if (runner != null && !runner.IsRunning)
                    return false;

                bool loadInProgress = IsFusionSceneLoadInProgress(runner, sceneLoadDoneVersionBefore);
                if (!loadInProgress && timeoutSeconds > 0f)
                {
                    if (idleElapsed >= timeoutSeconds)
                        break;

                    idleElapsed += pollIntervalSeconds;
                }

                (bool sceneLoadedFirst, _) = await UniTask.WhenAny(
                    tcs.Task,
                    UniTask.Delay(TimeSpan.FromSeconds(pollIntervalSeconds)));

                if (sceneLoadedFirst || IsDestinationSceneLoaded(targetBuildIndex, runner, expectedSceneName, expectedScenePath))
                    return true;
            }

            if (IsDestinationSceneLoaded(targetBuildIndex, runner, expectedSceneName, expectedScenePath))
                return true;

            if (runner != null
                && IsFusionLoadSceneAccepted(runner, targetBuildIndex, expectedSceneName, expectedScenePath))
            {
                return true;
            }

            if (IsFusionSceneLoadInProgress(runner, sceneLoadDoneVersionBefore))
            {
                AppLog.Warning(
                    $"[SceneLoadWaitUtility] Still waiting for Fusion to finish loading scene build index {targetBuildIndex} ('{ResolveSceneName(targetBuildIndex, expectedSceneName)}').");
                return false;
            }

            AppLog.Error(
                $"[SceneLoadWaitUtility] Timed out after {timeoutSeconds:0.#}s waiting for scene build index {targetBuildIndex} ('{ResolveSceneName(targetBuildIndex, expectedSceneName)}').");
            return false;
        }
        finally
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    public static async UniTask<bool> ConfirmFusionDestinationReadyAsync(
        NetworkRunner runner,
        int buildIndex,
        string expectedSceneName = null,
        string expectedScenePath = null,
        int sceneLoadDoneVersionBefore = -1)
    {
        if (buildIndex < 0)
            return true;

        if (runner == null || !runner.IsRunning)
            return false;

        if (IsDestinationSceneLoaded(buildIndex, runner, expectedSceneName, expectedScenePath))
            return true;

        if (IsFusionLoadSceneAccepted(runner, buildIndex, expectedSceneName, expectedScenePath))
            return true;

        if (sceneLoadDoneVersionBefore >= 0
            && PlayerSpawnCoordinator.SceneLoadDoneVersion > sceneLoadDoneVersionBefore)
        {
            return IsDestinationSceneLoaded(buildIndex, runner, expectedSceneName, expectedScenePath)
                   || IsFusionLoadSceneAccepted(runner, buildIndex, expectedSceneName, expectedScenePath);
        }

        var tcs = new UniTaskCompletionSource<bool>();
        bool completed = false;

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!DoesSceneMatchDestination(scene, buildIndex, expectedSceneName, expectedScenePath))
                return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (completed)
                return;

            completed = true;
            tcs.TrySetResult(true);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;

        try
        {
            const float pollIntervalSeconds = 0.1f;

            while (runner.IsRunning)
            {
                if (IsDestinationSceneLoaded(buildIndex, runner, expectedSceneName, expectedScenePath))
                    return true;

                if (IsFusionLoadSceneAccepted(runner, buildIndex, expectedSceneName, expectedScenePath))
                    return true;

                if (sceneLoadDoneVersionBefore >= 0
                    && PlayerSpawnCoordinator.SceneLoadDoneVersion > sceneLoadDoneVersionBefore)
                {
                    return IsDestinationSceneLoaded(buildIndex, runner, expectedSceneName, expectedScenePath)
                           || IsFusionLoadSceneAccepted(runner, buildIndex, expectedSceneName, expectedScenePath);
                }

                if (!IsFusionSceneLoadInProgress(runner, sceneLoadDoneVersionBefore))
                    break;

                (bool sceneLoadedFirst, _) = await UniTask.WhenAny(
                    tcs.Task,
                    UniTask.Delay(TimeSpan.FromSeconds(pollIntervalSeconds)));

                if (sceneLoadedFirst)
                    return true;
            }

            return IsDestinationSceneLoaded(buildIndex, runner, expectedSceneName, expectedScenePath)
                   || IsFusionLoadSceneAccepted(runner, buildIndex, expectedSceneName, expectedScenePath)
                   || (sceneLoadDoneVersionBefore >= 0
                       && PlayerSpawnCoordinator.SceneLoadDoneVersion > sceneLoadDoneVersionBefore);
        }
        finally
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    public static bool IsDestinationSceneLoaded(
        int buildIndex,
        NetworkRunner runner = null,
        string expectedSceneName = null,
        string expectedScenePath = null)
    {
        if (buildIndex < 0)
            return true;

        return TryResolveLoadedDestinationScene(
            buildIndex,
            expectedSceneName,
            expectedScenePath,
            runner,
            out _);
    }

    public static bool TryResolveLoadedDestinationScene(
        int buildIndex,
        string expectedSceneName,
        string expectedScenePath,
        NetworkRunner runner,
        out Scene destinationScene)
    {
        destinationScene = default;

        if (runner != null && runner.IsRunning)
        {
            if (TryResolveRunnerDestinationScene(
                    runner,
                    buildIndex,
                    expectedSceneName,
                    expectedScenePath,
                    out destinationScene))
            {
                return true;
            }

            return false;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (DoesSceneMatchDestination(scene, buildIndex, expectedSceneName, expectedScenePath))
            {
                destinationScene = scene;
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveRunnerDestinationScene(
        NetworkRunner runner,
        int buildIndex,
        string expectedSceneName,
        string expectedScenePath,
        out Scene destinationScene)
    {
        destinationScene = default;

        foreach (NetworkRunner peer in FusionCoSessionRunners.Enumerate(runner))
        {
            if (peer == null || !peer.IsRunning)
                continue;

            Scene simulationScene = peer.SimulationUnityScene;
            if (DoesSceneMatchDestination(simulationScene, buildIndex, expectedSceneName, expectedScenePath))
            {
                destinationScene = simulationScene;
                return true;
            }

            if (TryResolveFusionMultiPeerDestination(
                    peer,
                    buildIndex,
                    expectedSceneName,
                    expectedScenePath,
                    out destinationScene))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsFusionLoadSceneAccepted(
        NetworkRunner runner,
        int buildIndex,
        string expectedSceneName = null,
        string expectedScenePath = null)
    {
        if (runner == null || !runner.IsRunning)
            return false;

        if (IsDestinationSceneLoaded(buildIndex, runner, expectedSceneName, expectedScenePath))
            return true;

        if (TryResolveFusionMultiPeerDestination(
                runner,
                buildIndex,
                expectedSceneName,
                expectedScenePath,
                out _))
        {
            return true;
        }

        return false;
    }

    public static bool IsFusionSceneLoadInProgress(NetworkRunner runner, int sceneLoadDoneVersionBefore = -1)
    {
        if (runner == null)
            return false;

        foreach (NetworkRunner peer in FusionCoSessionRunners.Enumerate(runner))
        {
            if (peer == null || !peer.IsRunning)
                continue;

            if (FusionSceneTransitionService.IsLoading(peer))
                return true;

            if (peer.SceneManager != null && peer.SceneManager.IsBusy)
                return true;
        }

        if (sceneLoadDoneVersionBefore >= 0
            && PlayerSpawnCoordinator.SceneLoadDoneVersion <= sceneLoadDoneVersionBefore)
        {
            return true;
        }

        return false;
    }

    public static string ResolveSceneName(int buildIndex, string expectedSceneName = null)
    {
        if (!string.IsNullOrEmpty(expectedSceneName))
            return expectedSceneName;

        if (buildIndex < 0)
            return null;

        string scenePath = ResolveScenePath(buildIndex);
        return string.IsNullOrEmpty(scenePath) ? null : Path.GetFileNameWithoutExtension(scenePath);
    }

    public static string ResolveScenePath(int buildIndex, string expectedScenePath = null)
    {
        if (!string.IsNullOrEmpty(expectedScenePath))
            return expectedScenePath;

        if (buildIndex < 0)
            return null;

        return SceneUtility.GetScenePathByBuildIndex(buildIndex);
    }

    private static bool TryResolveFusionMultiPeerDestination(
        NetworkRunner runner,
        int buildIndex,
        string expectedSceneName,
        string expectedScenePath,
        out Scene destinationScene)
    {
        destinationScene = default;

        if (runner == null || !runner.IsRunning)
            return false;

        if (NetworkProjectConfig.Global.PeerMode != NetworkProjectConfig.PeerModes.Multiple)
            return false;

        Scene simulationScene = runner.SimulationUnityScene;
        if (!simulationScene.IsValid() || !simulationScene.isLoaded)
            return false;

        string sceneName = ResolveSceneName(buildIndex, expectedSceneName);
        string scenePath = ResolveScenePath(buildIndex, expectedScenePath);

        foreach (GameObject rootObject in simulationScene.GetRootGameObjects())
        {
            if (!TryParseMultiPeerSceneRootName(rootObject.name, out string rootSceneName))
                continue;

            if (!string.IsNullOrEmpty(sceneName) && SceneNamesMatch(rootSceneName, sceneName))
            {
                destinationScene = simulationScene;
                return true;
            }
        }

        return false;
    }

    private static bool TryParseMultiPeerSceneRootName(string objectName, out string sceneName)
    {
        sceneName = null;
        if (string.IsNullOrEmpty(objectName) || objectName.Length < 3)
            return false;

        if (objectName[0] != '[' || objectName[^1] != ']')
            return false;

        sceneName = objectName.Substring(1, objectName.Length - 2);
        return !string.IsNullOrEmpty(sceneName);
    }

    private static bool DoesSceneMatchDestination(
        Scene scene,
        int buildIndex,
        string expectedSceneName,
        string expectedScenePath)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return false;

        if (buildIndex >= 0 && scene.buildIndex == buildIndex)
            return true;

        string sceneName = ResolveSceneName(buildIndex, expectedSceneName);
        if (!string.IsNullOrEmpty(sceneName) && SceneNamesMatch(scene.name, sceneName))
            return true;

        string scenePath = ResolveScenePath(buildIndex, expectedScenePath);
        if (!string.IsNullOrEmpty(scenePath) && PathsMatch(scene.path, scenePath))
            return true;

        return false;
    }

    private static bool SceneNamesMatch(string actual, string expected)
    {
        if (string.IsNullOrEmpty(actual) || string.IsNullOrEmpty(expected))
            return false;

        if (string.Equals(actual, expected, StringComparison.Ordinal))
            return true;

        return actual.StartsWith(expected + "(", StringComparison.Ordinal);
    }

    private static bool PathsMatch(string actual, string expected)
    {
        if (string.IsNullOrEmpty(actual) || string.IsNullOrEmpty(expected))
            return false;

        return string.Equals(NormalizePath(actual), NormalizePath(expected), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private static bool IsBootstrapScene(Scene scene)
    {
        if (!scene.IsValid())
            return false;

        string name = scene.name;
        return name == "PreLoad"
               || name == "DontDestroyOnLoad"
               || name == "Bootstrap";
    }
}
