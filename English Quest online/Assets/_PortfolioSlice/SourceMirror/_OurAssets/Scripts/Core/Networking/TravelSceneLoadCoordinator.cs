using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

/// <summary>
/// Tracks travel/level scene-load RPC acknowledgments from scene authority to requesters.
/// </summary>
public static class TravelSceneLoadCoordinator
{
    private static readonly Dictionary<PendingKey, UniTaskCompletionSource<bool>> s_pendingAcks = new();

    public readonly struct PendingKey : IEquatable<PendingKey>
    {
        public readonly NetworkRunner Runner;
        public readonly int BuildIndex;
        public readonly PlayerRef Requester;

        public PendingKey(NetworkRunner runner, int buildIndex, PlayerRef requester)
        {
            Runner = runner;
            BuildIndex = buildIndex;
            Requester = requester;
        }

        public bool Equals(PendingKey other) =>
            Runner == other.Runner && BuildIndex == other.BuildIndex && Requester == other.Requester;

        public override bool Equals(object obj) => obj is PendingKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Runner != null ? Runner.GetInstanceID() : 0;
                hash = (hash * 397) ^ BuildIndex;
                hash = (hash * 397) ^ Requester.GetHashCode();
                return hash;
            }
        }
    }

    public static void RegisterPendingAck(PendingKey key, UniTaskCompletionSource<bool> tcs)
    {
        s_pendingAcks[key] = tcs;
    }

    public static void UnregisterPendingAck(PendingKey key)
    {
        s_pendingAcks.Remove(key);
    }

    public static async UniTask<bool> RequestSceneLoadAsync(
        PlayerInteraction requester,
        int buildIndex,
        float timeoutSeconds = FusionSceneFollowService.RemoteLoadAckTimeoutSeconds)
    {
        if (requester?.Object == null || buildIndex < 0)
            return false;

        NetworkRunner runner = requester.Object.Runner;
        if (runner == null || !runner.IsRunning)
            return false;

        return await NetworkSessionSceneLoadRequest.RequestAndWaitForAckAsync(
            runner,
            buildIndex,
            timeoutSeconds);
    }

    public static void NotifySceneLoadResult(NetworkRunner runner, int buildIndex, PlayerRef requester, bool success)
    {
        if (runner == null)
            return;

        var key = new PendingKey(runner, buildIndex, requester);
        if (s_pendingAcks.TryGetValue(key, out UniTaskCompletionSource<bool> tcs))
            tcs.TrySetResult(success);
    }

    public static void ClearRunner(NetworkRunner runner)
    {
        if (runner == null)
            return;

        var keysToRemove = new List<PendingKey>();
        foreach (KeyValuePair<PendingKey, UniTaskCompletionSource<bool>> entry in s_pendingAcks)
        {
            if (entry.Key.Runner == runner)
                keysToRemove.Add(entry.Key);
        }

        for (int i = 0; i < keysToRemove.Count; i++)
        {
            PendingKey key = keysToRemove[i];
            if (s_pendingAcks.Remove(key, out UniTaskCompletionSource<bool> tcs))
                tcs.TrySetResult(false);
        }
    }
}
