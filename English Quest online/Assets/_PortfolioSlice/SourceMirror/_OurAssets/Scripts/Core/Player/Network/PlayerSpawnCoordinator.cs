using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityServiceLocator;

/// <summary>
/// DDOL player spawn authority. Scene-local <see cref="PlayerSceneContext"/> supplies prefab and camera;
/// this coordinator runs a single pipeline after Fusion scene loads.
/// Supports multiple local <see cref="NetworkRunner"/> instances in Fusion Multi-Peer editor mode.
/// </summary>
public class PlayerSpawnCoordinator : MonoBehaviour
{
    private sealed class RunnerSpawnState
    {
        public Coroutine PipelineCoroutine;
        public Coroutine RepositionCoroutine;
        public int LastProcessedSceneHandle = -1;
    }

    [SerializeField] private NetworkPrefabRef _defaultPlayerPrefab;

    private static PlayerSpawnCoordinator s_instance;

    private readonly HashSet<NetworkRunner> _registeredRunners = new();
    private readonly Dictionary<NetworkRunner, RunnerSpawnState> _runnerStates = new();
    private readonly HashSet<(NetworkRunner Runner, PlayerRef Player)> _pendingSpawns = new();
    private int _sceneLoadDoneVersion;

    public static int SceneLoadDoneVersion => s_instance != null ? s_instance._sceneLoadDoneVersion : 0;

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            bool thisIsPersistent = IsPersistentHost(gameObject);
            bool existingIsPersistent = IsPersistentHost(s_instance.gameObject);

            // Production DDOL coordinator must win over transient scene prototype copies.
            if (thisIsPersistent && !existingIsPersistent)
            {
                s_instance.UnsubscribeFromCallbackHub();
                Destroy(s_instance);
                s_instance = this;
                RegisterPlayerPrefab(_defaultPlayerPrefab);
                SubscribeToCallbackHub();
                return;
            }

            Destroy(this);
            return;
        }

        s_instance = this;
        RegisterPlayerPrefab(_defaultPlayerPrefab);
        SubscribeToCallbackHub();
    }

    private static bool IsPersistentHost(GameObject host) =>
        host != null && host.scene.name == "DontDestroyOnLoad";

    private void OnDestroy()
    {
        UnsubscribeFromCallbackHub();
        UnregisterAllRunners();

        if (s_instance == this)
            s_instance = null;
    }

    private void SubscribeToCallbackHub()
    {
        NetworkRunnerCallbackHub.Instance.SceneLoadStart += HandleSceneLoadStart;
        NetworkRunnerCallbackHub.Instance.SceneLoadDone += HandleSceneLoadDone;
        NetworkRunnerCallbackHub.Instance.PlayerJoined += HandlePlayerJoined;
        NetworkRunnerCallbackHub.Instance.Shutdown += HandleShutdown;
    }

    private void UnsubscribeFromCallbackHub()
    {
        NetworkRunnerCallbackHub.Instance.SceneLoadStart -= HandleSceneLoadStart;
        NetworkRunnerCallbackHub.Instance.SceneLoadDone -= HandleSceneLoadDone;
        NetworkRunnerCallbackHub.Instance.PlayerJoined -= HandlePlayerJoined;
        NetworkRunnerCallbackHub.Instance.Shutdown -= HandleShutdown;
    }

    private void Update()
    {
        SyncRegisteredRunners();
    }

    public static void RegisterPlayerPrefab(NetworkPrefabRef prefab)
    {
        PlayerPrefabResolver.Register(prefab);
    }

    public static void BindRunner(NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning || s_instance == null)
            return;

        s_instance.RegisterRunner(runner);
    }

    public static void EnsureLocalPlayerAfterSceneLoad(NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning || s_instance == null)
            return;

        s_instance.StartSceneLoadPipeline(runner);
    }

    private void SyncRegisteredRunners()
    {
        if (s_instance != this)
            return;

        var activeRunners = new HashSet<NetworkRunner>();

        ServiceLocator locator = ServiceLocator.For(this);
        if (locator != null && locator.TryGet<INetworkSessionService>(out var netSession))
        {
            NetworkRunner serviceRunner = netSession.Runner;
            if (serviceRunner != null && serviceRunner.IsRunning)
                activeRunners.Add(serviceRunner);
        }

#if UNITY_EDITOR
        // Fusion Multi-Peer creates several local runners; all of them need spawn callbacks.
        foreach (NetworkRunner instance in NetworkRunner.Instances)
        {
            if (instance != null && instance.IsRunning)
                activeRunners.Add(instance);
        }
#endif

        if (activeRunners.Count == 0)
        {
            foreach (NetworkRunner instance in NetworkRunner.Instances)
            {
                if (instance != null && instance.IsRunning)
                    activeRunners.Add(instance);
            }
        }

        foreach (NetworkRunner runner in activeRunners)
            RegisterRunner(runner);

        var stoppedRunners = new List<NetworkRunner>();
        foreach (NetworkRunner runner in _registeredRunners)
        {
            if (runner == null || !runner.IsRunning)
                stoppedRunners.Add(runner);
        }

        foreach (NetworkRunner runner in stoppedRunners)
            UnregisterRunner(runner);
    }

    private void RegisterRunner(NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning || !_registeredRunners.Add(runner))
            return;

        TryStartPipelineIfNeeded(runner);
    }

    private void TryStartPipelineIfNeeded(NetworkRunner runner)
    {
        if (!IsRunnerSceneReady(runner))
            return;

        if (HasCompletedPipelineForCurrentScene(runner))
            return;

        StartSceneLoadPipeline(runner);
    }

    private static bool HasCompletedPipelineForCurrentScene(NetworkRunner runner)
    {
        if (s_instance == null)
            return false;

        Scene runnerScene = ResolveRunnerScene(runner);
        if (!runnerScene.IsValid())
            return false;

        if (!s_instance._runnerStates.TryGetValue(runner, out RunnerSpawnState state))
            return false;

        if (state.LastProcessedSceneHandle != runnerScene.handle)
            return false;

        if (runner.GameMode != GameMode.Shared)
            return false;

        NetworkObject localPlayer = runner.GetPlayerObject(runner.LocalPlayer);
        return PlayerArrivalUtility.IsUsablePlayerObject(localPlayer, runnerScene);
    }

    private static Scene ResolveRunnerScene(NetworkRunner runner)
    {
        Scene simulationScene = runner.SimulationUnityScene;
        if (simulationScene.IsValid() && simulationScene.isLoaded)
            return simulationScene;

        return SceneManager.GetActiveScene();
    }

    private static bool IsRunnerSceneReady(NetworkRunner runner)
    {
        Scene scene = runner.SimulationUnityScene;
        return scene.IsValid() && scene.isLoaded;
    }

    private void UnregisterRunner(NetworkRunner runner)
    {
        if (runner == null || !_registeredRunners.Remove(runner))
            return;

        StopRunnerCoroutines(runner);

        var completedSpawns = new List<(NetworkRunner Runner, PlayerRef Player)>();
        foreach ((NetworkRunner pendingRunner, PlayerRef pendingPlayer) in _pendingSpawns)
        {
            if (pendingRunner == runner)
                completedSpawns.Add((pendingRunner, pendingPlayer));
        }

        foreach ((NetworkRunner pendingRunner, PlayerRef pendingPlayer) in completedSpawns)
            _pendingSpawns.Remove((pendingRunner, pendingPlayer));

        _runnerStates.Remove(runner);
    }

    private void UnregisterAllRunners()
    {
        foreach (NetworkRunner runner in new List<NetworkRunner>(_runnerStates.Keys))
            StopRunnerCoroutines(runner);

        _registeredRunners.Clear();
        _pendingSpawns.Clear();
        _runnerStates.Clear();
    }

    private RunnerSpawnState GetOrCreateState(NetworkRunner runner)
    {
        if (!_runnerStates.TryGetValue(runner, out RunnerSpawnState state))
        {
            state = new RunnerSpawnState();
            _runnerStates[runner] = state;
        }

        return state;
    }

    private void StopRunnerCoroutines(NetworkRunner runner)
    {
        if (!_runnerStates.TryGetValue(runner, out RunnerSpawnState state))
            return;

        if (state.PipelineCoroutine != null)
        {
            StopCoroutine(state.PipelineCoroutine);
            state.PipelineCoroutine = null;
        }

        if (state.RepositionCoroutine != null)
        {
            StopCoroutine(state.RepositionCoroutine);
            state.RepositionCoroutine = null;
        }
    }

    private void StartSceneLoadPipeline(NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning)
            return;

        RunnerSpawnState state = GetOrCreateState(runner);

        // Never stack pipelines — a second trigger while one is running can duplicate SpawnAsync calls.
        if (state.PipelineCoroutine != null)
            return;

        state.PipelineCoroutine = StartCoroutine(SceneLoadPipelineRoutine(runner));
    }

    private void HandleSceneLoadStart(NetworkRunner runner)
    {
        LocalPlayerReadiness.Clear(runner);
        ClearPendingSpawns(runner);
        StopRunnerCoroutines(runner);

        if (_runnerStates.TryGetValue(runner, out RunnerSpawnState state))
            state.LastProcessedSceneHandle = -1;
    }

    private void HandleSceneLoadDone(NetworkRunner runner)
    {
        _sceneLoadDoneVersion++;
        LocalPlayerReadiness.Clear(runner);

        Scene runnerScene = ResolveRunnerScene(runner);
        HarnessTravelSpawnUtility.TryEnsureDestinationSpawn(runner, runnerScene);
        PlayerSceneCamera.ConfigureRunnerChannelIsolation(runnerScene, runner);
        ServiceLocator.RefreshForScene(runnerScene);
        PlayerSceneContext.RefreshForScene(runnerScene);

        if (HasCompletedPipelineForCurrentScene(runner))
            return;

        AppLog.Info("[PlayerSpawnCoordinator] Fusion scene load done. Starting local player pipeline.");
        StartSceneLoadPipeline(runner);
    }

    private void HandlePlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        bool shouldHandle = runner.GameMode == GameMode.Shared
            ? player == runner.LocalPlayer
            : runner.IsServer;

        if (!shouldHandle)
            return;

        if (runner.GetPlayerObject(player) != null)
            return;

        // Initial scene load is handled exclusively by OnSceneLoadDone.
        if (_sceneLoadDoneVersion == 0)
            return;

        RunnerSpawnState state = GetOrCreateState(runner);
        if (state.PipelineCoroutine != null)
            return;

        StartCoroutine(EnsurePlayerInSceneRoutine(runner, player, ResolveRunnerScene(runner)));
    }

    private void ClearPendingSpawns(NetworkRunner runner)
    {
        var completedSpawns = new List<(NetworkRunner Runner, PlayerRef Player)>();
        foreach ((NetworkRunner pendingRunner, PlayerRef pendingPlayer) in _pendingSpawns)
        {
            if (pendingRunner == runner)
                completedSpawns.Add((pendingRunner, pendingPlayer));
        }

        foreach ((NetworkRunner pendingRunner, PlayerRef pendingPlayer) in completedSpawns)
            _pendingSpawns.Remove((pendingRunner, pendingPlayer));
    }

    private IEnumerator SceneLoadPipelineRoutine(NetworkRunner runner)
    {
        yield return null;
        yield return WaitForSceneManagerIdleRoutine(runner);

        Scene runnerScene = ResolveRunnerScene(runner);
        yield return WaitForSpawnPointsRoutine(runnerScene);

        if (runner.GameMode == GameMode.Shared)
        {
            yield return EnsurePlayerInSceneRoutine(runner, runner.LocalPlayer, runnerScene);
        }
        else if (runner.IsServer)
        {
            foreach (PlayerRef player in runner.ActivePlayers)
                yield return EnsurePlayerInSceneRoutine(runner, player, runnerScene);
        }

        if (_runnerStates.TryGetValue(runner, out RunnerSpawnState state))
        {
            state.PipelineCoroutine = null;
            state.LastProcessedSceneHandle = runnerScene.handle;
        }
    }

    private static IEnumerator WaitForSpawnPointsRoutine(Scene scene)
    {
        const int maxFrames = 120;
        for (int frame = 0; frame < maxFrames; frame++)
        {
            if (PlayerSpawnPoint.HasSpawnPointsInScene(scene))
                yield break;

            yield return null;
        }

        AppLog.Warning($"[PlayerSpawnCoordinator] Timed out waiting for spawn points in scene '{scene.name}'.");
    }

    private static IEnumerator WaitForSceneManagerIdleRoutine(NetworkRunner runner, float timeoutSeconds = 5f)
    {
        if (runner == null || !runner.IsRunning || runner.SceneManager == null)
            yield break;

        float elapsed = 0f;
        while (runner.SceneManager.IsBusy)
        {
            if (elapsed >= timeoutSeconds)
            {
                AppLog.Warning(
                    $"[PlayerSpawnCoordinator] Timed out waiting for scene manager on '{runner.name}'.");
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator EnsurePlayerInSceneRoutine(NetworkRunner runner, PlayerRef player, Scene scene)
    {
        NetworkPrefabRef prefab = PlayerPrefabResolver.Resolve(scene, runner);
        if (!prefab.IsValid)
        {
            AppLog.Error("[PlayerSpawnCoordinator] Player prefab not set.");
            yield break;
        }

        if (runner.GameMode == GameMode.Shared && player != runner.LocalPlayer)
            yield break;

        PlayerArrivalUtility.ClearStalePlayerObject(runner, player, scene);

        NetworkObject existingObject = runner.GetPlayerObject(player);
        if (PlayerArrivalUtility.IsUsablePlayerObject(existingObject, scene))
        {
            AppLog.Info("[PlayerSpawnCoordinator] Existing player in scene. Repositioning for arrival.");
            yield return RepositionExistingPlayerRoutine(runner, existingObject, scene);

            if (player == runner.LocalPlayer && PlayerArrivalUtility.IsUsablePlayerObject(existingObject, scene))
                BindCameraAndNotifyReady(runner, existingObject, scene, existingObject.transform.rotation);

            yield break;
        }

        if (!PlayerArrivalUtility.TryResolveSpawnTransform(runner, scene, consumePendingTravel: true, out Vector3 spawnPos, out Quaternion spawnRot))
        {
            AppLog.Warning("[PlayerSpawnCoordinator] No spawn point found. Spawning at origin.");
            spawnPos = Vector3.zero;
            spawnRot = Quaternion.identity;
        }

        AppLog.Info($"[PlayerSpawnCoordinator] Spawning player {player} at {spawnPos} facing {spawnRot.eulerAngles}");

        var spawnKey = (runner, player);
        if (_pendingSpawns.Contains(spawnKey))
            yield break;

        // Re-check after yields — another pipeline trigger may have completed the spawn.
        existingObject = runner.GetPlayerObject(player);
        if (PlayerArrivalUtility.IsUsablePlayerObject(existingObject, scene))
        {
            if (player == runner.LocalPlayer)
                BindCameraAndNotifyReady(runner, existingObject, scene, existingObject.transform.rotation);

            yield break;
        }

        NetworkSpawnStatus status = runner.TrySpawn(
            prefab,
            out NetworkObject playerObject,
            spawnPos,
            spawnRot,
            player);

        if (status == NetworkSpawnStatus.Spawned &&
            PlayerArrivalUtility.IsUsablePlayerObject(playerObject, scene))
        {
            CompletePlayerSpawn(runner, player, playerObject, scene, spawnRot);
            yield break;
        }

        bool spawnCompleted = false;
        _pendingSpawns.Add(spawnKey);
        runner.SpawnAsync(
            prefab,
            spawnPos,
            spawnRot,
            player,
            onBeforeSpawned: null,
            flags: default,
            onCompleted: spawnOp =>
            {
                _pendingSpawns.Remove(spawnKey);

                NetworkRunner spawnRunner = spawnOp.Runner;
                if (spawnRunner == null || !spawnRunner.IsRunning)
                {
                    spawnCompleted = true;
                    return;
                }

                if (!spawnOp.IsSpawned)
                {
                    AppLog.Error("[PlayerSpawnCoordinator] Failed to spawn player after scene load.");
                    spawnCompleted = true;
                    return;
                }

                NetworkObject spawnedObject = spawnOp.Object;
                if (!PlayerArrivalUtility.IsUsablePlayerObject(spawnedObject, scene))
                {
                    AppLog.Error("[PlayerSpawnCoordinator] Failed to spawn player after scene load.");
                    spawnCompleted = true;
                    return;
                }

                CompletePlayerSpawn(spawnRunner, player, spawnedObject, scene, spawnRot);
                spawnCompleted = true;
            });

        while (!spawnCompleted)
            yield return null;
    }

    private void CompletePlayerSpawn(
        NetworkRunner runner,
        PlayerRef player,
        NetworkObject playerObject,
        Scene scene,
        Quaternion spawnRot)
    {
        runner.SetPlayerObject(player, playerObject);

        if (player == runner.LocalPlayer)
            BindCameraAndNotifyReady(runner, playerObject, scene, spawnRot);
    }

    private void BindCameraAndNotifyReady(
        NetworkRunner runner,
        NetworkObject playerObject,
        Scene scene,
        Quaternion facing)
    {
        PlayerSceneCamera.ConfigureRunnerChannelIsolation(scene, runner);

        CinemachineCamera followCamera = PlayerSceneContext.ResolveFollowCamera(scene);
        PlayerSceneCamera.AssignFollow(followCamera, playerObject.transform, facing);

        if (playerObject.HasInputAuthority)
        {
            Scene lifecycleScene = ResolveLifecycleScene(runner, scene);

            SocialSystemsBootstrap.EnsureForLocalPlayer(runner, lifecycleScene, playerObject);

            if (!LocalPlayerReadiness.TryPublishReady(new LocalPlayerReadyArgs(
                    runner,
                    playerObject,
                    lifecycleScene,
                    followCamera)))
            {
                AppLog.Warning(
                    $"[PlayerSpawnCoordinator] Failed to publish local player readiness for scene '{lifecycleScene.name}'.");
            }
        }
    }

    private static Scene ResolveLifecycleScene(NetworkRunner runner, Scene fallbackScene)
    {
        Scene simulationScene = runner.SimulationUnityScene;
        return simulationScene.IsValid() && simulationScene.isLoaded
            ? simulationScene
            : fallbackScene;
    }

    private IEnumerator RepositionExistingPlayerRoutine(NetworkRunner runner, NetworkObject playerObject, Scene scene)
    {
        RunnerSpawnState state = GetOrCreateState(runner);

        if (state.RepositionCoroutine != null)
            StopCoroutine(state.RepositionCoroutine);

        bool completed = false;
        state.RepositionCoroutine = StartCoroutine(
            RepositionExistingPlayerCoreRoutine(runner, playerObject, scene, () => completed = true));

        while (!completed)
            yield return null;

        state.RepositionCoroutine = null;
    }

    private IEnumerator RepositionExistingPlayerCoreRoutine(
        NetworkRunner runner,
        NetworkObject playerObject,
        Scene scene,
        Action onComplete)
    {
        yield return null;

        const int maxFrames = 120;
        for (int frame = 0; frame < maxFrames; frame++)
        {
            if (!PlayerArrivalUtility.IsUsablePlayerObject(playerObject, scene))
            {
                onComplete?.Invoke();
                yield break;
            }

            if (PlayerSpawnPoint.HasSpawnPointsInScene(scene))
                break;

            yield return null;
        }

        if (!PlayerArrivalUtility.IsUsablePlayerObject(playerObject, scene))
        {
            onComplete?.Invoke();
            yield break;
        }

        if (PlayerTravelArrival.TryConsumeForScene(runner, scene, out PlayerTravelArrival.PendingArrival travelArrival))
        {
            PlayerArrivalUtility.TryApplyArrival(playerObject, scene, travelArrival);
            onComplete?.Invoke();
            yield break;
        }

        if (PlayerArrivalUtility.TryApplyDefaultSceneSpawn(playerObject, scene))
        {
            onComplete?.Invoke();
            yield break;
        }

        AppLog.Warning("[PlayerSpawnCoordinator] Could not reposition existing player after scene load.");
        onComplete?.Invoke();
    }

    private void HandleShutdown(NetworkRunner runner, ShutdownReason shutdownReason) => UnregisterRunner(runner);
}
