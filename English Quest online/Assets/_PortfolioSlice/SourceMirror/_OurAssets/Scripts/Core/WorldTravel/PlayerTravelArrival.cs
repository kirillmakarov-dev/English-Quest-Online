using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Carries the next player arrival across scene loads per Fusion runner (or offline slot).
/// World travel sets this before loading; <see cref="PlayerSpawner"/> consumes it when the player object already exists.
/// </summary>
public static class PlayerTravelArrival
{
    public struct PendingArrival
    {
        public string SpawnNodeId;
        public int TargetSceneBuildIndex;
        public Vector3 WorldPosition;
        public Vector3 WorldRotationEuler;
        public bool HasWorldFallback;
    }

    private static readonly Dictionary<NetworkRunner, PendingArrival> _pendingByRunner = new();
    private static PendingArrival? _offlinePending;

    public static bool HasPending => _offlinePending.HasValue || _pendingByRunner.Count > 0;

    public static bool HasPendingFor(NetworkRunner runner) =>
        runner != null ? _pendingByRunner.ContainsKey(runner) : _offlinePending.HasValue;

    public static void SetPending(
        NetworkRunner runner,
        string spawnNodeId,
        int targetSceneBuildIndex,
        in WorldMapNodeData destination)
    {
        var pending = new PendingArrival
        {
            SpawnNodeId = spawnNodeId,
            TargetSceneBuildIndex = targetSceneBuildIndex,
            WorldPosition = destination.worldPosition,
            WorldRotationEuler = destination.worldRotationEuler,
            HasWorldFallback = destination.worldPosition != Vector3.zero
        };

        if (runner != null)
            _pendingByRunner[runner] = pending;
        else
            _offlinePending = pending;
    }

    public static bool TryConsumeForScene(
        NetworkRunner runner,
        Scene scene,
        out PendingArrival arrival)
    {
        arrival = default;

        if (!TryGetPending(runner, out PendingArrival pending))
            return false;

        if (scene.buildIndex != pending.TargetSceneBuildIndex)
            return false;

        arrival = pending;
        Clear(runner);
        return true;
    }

    public static bool TryPeekForScene(
        NetworkRunner runner,
        Scene scene,
        out PendingArrival arrival)
    {
        arrival = default;

        if (!TryGetPending(runner, out PendingArrival pending))
            return false;

        if (scene.buildIndex != pending.TargetSceneBuildIndex)
            return false;

        arrival = pending;
        return true;
    }

    public static void Clear(NetworkRunner runner = null)
    {
        if (runner != null)
            _pendingByRunner.Remove(runner);
        else
            _offlinePending = null;
    }

    public static void ClearAll()
    {
        _pendingByRunner.Clear();
        _offlinePending = null;
    }

    private static bool TryGetPending(NetworkRunner runner, out PendingArrival pending)
    {
        if (runner != null)
            return _pendingByRunner.TryGetValue(runner, out pending);

        if (_offlinePending.HasValue)
        {
            pending = _offlinePending.Value;
            return true;
        }

        pending = default;
        return false;
    }

    public static bool TryResolveTransform(
        in PendingArrival arrival,
        Scene scene,
        out Vector3 position,
        out Quaternion rotation)
    {
        if (!string.IsNullOrEmpty(arrival.SpawnNodeId)
            && PlayerSpawnPoint.TryResolveTravelSpawn(arrival.SpawnNodeId, scene, out position, out rotation))
        {
            return true;
        }

        if (arrival.HasWorldFallback)
        {
            position = arrival.WorldPosition;
            rotation = Quaternion.Euler(arrival.WorldRotationEuler);
            return true;
        }

        position = Vector3.zero;
        rotation = Quaternion.identity;
        return false;
    }
}
