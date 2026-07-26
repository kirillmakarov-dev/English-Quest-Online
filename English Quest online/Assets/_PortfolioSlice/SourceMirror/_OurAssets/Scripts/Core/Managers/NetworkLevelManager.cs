using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using EnglishKingdom.UI.Loading;
using UnityServiceLocator;

public class NetworkLevelManager : Singleton<NetworkLevelManager>, ILevelManager
{
    private bool _isTransitioning;

    public static bool IsReturningToMenu =>
        Instance != null && Instance._isTransitioning;

    protected override void Awake()
    {
        base.Awake();
        if (Instance == this)
            ServiceLocator.For(this).Register<ILevelManager>(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            ServiceLocator.DeregisterFor<ILevelManager>(this);
    }

    // Call this to change levels with a transition
    public async UniTask TransitionToSceneAsync(string sceneName)
    {
        if (sceneName.IndexOf("menu", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            ReturnToMenu();
            return;
        }
        _isTransitioning = true;
        
        // We access the Runner from the GameNetworkManager
        var runner = ServiceLocator.For(this).TryGet<INetworkSessionService>(out var netSession) ? netSession.Runner : null;

        if (IsRunnerInvalid(runner))
        {
            AppLog.Error("Network Runner is not running. Cannot transition via Fusion.");
            _isTransitioning = false;
            return;
        }

        int sceneBuildIndex = SceneUtility.GetBuildIndexByScenePath(sceneName);
        if (sceneBuildIndex == -1)
        {
            AppLog.Error($"Could not find scene {sceneName} to load. Loading via Fusion failed.");
            _isTransitioning = false;
            return;
        }

        // In Shared Mode, only scene authority or master client should initiate scene loads.
        if (!SceneLoadHandle.CanLoadScene(runner))
        {
            bool followed = await TryFollowAuthoritySceneLoadAsync(runner, sceneBuildIndex, sceneName);
            _isTransitioning = false;
            if (!followed)
                AppLog.Warning($"[NetworkLevelManager] Non-authority client could not follow scene load to '{sceneName}'.");
            return;
        }

        if (ServiceLocator.For(this).TryGet<ILoadingScreenService>(out var loadingScreen))
        {
            await loadingScreen.LoadSceneNetworkAsync(async () =>
            {
                bool loaded = await FusionSceneTransitionService.LoadSceneAsync(runner, sceneBuildIndex);
                if (!loaded)
                    throw new System.Exception($"Failed to load scene build index {sceneBuildIndex} via Fusion.");
            });
        }
        else
        {
            bool loaded = await FusionSceneTransitionService.LoadSceneAsync(runner, sceneBuildIndex);
            if (!loaded)
                AppLog.Error($"Could not load scene {sceneName} via Fusion.");
        }

        _isTransitioning = false;
    }

    private static async UniTask<bool> TryFollowAuthoritySceneLoadAsync(
        NetworkRunner runner,
        int sceneBuildIndex,
        string sceneName)
    {
        PlayerInteraction localInteractor = ResolveLocalPlayerInteraction(runner);

        if (ServiceLocator.Global != null
            && ServiceLocator.Global.TryGet<ILoadingScreenService>(out ILoadingScreenService loadingScreen))
        {
            bool followed = false;
            await loadingScreen.LoadSceneNetworkAsync(async () =>
            {
                followed = await FusionSceneFollowService.LoadOrWaitForSceneAsync(
                    runner,
                    sceneBuildIndex,
                    localInteractor,
                    sceneName);
            });
            return followed;
        }

        return await FusionSceneFollowService.LoadOrWaitForSceneAsync(
            runner,
            sceneBuildIndex,
            localInteractor,
            sceneName);
    }

    private static PlayerInteraction ResolveLocalPlayerInteraction(NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning)
            return null;

        NetworkObject localPlayer = runner.GetPlayerObject(runner.LocalPlayer);
        if (localPlayer == null)
            return null;

        return localPlayer.GetComponent<PlayerInteraction>()
            ?? localPlayer.GetComponentInChildren<PlayerInteraction>();
    }

    private static bool IsRunnerInvalid(NetworkRunner runner)
    {
        return runner == null || !runner.IsRunning;
    }


    public async void ReturnToMenu()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;

        if (ServiceLocator.For(this).TryGet<ILoadingScreenService>(out var loadingScreen))
        {
            await loadingScreen.LoadSceneNetworkAsync(async () => 
            {
                // Disconnect/Shutdown Fusion Runner
                if (ServiceLocator.For(this).TryGet<INetworkSessionService>(out var netSession))
                {
                    await netSession.Disconnect();
                }

                // Load Menu Scene
                SceneManager.LoadScene("Menu");
            });
        }
        else
        {
            // Disconnect/Shutdown Fusion Runner
            if (ServiceLocator.For(this).TryGet<INetworkSessionService>(out var netSession))
            {
                await netSession.Disconnect();
            }

            // Load Menu Scene
            SceneManager.LoadScene("Menu");
        }
        
        _isTransitioning = false;
    }
}
