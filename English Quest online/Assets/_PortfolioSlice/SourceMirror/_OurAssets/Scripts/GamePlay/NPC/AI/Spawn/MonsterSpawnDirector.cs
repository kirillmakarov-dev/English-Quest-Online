using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityServiceLocator;

/// <summary>
/// Scene director for MapleStory-style spawn points: population caps, weighted pools, and respawn timers.
/// Open-world best practice: proximity activation, global live cap, far despawn, and spread spawning.
/// Only the Fusion shared-mode master client performs networked spawns.
/// </summary>
[DefaultExecutionOrder(-100)]
public class MonsterSpawnDirector : MonoBehaviour, IMonsterSpawnService
{
    private const int SpawnsPerFrame = 2;
    private const int OfflineSettleFrames = 30;
    private const float SceneManagerIdleTimeoutSeconds = 10f;
    private const int NetworkReadyMaxAttempts = 80;
    private const float ActivationTickSeconds = 0.5f;

    [SerializeField] private float _activateRadius = 50f;
    [SerializeField] private float _deactivateGraceSeconds = 20f;
    [SerializeField] private int _globalMaxLiveMonsters = 40;

    private sealed class PointState
    {
        public int LiveCount;
        public int PendingRespawns;
        public bool IsActive;
        public float FarTimer;
        public int ActivationEpoch;
        public readonly List<GameObject> LiveInstances = new();
    }

    private readonly Dictionary<MonsterSpawnPoint, PointState> _points = new();
    private readonly List<Vector3> _playerPositions = new();
    private readonly List<KeyValuePair<MonsterSpawnPoint, PointState>> _activationScratch = new();
    private readonly List<MonsterSpawnPoint> _fillScratch = new();
    private readonly System.Random _rng = new();
    private CancellationTokenSource _destroyCts;
    private bool _directorLoopStarted;
    private bool _directorLoopRunning;
    private int _cachedLiveCount;

    /// <summary>True after the proximity director loop has started (or been skipped for non-master).</summary>
    public bool HasCompletedInitialPopulation => _directorLoopStarted;

    /// <summary>Tracked spawn points (edit-mode / tests).</summary>
    public int RegisteredPointCount => _points.Count;

    public float ActivateRadius => Mathf.Max(0f, _activateRadius);
    public float DeactivateGraceSeconds => Mathf.Max(0f, _deactivateGraceSeconds);
    public int GlobalMaxLiveMonsters => Mathf.Max(1, _globalMaxLiveMonsters);

    public int GetTotalLiveCount() => Mathf.Max(0, _cachedLiveCount);

    private void RecalculateCachedLiveCount()
    {
        int total = 0;
        foreach (KeyValuePair<MonsterSpawnPoint, PointState> pair in _points)
            total += Mathf.Max(0, pair.Value.LiveCount);
        _cachedLiveCount = total;
    }

    private void Awake()
    {
        _destroyCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        ServiceLocator.For(this).Register<IMonsterSpawnService>(this);
    }

    private void Start()
    {
        Scene scene = gameObject.scene;
        IReadOnlyList<MonsterSpawnPoint> points = MonsterSpawnPoint.GetPointsInScene(scene);
        for (int i = 0; i < points.Count; i++)
            RegisterPoint(points[i]);

        RunDirectorWhenReadyAsync().Forget();
    }

    private void OnDestroy()
    {
        ServiceLocator.DeregisterFor<IMonsterSpawnService>(this);
        _destroyCts?.Cancel();
        _destroyCts?.Dispose();
        _destroyCts = null;
    }

    public void RegisterPoint(MonsterSpawnPoint point)
    {
        if (point == null || !CanSpawn(point))
            return;

        if (!_points.ContainsKey(point))
            _points[point] = new PointState();

        // Registration never spawns synchronously. The proximity loop fills active points.
    }

    public void UnregisterPoint(MonsterSpawnPoint point)
    {
        if (point == null)
            return;

        if (_points.TryGetValue(point, out PointState state))
            _cachedLiveCount = Mathf.Max(0, _cachedLiveCount - Mathf.Max(0, state.LiveCount));

        _points.Remove(point);
    }

    public void NotifyMobDied(MonsterSpawnPoint point, MonsterSpawnInstance instance)
    {
        if (point == null || !_points.TryGetValue(point, out PointState state))
            return;

        if (instance != null)
            state.LiveInstances.Remove(instance.gameObject);

        if (state.LiveCount > 0)
        {
            state.LiveCount--;
            _cachedLiveCount = Mathf.Max(0, _cachedLiveCount - 1);
        }

        ScheduleRespawnAsync(point, state, point.RespawnDelaySeconds, state.ActivationEpoch).Forget();
    }

    private async UniTaskVoid RunDirectorWhenReadyAsync()
    {
        CancellationToken token = _destroyCts.Token;

        try
        {
            bool shouldRun = await WaitUntilReadyToPopulateAsync(token);
            if (!shouldRun || _directorLoopRunning)
                return;

            _directorLoopRunning = true;
            _directorLoopStarted = true;

            if (IsAnyFusionRunnerRunning())
                CleanupLocalSpawnsBeforeNetworkFill();

            await DirectorLoopAsync(token);
        }
        catch (OperationCanceledException)
        {
            // Director destroyed during wait/tick.
        }
        finally
        {
            _directorLoopRunning = false;
        }
    }

    private async UniTask DirectorLoopAsync(CancellationToken token)
    {
        float activationTimer = 0f;

        while (!token.IsCancellationRequested)
        {
            activationTimer += Time.unscaledDeltaTime;
            if (activationTimer >= ActivationTickSeconds)
            {
                activationTimer = 0f;
                CollectPlayerPositions();
                UpdateActivationAndDespawnFar();
            }

            SpreadFillActivePointsFrame();
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }

    private async UniTask<bool> WaitUntilReadyToPopulateAsync(CancellationToken token)
    {
        for (int attempt = 0; attempt < NetworkReadyMaxAttempts; attempt++)
        {
            if (MonsterSpawnRunnerResolver.TryGetSpawnRunner(this, out NetworkRunner runner))
            {
                if (!runner.IsSharedModeMasterClient)
                {
                    _directorLoopStarted = true;
                    return false;
                }

                await WaitForSceneManagerIdleAsync(runner, token);
                return true;
            }

            if (IsAnyFusionRunnerRunning())
            {
                await UniTask.Delay(250, cancellationToken: token);
                continue;
            }

            if (attempt >= OfflineSettleFrames)
                return true;

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        if (IsAnyFusionRunnerRunning())
        {
            AppLog.Warning(
                "[MonsterSpawnDirector] Timed out waiting for a spawn runner bound to this scene. Skipping director loop.",
                this);
            _directorLoopStarted = true;
            return false;
        }

        return true;
    }

    private static async UniTask WaitForSceneManagerIdleAsync(NetworkRunner runner, CancellationToken token)
    {
        if (runner == null || !runner.IsRunning || runner.SceneManager == null)
            return;

        float elapsed = 0f;
        while (runner.SceneManager.IsBusy)
        {
            if (elapsed >= SceneManagerIdleTimeoutSeconds)
            {
                AppLog.Warning(
                    "[MonsterSpawnDirector] Timed out waiting for Fusion scene manager idle; starting proximity loop anyway.");
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        await UniTask.Yield(PlayerLoopTiming.Update, token);
    }

    private void CollectPlayerPositions()
    {
        _playerPositions.Clear();

        if (MonsterSpawnRunnerResolver.TryGetSpawnRunner(this, out NetworkRunner runner) && runner.IsRunning)
        {
            foreach (PlayerRef player in runner.ActivePlayers)
            {
                NetworkObject playerObject = runner.GetPlayerObject(player);
                if (playerObject == null || !playerObject.IsValid)
                    continue;

                _playerPositions.Add(playerObject.transform.position);
            }

            return;
        }

        if (IsAnyFusionRunnerRunning())
            return;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            _playerPositions.Add(mainCamera.transform.position);
    }

    private void UpdateActivationAndDespawnFar()
    {
        float activateRadius = ActivateRadius;
        float grace = DeactivateGraceSeconds;
        _activationScratch.Clear();
        foreach (KeyValuePair<MonsterSpawnPoint, PointState> pair in _points)
            _activationScratch.Add(pair);

        for (int i = 0; i < _activationScratch.Count; i++)
        {
            MonsterSpawnPoint point = _activationScratch[i].Key;
            PointState state = _activationScratch[i].Value;
            if (point == null)
                continue;

            bool near = MonsterSpawnProximity.IsNearAnyPlayer(point.transform.position, _playerPositions, activateRadius);
            if (near)
            {
                if (!state.IsActive)
                {
                    state.IsActive = true;
                    state.ActivationEpoch++;
                }

                state.FarTimer = 0f;
                continue;
            }

            if (!state.IsActive)
                continue;

            state.FarTimer += ActivationTickSeconds;
            if (state.FarTimer < grace)
                continue;

            state.IsActive = false;
            state.FarTimer = 0f;
            state.ActivationEpoch++;
            DespawnPointPopulation(point, state);
        }
    }

    private void SpreadFillActivePointsFrame()
    {
        int budget = SpawnsPerFrame;
        if (budget <= 0 || GetTotalLiveCount() >= GlobalMaxLiveMonsters)
            return;

        _fillScratch.Clear();
        foreach (MonsterSpawnPoint point in _points.Keys)
            _fillScratch.Add(point);

        for (int i = 0; i < _fillScratch.Count && budget > 0; i++)
        {
            MonsterSpawnPoint point = _fillScratch[i];
            if (point == null || !point.SpawnOnEnable || !_points.TryGetValue(point, out PointState state))
                continue;

            if (!state.IsActive || !NeedsSpawn(point, state))
                continue;

            if (GetTotalLiveCount() >= GlobalMaxLiveMonsters)
                break;

            int before = state.LiveCount;
            TrySpawnOne(point, state);
            if (state.LiveCount > before)
                budget--;
        }
    }

    private void DespawnPointPopulation(MonsterSpawnPoint point, PointState state)
    {
        bool hasRunner = MonsterSpawnRunnerResolver.TryGetSpawnRunner(this, out NetworkRunner runner)
                         && runner != null
                         && runner.IsRunning;

        for (int i = state.LiveInstances.Count - 1; i >= 0; i--)
        {
            GameObject liveObject = state.LiveInstances[i];
            state.LiveInstances.RemoveAt(i);
            if (liveObject == null)
                continue;

            if (hasRunner && liveObject.TryGetComponent(out NetworkObject networkObject) && networkObject.IsValid)
            {
                runner.Despawn(networkObject);
                continue;
            }

            Destroy(liveObject);
        }

        _cachedLiveCount = Mathf.Max(0, _cachedLiveCount - Mathf.Max(0, state.LiveCount));
        state.LiveCount = 0;
        state.PendingRespawns = 0;
    }

    private bool NeedsSpawn(MonsterSpawnPoint point, PointState state)
    {
        if (!CanSpawn(point) || !state.IsActive)
            return false;

        return state.LiveCount + state.PendingRespawns < point.MaxPopulation;
    }

    private void TrySpawnOne(MonsterSpawnPoint point, PointState state)
    {
        if (!CanSpawn(point) || !state.IsActive)
            return;

        if (GetTotalLiveCount() >= GlobalMaxLiveMonsters)
            return;

        MonsterDefinitionSO definition = point.Pool?.Select(_rng);
        if (definition == null)
        {
            AppLog.Error($"[MonsterSpawnDirector] Spawn pool on '{point.name}' returned no monster.", point);
            return;
        }

        Vector3 position = point.GetSpawnPosition();
        Quaternion rotation = point.GetSpawnRotation();
        bool useNetworkSpawn = MonsterSpawnRunnerResolver.TryGetSpawnRunner(this, out NetworkRunner runner);

        GameObject spawnedObject = null;
        bool isLocalSpawn = false;

        if (useNetworkSpawn)
        {
            if (!runner.IsSharedModeMasterClient)
                return;

            NetworkObject networkObject = MonsterSpawnUtility.SpawnNetworked(runner, definition, position, rotation);
            if (networkObject == null)
            {
                AppLog.Error(
                    $"[MonsterSpawnDirector] Failed to spawn networked '{definition.DisplayName}' at '{point.name}'. " +
                    "Ensure the monster prefab is registered with Fusion.",
                    point);
                return;
            }

            spawnedObject = networkObject.gameObject;
        }
        else
        {
            if (IsAnyFusionRunnerRunning())
                return;

            spawnedObject = MonsterSpawnUtility.SpawnLocal(definition, position, rotation);
            isLocalSpawn = true;
        }

        if (spawnedObject == null)
            return;

        MonsterSpawnInstance instance = spawnedObject.GetComponent<MonsterSpawnInstance>();
        if (instance == null)
            instance = spawnedObject.AddComponent<MonsterSpawnInstance>();

        instance.Initialize(point, definition, isLocalSpawn);
        state.LiveInstances.Add(spawnedObject);
        state.LiveCount++;
        _cachedLiveCount++;
    }

    private async UniTaskVoid ScheduleRespawnAsync(
        MonsterSpawnPoint point,
        PointState state,
        float delaySeconds,
        int epochAtSchedule)
    {
        CancellationToken token = _destroyCts.Token;
        state.PendingRespawns++;

        try
        {
            if (delaySeconds > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: token);

            if (point == null || !point.isActiveAndEnabled || !_points.ContainsKey(point))
                return;

            state.PendingRespawns = Mathf.Max(0, state.PendingRespawns - 1);

            if (state.ActivationEpoch != epochAtSchedule || !state.IsActive)
                return;

            if (state.LiveCount >= point.MaxPopulation || GetTotalLiveCount() >= GlobalMaxLiveMonsters)
                return;

            TrySpawnOne(point, state);
        }
        catch (OperationCanceledException)
        {
            if (state != null)
                state.PendingRespawns = Mathf.Max(0, state.PendingRespawns - 1);
        }
    }

    private void CleanupLocalSpawnsBeforeNetworkFill()
    {
        foreach (KeyValuePair<MonsterSpawnPoint, PointState> pair in _points)
        {
            PointState state = pair.Value;

            for (int i = state.LiveInstances.Count - 1; i >= 0; i--)
            {
                GameObject liveObject = state.LiveInstances[i];
                if (liveObject == null)
                {
                    state.LiveInstances.RemoveAt(i);
                    if (state.LiveCount > 0)
                        state.LiveCount--;

                    continue;
                }

                MonsterSpawnInstance instance = liveObject.GetComponent<MonsterSpawnInstance>();
                if (instance == null || !instance.IsOrphanedLocalSpawn())
                    continue;

                state.LiveInstances.RemoveAt(i);
                if (state.LiveCount > 0)
                    state.LiveCount--;

                Destroy(liveObject);
            }
        }

        RecalculateCachedLiveCount();
    }

    private static bool CanSpawn(MonsterSpawnPoint point)
    {
        if (point == null || point.Pool == null)
        {
            if (point != null)
                AppLog.Error($"[MonsterSpawnDirector] Spawn point '{point.name}' has no pool assigned.", point);

            return false;
        }

        return true;
    }

    private static bool IsAnyFusionRunnerRunning()
    {
        foreach (NetworkRunner instance in NetworkRunner.Instances)
        {
            if (instance != null && instance.IsRunning)
                return true;
        }

        return false;
    }
}
