using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityServiceLocator;

/// <summary>
/// Loads a scene in the background without activating it (local path),
/// or awaits a Fusion scene load (networked path).
/// </summary>
public class SceneLoadHandle
{
    private AsyncOperation _asyncOperation;
    private bool _useNetworkLoad;
    private int _targetBuildIndex = -1;
    private string _expectedSceneName;
    private string _expectedScenePath;
    private NetworkRunner _waitRunner;
    private UniTask<bool> _networkLoadTask;
    private ThreadPriority _previousLoadingPriority = Application.backgroundLoadingPriority;

    public bool RequiresActivation => !_useNetworkLoad && _asyncOperation != null;

    internal NetworkRunner WaitRunner => _waitRunner;

    public bool IsReady
    {
        get
        {
            if (_useNetworkLoad)
                return _networkLoadTask.Status.IsCompleted()
                    && _networkLoadTask.GetAwaiter().GetResult();

            return _asyncOperation != null && _asyncOperation.progress >= 0.9f;
        }
    }

    public static SceneLoadHandle BeginLocal(SceneReference scene)
    {
        if (!scene.IsValid)
        {
            AppLog.Error($"[SceneLoadHandle] Invalid scene reference: {scene.ScenePath}");
            return null;
        }

        var handle = new SceneLoadHandle();
        handle._previousLoadingPriority = Application.backgroundLoadingPriority;
        Application.backgroundLoadingPriority = ThreadPriority.Low;
        handle._asyncOperation = SceneManager.LoadSceneAsync(scene.BuildIndex);
        if (handle._asyncOperation == null)
        {
            AppLog.Error($"[SceneLoadHandle] Failed to start async load for {scene.SceneName}");
            return null;
        }

        handle._asyncOperation.allowSceneActivation = false;
        handle._targetBuildIndex = scene.BuildIndex;
        handle._expectedSceneName = scene.SceneName;
        handle._expectedScenePath = scene.ScenePath;
        return handle;
    }

    public static SceneLoadHandle BeginNetworked(SceneReference scene, MonoBehaviour context, bool allowLocalFallback = true)
    {
        if (!ServiceLocator.For(context).TryGet<INetworkSessionService>(out var netSession) ||
            netSession.Runner == null || !netSession.Runner.IsRunning)
        {
            if (!allowLocalFallback)
            {
                AppLog.Error("[SceneLoadHandle] No active Fusion runner on context; refusing local fallback.");
                return null;
            }

            AppLog.Warning("[SceneLoadHandle] No active Fusion runner on context; falling back to local load.");
            return BeginLocal(scene);
        }

        return BeginNetworked(scene, netSession.Runner, allowLocalFallback);
    }

    public static SceneLoadHandle BeginNetworked(
        SceneReference scene,
        NetworkRunner runner,
        bool allowLocalFallback = true)
    {
        if (!scene.IsValid)
        {
            AppLog.Error($"[SceneLoadHandle] Invalid scene reference: {scene.ScenePath}");
            return null;
        }

        if (runner == null || !runner.IsRunning)
        {
            if (!allowLocalFallback)
            {
                AppLog.Error("[SceneLoadHandle] No active Fusion runner; refusing local fallback.");
                return null;
            }

            AppLog.Warning("[SceneLoadHandle] No active Fusion runner; falling back to local load.");
            return BeginLocal(scene);
        }

        if (!CanLoadScene(runner))
        {
            AppLog.Warning("[SceneLoadHandle] Only scene authority can load networked scenes.");
            return null;
        }

        var handle = new SceneLoadHandle
        {
            _useNetworkLoad = true,
            _targetBuildIndex = scene.BuildIndex,
            _expectedSceneName = scene.SceneName,
            _expectedScenePath = scene.ScenePath,
            _waitRunner = runner,
            _networkLoadTask = RunNetworkLoadAsync(runner, scene.BuildIndex, scene.SceneName, scene.ScenePath)
        };
        return handle;
    }

    public static SceneLoadHandle BeginWaitForScene(
        int buildIndex,
        NetworkRunner runner = null,
        string expectedSceneName = null,
        string expectedScenePath = null)
    {
        if (buildIndex < 0)
            return null;

        return new SceneLoadHandle
        {
            _useNetworkLoad = true,
            _targetBuildIndex = buildIndex,
            _expectedSceneName = SceneLoadWaitUtility.ResolveSceneName(buildIndex, expectedSceneName),
            _expectedScenePath = SceneLoadWaitUtility.ResolveScenePath(buildIndex, expectedScenePath),
            _waitRunner = runner,
            _networkLoadTask = WaitForSceneLoadedAsync(buildIndex, runner, expectedSceneName, expectedScenePath)
        };
    }

    public static async UniTask<SceneLoadHandle> BeginNetworkedAsync(SceneReference scene, MonoBehaviour context)
    {
        SceneLoadHandle handle = BeginNetworked(scene, context, allowLocalFallback: false);
        if (handle == null)
            return null;

        bool ready = await handle.WaitUntilReadyAsync();
        return ready ? handle : null;
    }

    public static bool CanLoadScene(NetworkRunner runner)
    {
        return runner != null && runner.IsRunning && (runner.IsSceneAuthority || runner.IsSharedModeMasterClient);
    }

    public async UniTask<bool> WaitUntilReadyAsync()
    {
        if (_useNetworkLoad)
            return await _networkLoadTask;

        while (!IsReady)
            await UniTask.DelayFrame(2, PlayerLoopTiming.LastPostLateUpdate);

        RestoreLoadingPriority();
        return true;
    }

    public void Activate()
    {
        if (_useNetworkLoad)
            return;

        if (_asyncOperation != null)
            _asyncOperation.allowSceneActivation = true;

        RestoreLoadingPriority();
    }

    private void RestoreLoadingPriority()
    {
        Application.backgroundLoadingPriority = _previousLoadingPriority;
    }

    private static async UniTask<bool> RunNetworkLoadAsync(
        NetworkRunner runner,
        int buildIndex,
        string expectedSceneName,
        string expectedScenePath)
    {
        return await FusionSceneTransitionService.LoadSceneAsync(
            runner,
            buildIndex,
            expectedSceneName,
            expectedScenePath);
    }

    public static UniTask<bool> WaitUntilSceneActiveAsync(
        int targetBuildIndex,
        NetworkRunner runner = null,
        string expectedSceneName = null,
        string expectedScenePath = null) =>
        WaitForSceneLoadedAsync(targetBuildIndex, runner, expectedSceneName, expectedScenePath);

    private static UniTask<bool> WaitForSceneLoadedAsync(
        int targetBuildIndex,
        NetworkRunner runner = null,
        string expectedSceneName = null,
        string expectedScenePath = null) =>
        SceneLoadWaitUtility.WaitForDestinationReadyAsync(
            targetBuildIndex,
            runner,
            SceneLoadWaitUtility.ResolveSceneName(targetBuildIndex, expectedSceneName),
            SceneLoadWaitUtility.ResolveScenePath(targetBuildIndex, expectedScenePath),
            SceneLoadWaitUtility.DefaultTimeoutSeconds);
}
