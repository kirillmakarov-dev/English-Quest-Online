using System;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityServiceLocator;
using EnglishQuest.UI.Loading;

public class GameNetworkManager : Singleton<GameNetworkManager>, INetworkSessionService
{
    public NetworkRunner Runner => ResolveRunner();

    [SerializeField] private EnglishQuestNetworkSceneManager _sceneManager;
    [SerializeField] private NetworkSessionProfile _openWorldProfile;
    [SerializeField] private NetworkPrefabRef _sessionBridgePrefab;

    private NetworkRunner _runner;
    private NetworkLifecycleHandler _lifecycleHandler;
    private NetworkAuthorityService _authorityService;

    private const int DefaultOpenWorldMaxPlayers = 2;
    private const string DefaultOpenWorldSceneName = "OpenWorld";

    protected override void Awake()
    {
        base.Awake();
        if (Instance == this)
        {
            ServiceLocator.For(this).Register<INetworkSessionService>(this);

            if (GetComponent<PlayerSpawnCoordinator>() == null)
                gameObject.AddComponent<PlayerSpawnCoordinator>();

            if (GetComponent<NetworkLifecycleHandler>() == null)
                gameObject.AddComponent<NetworkLifecycleHandler>();

            if (GetComponent<NetworkAuthorityService>() == null)
                gameObject.AddComponent<NetworkAuthorityService>();

            if (GetComponent<EnglishQuestNetworkSceneManager>() == null)
                _sceneManager = gameObject.AddComponent<EnglishQuestNetworkSceneManager>();
            else if (_sceneManager == null)
                _sceneManager = GetComponent<EnglishQuestNetworkSceneManager>();

            _lifecycleHandler = GetComponent<NetworkLifecycleHandler>();
            _lifecycleHandler.UnexpectedShutdown += HandleUnexpectedShutdown;
            _authorityService = GetComponent<NetworkAuthorityService>();
        }
    }

    private void OnDestroy()
    {
        if (_lifecycleHandler != null)
            _lifecycleHandler.UnexpectedShutdown -= HandleUnexpectedShutdown;

        if (Instance == this)
            ServiceLocator.DeregisterFor<INetworkSessionService>(this);
    }

    private void HandleUnexpectedShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (_runner == runner)
            _runner = null;

        NetworkSessionErrorService.Report(
            NetworkSessionErrorService.FormatStartGameFailure(shutdownReason));

        if (ServiceLocator.For(this).TryGet<ILevelManager>(out var levelManager))
            levelManager.ReturnToMenu();
    }

    public async UniTask StartSharedSession(string sessionName, int sceneBuildIndex)
    {
        await StartSharedSession(sessionName, sceneBuildIndex, DefaultOpenWorldMaxPlayers, enableClientSessionCreation: true);
    }

    public async UniTask StartSharedSession(
        string sessionName,
        int sceneBuildIndex,
        int maxPlayers,
        bool enableClientSessionCreation = true)
    {
        EnsureRunnerExists();

        if (sceneBuildIndex < 0)
        {
            AppLog.Error($"Invalid scene build index ({sceneBuildIndex}). Make sure the scene is in Build Settings.");
            return;
        }

        var sceneRef = SceneRef.FromIndex(sceneBuildIndex);

        if (ServiceLocator.For(this).TryGet<ILoadingScreenService>(out var loadingScreen))
        {
            await loadingScreen.LoadSceneNetworkAsync(
                async () => await StartGameAndAwaitSceneLoadAsync(
                    sessionName,
                    sceneRef,
                    sceneBuildIndex,
                    maxPlayers,
                    enableClientSessionCreation));
        }
        else
        {
            AppLog.Warning("LoadingScreenManager not found, running without loading screen.");
            await StartGameAndAwaitSceneLoadAsync(
                sessionName,
                sceneRef,
                sceneBuildIndex,
                maxPlayers,
                enableClientSessionCreation);
        }

        AppLog.Info($"Started Shared Session: {sessionName}");
    }

    public UniTask StartSharedSession(NetworkSessionProfile profile)
    {
        if (profile == null)
        {
            AppLog.Error("[GameNetworkManager] Session profile is null.");
            return UniTask.CompletedTask;
        }

        int sceneBuildIndex = profile.ResolveInitialSceneBuildIndex();
        if (sceneBuildIndex < 0)
        {
            AppLog.Error(
                $"[GameNetworkManager] Profile '{profile.name}' could not resolve scene '{profile.InitialSceneName}'.");
            return UniTask.CompletedTask;
        }

        return StartSharedSession(profile.SessionName, sceneBuildIndex);
    }
    
    public async UniTask JoinSharedSession(string sessionName)
    {
        EnsureRunnerExists();

        if (ServiceLocator.For(this).TryGet<ILoadingScreenService>(out var loadingScreen))
        {
            await loadingScreen.LoadSceneNetworkAsync(
                async () => await JoinGameAndFinalizeEntryAsync(sessionName));
        }
        else
        {
            await JoinGameAndFinalizeEntryAsync(sessionName);
        }

        AppLog.Info($"Joined Shared Session: {sessionName}");
    }

    public async UniTask JoinOpenWorldAsync(string openWorldSceneName)
    {
        EnsureRunnerExists();

        int sceneBuildIndex = ResolveSceneBuildIndex(openWorldSceneName);
        if (sceneBuildIndex == -1)
        {
            AppLog.Error($"Scene '{openWorldSceneName}' not found in Build Settings! Make sure to add it.");
            return;
        }

        var sceneRef = SceneRef.FromIndex(sceneBuildIndex);
        NetworkSessionProfile profile = ResolveOpenWorldProfile();

        if (ServiceLocator.For(this).TryGet<ILoadingScreenService>(out var loadingScreen))
        {
            await loadingScreen.LoadSceneNetworkAsync(
                async () => await JoinOrCreateOpenWorldAsync(sceneRef, sceneBuildIndex, profile));
        }
        else
        {
            AppLog.Warning("LoadingScreenManager not found, running without loading screen.");
            await JoinOrCreateOpenWorldAsync(sceneRef, sceneBuildIndex, profile);
        }

        AppLog.Info("Joined Open World session.");
    }

    public UniTask JoinOpenWorldAsync(NetworkSessionProfile profile)
    {
        if (profile == null)
            return JoinOpenWorldAsync(DefaultOpenWorldSceneName);

        return JoinOpenWorldAsync(profile.InitialSceneName);
    }

    /// <summary>
    /// Binds the runner used for the next session operation. Required for multi-peer travel
    /// where each peer owns its own <see cref="NetworkRunner"/>.
    /// </summary>
    public void UseRunner(NetworkRunner runner)
    {
        if (runner != null && runner.IsRunning)
            _runner = runner;
    }

    public async UniTask Disconnect()
    {
        if (_runner != null)
        {
            await _runner.Shutdown(); // Fusion destroys the runner's GameObject by default
            _runner = null;
            // Wait one frame so Unity finalises the Destroy before any new runner is created.
            // Without this, Fusion's single-peer check sees the old runner still in memory
            // and throws a "multipeer" error when StartGame is called again.
            await UniTask.NextFrame();
        }
    }

    #region Private Helpers

    private NetworkRunner ResolveRunner()
    {
        if (_runner != null && _runner.IsRunning)
            return _runner;

        return _runner;
    }

    private void EnsureRunnerExists()
    {
        EnsurePlayerSpawnCoordinator();
        ResolveRunner();

        if (_runner == null)
        {
            // Create the runner on its own GameObject so Fusion's default
            // destroyGameObject:true shutdown does not destroy this manager.
            var runnerGo = new GameObject("NetworkRunner");
            DontDestroyOnLoad(runnerGo);
            _runner = runnerGo.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            EnsureRunnerObjectProvider(runnerGo);
        }
    }

    private static void EnsureRunnerObjectProvider(GameObject runnerGo)
    {
        // Prefer pooled provider; remove bare default if a previous session attached it.
        NetworkObjectProviderDefault existingDefault = runnerGo.GetComponent<NetworkObjectProviderDefault>();
        if (existingDefault != null && existingDefault is not EnglishQuestNetworkObjectProvider)
            UnityEngine.Object.Destroy(existingDefault);

        EnglishQuestNetworkObjectProvider provider = runnerGo.GetComponent<EnglishQuestNetworkObjectProvider>();
        if (provider == null)
            provider = runnerGo.AddComponent<EnglishQuestNetworkObjectProvider>();

        provider.DelayIfSceneManagerIsBusy = true;
    }

    private void EnsurePlayerSpawnCoordinator()
    {
        if (GetComponent<PlayerSpawnCoordinator>() == null)
            gameObject.AddComponent<PlayerSpawnCoordinator>();
    }

    private async UniTask StartGameAndAwaitSceneLoadAsync(
        string sessionName,
        SceneRef sceneRef,
        int targetBuildIndex,
        int maxPlayers = DefaultOpenWorldMaxPlayers,
        bool enableClientSessionCreation = true)
    {
        int sceneLoadVersionBefore = PlayerSpawnCoordinator.SceneLoadDoneVersion;

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = sessionName,
            PlayerCount = Mathf.Clamp(maxPlayers, 1, DefaultOpenWorldMaxPlayers),
            Scene = BuildSingleSceneInfo(sceneRef),
            SceneManager = _sceneManager,
            EnableClientSessionCreation = enableClientSessionCreation
        });

        if (!result.Ok)
        {
            NetworkSessionErrorService.ReportStartGameFailure(result.ShutdownReason);
            throw new Exception($"Failed to start game: {result.ShutdownReason}");
        }

        BindRunnerCallbacks(_runner);
        await FinalizeGameplayEntryAsync(targetBuildIndex, sceneLoadVersionBefore);
    }

    private async UniTask JoinOrCreateOpenWorldAsync(
        SceneRef sceneRef,
        int targetBuildIndex,
        NetworkSessionProfile profile)
    {
        int sceneLoadVersionBefore = PlayerSpawnCoordinator.SceneLoadDoneVersion;

        string openWorldSessionName = WorldTravelSessionNaming.ResolveOpenWorldSessionName();
        StartGameArgs startArgs = profile != null
            ? profile.BuildStartGameArgs(BuildSingleSceneInfo(sceneRef), _sceneManager)
            : new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = openWorldSessionName,
                PlayerCount = DefaultOpenWorldMaxPlayers,
                Scene = BuildSingleSceneInfo(sceneRef),
                SceneManager = _sceneManager
            };
        startArgs.SessionName = openWorldSessionName;
        startArgs.PlayerCount = Mathf.Clamp(
            startArgs.PlayerCount.GetValueOrDefault(DefaultOpenWorldMaxPlayers),
            1,
            DefaultOpenWorldMaxPlayers);

        AppLog.Info($"[GameNetworkManager] Joining Open World shared room '{openWorldSessionName}'.");
        var result = await _runner.StartGame(startArgs);

        if (!result.Ok)
        {
            NetworkSessionErrorService.ReportStartGameFailure(result.ShutdownReason);
            throw new Exception($"Failed to join Open World: {result.ShutdownReason}");
        }

        BindRunnerCallbacks(_runner);
        await FinalizeGameplayEntryAsync(targetBuildIndex, sceneLoadVersionBefore);
    }

    private async UniTask JoinGameAndFinalizeEntryAsync(string sessionName)
    {
        int sceneVersionBefore = PlayerSpawnCoordinator.SceneLoadDoneVersion;
        await JoinGameAsync(sessionName);
        await FinalizeGameplayEntryAsync(-1, sceneVersionBefore);
    }

    private async UniTask JoinGameAsync(string sessionName)
    {
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = sessionName,
            SceneManager = _sceneManager,
            EnableClientSessionCreation = false
        });

        if (!result.Ok)
        {
            NetworkSessionErrorService.ReportStartGameFailure(result.ShutdownReason);
            throw new Exception($"Failed to join session: {result.ShutdownReason}");
        }

        BindRunnerCallbacks(_runner);

        // In Shared Mode the first player to connect becomes Master Client.
        // If we are Master Client after a join attempt, the session didn't exist – we created it by accident.
        if (_runner.IsSharedModeMasterClient)
        {
            await _runner.Shutdown();
            _runner = null;
            const string message = "Session not found. Check the session name and try again.";
            NetworkSessionErrorService.Report(message);
            throw new Exception($"Session '{sessionName}' does not exist.");
        }
    }

    private void BindRunnerCallbacks(NetworkRunner runner)
    {
        NetworkRunnerCallbackHub.BindRunner(runner);
        PlayerSpawnCoordinator.BindRunner(runner);
        NetworkSessionBridgeSpawner.TrySpawn(runner, _sessionBridgePrefab);
    }

    private NetworkSessionProfile ResolveOpenWorldProfile() => _openWorldProfile;

    private async UniTask FinalizeGameplayEntryAsync(int targetBuildIndex, int sceneLoadVersionBefore)
    {
        if (sceneLoadVersionBefore >= 0)
            await PlayerArrivalUtility.WaitForFusionSceneLoadDoneAsync(sceneLoadVersionBefore);
        else
            await WaitForSceneLoadAsync(targetBuildIndex);

        // Single peer: Fusion unloads Menu and applies level lighting (Development path).
        // Multi-Peer: scenes merge — unload Menu and restore captured RenderSettings manually.
        if (FusionPeerModeUtility.IsMultiplePeer)
            await AdoptFusionGameplaySceneAsync(targetBuildIndex);

        if (_runner == null || !_runner.IsRunning)
            return;

        PlayerSpawnCoordinator.EnsureLocalPlayerAfterSceneLoad(_runner);

        Scene gameplayScene = FindLoadedSceneByBuildIndex(targetBuildIndex);
        if (!gameplayScene.IsValid())
            gameplayScene = _runner.SimulationUnityScene;

        if (!gameplayScene.IsValid())
            return;

        LocalPlayerReadyArgs ready = default;
        if (LocalPlayerReadiness.TryGet(_runner, gameplayScene, out ILocalPlayerReadiness readiness))
            ready = await readiness.WaitReadyAsync();
        if (!ready.IsValid)
            AppLog.Error("[GameNetworkManager] Local player was not ready after menu gameplay entry.");
    }

    private static int ResolveSceneBuildIndex(string sceneNameOrPath)
    {
        int buildIndex = SceneUtility.GetBuildIndexByScenePath(sceneNameOrPath);
        if (buildIndex >= 0)
            return buildIndex;

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(scenePath))
                continue;

            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneName == sceneNameOrPath)
                return i;
        }

        return -1;
    }

    private async UniTask WaitForSceneLoadAsync(int targetBuildIndex)
    {
        bool loaded = await SceneLoadWaitUtility.WaitForSceneActiveAsync(targetBuildIndex);
        if (!loaded)
            AppLog.Error($"[GameNetworkManager] Timed out waiting for scene build index {targetBuildIndex}.");
    }

    private static NetworkSceneInfo BuildSingleSceneInfo(SceneRef sceneRef)
    {
        var sceneInfo = new NetworkSceneInfo();
        sceneInfo.AddSceneRef(sceneRef, LoadSceneMode.Single);
        return sceneInfo;
    }

    private async UniTask AdoptFusionGameplaySceneAsync(int preferredBuildIndex = -1)
    {
        Scene gameplayScene = FindLoadedSceneByBuildIndex(preferredBuildIndex);
        if (!gameplayScene.IsValid())
            gameplayScene = _runner.SimulationUnityScene;

        if (!gameplayScene.IsValid() || !gameplayScene.isLoaded)
            return;

        await UnloadScenesExceptAsync(gameplayScene);

        if (SceneManager.GetActiveScene() != gameplayScene)
            SceneManager.SetActiveScene(gameplayScene);

        _sceneManager?.ReapplyCapturedEnvironment();
    }

    private static Scene FindLoadedSceneByBuildIndex(int buildIndex)
    {
        if (buildIndex < 0)
            return default;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && scene.buildIndex == buildIndex)
                return scene;
        }

        return default;
    }

    private static async UniTask UnloadScenesExceptAsync(Scene keepScene)
    {
        if (!keepScene.IsValid())
            return;

        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || scene.handle == keepScene.handle)
                continue;

            if (scene.name == "DontDestroyOnLoad")
                continue;

            await SceneManager.UnloadSceneAsync(scene);
        }
    }

    #endregion
}

