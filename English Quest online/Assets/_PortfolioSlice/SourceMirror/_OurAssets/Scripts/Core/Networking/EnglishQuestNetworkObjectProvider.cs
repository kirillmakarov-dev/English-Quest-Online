using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Pooled <see cref="INetworkObjectProvider"/> for Fusion Shared Mode.
/// Reuses inactive prefab instances instead of Instantiate/Destroy on every Spawn/Despawn.
/// </summary>
public sealed class EnglishQuestNetworkObjectProvider : NetworkObjectProviderDefault
{
    private const int DefaultMaxPoolPerPrefab = 32;

    [SerializeField] private int _maxPoolPerPrefab = DefaultMaxPoolPerPrefab;

    private readonly Dictionary<NetworkPrefabId, Queue<NetworkObject>> _pools = new();

    public int MaxPoolPerPrefab => Mathf.Max(0, _maxPoolPerPrefab);

    public int GetPooledCount(NetworkPrefabId prefabId)
    {
        return _pools.TryGetValue(prefabId, out Queue<NetworkObject> queue) ? queue.Count : 0;
    }

    public override NetworkObjectAcquireResult AcquirePrefabInstance(
        NetworkRunner runner,
        in NetworkPrefabAcquireContext context,
        out NetworkObject instance)
    {
        instance = null;

        if (DelayIfSceneManagerIsBusy && runner.SceneManager != null && runner.SceneManager.IsBusy)
            return NetworkObjectAcquireResult.Retry;

        NetworkObject prefab;
        try
        {
            prefab = runner.Prefabs.Load(context.PrefabId, isSynchronous: context.IsSynchronous);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EnglishQuestNetworkObjectProvider] Failed to load prefab: {ex}");
            return NetworkObjectAcquireResult.Failed;
        }

        if (!prefab)
            return NetworkObjectAcquireResult.Retry;

        instance = TakeOrCreate(runner, prefab, context.PrefabId);
        if (instance == null)
            return NetworkObjectAcquireResult.Failed;

        if (context.DontDestroyOnLoad)
            runner.MakeDontDestroyOnLoad(instance.gameObject);
        else
            runner.MoveToRunnerScene(instance.gameObject);

        runner.Prefabs.AddInstance(context.PrefabId);
        return NetworkObjectAcquireResult.Success;
    }

    protected override void DestroyPrefabInstance(NetworkRunner runner, NetworkPrefabId prefabId, NetworkObject instance)
    {
        if (instance == null)
            return;

        int maxPool = MaxPoolPerPrefab;
        if (!_pools.TryGetValue(prefabId, out Queue<NetworkObject> pool))
        {
            pool = new Queue<NetworkObject>();
            _pools[prefabId] = pool;
        }

        if (maxPool > 0 && pool.Count >= maxPool)
        {
            Destroy(instance.gameObject);
            return;
        }

        instance.gameObject.SetActive(false);
        pool.Enqueue(instance);
    }

    private NetworkObject TakeOrCreate(NetworkRunner runner, NetworkObject prefab, NetworkPrefabId prefabId)
    {
        if (_pools.TryGetValue(prefabId, out Queue<NetworkObject> pool))
        {
            while (pool.Count > 0)
            {
                NetworkObject pooled = pool.Dequeue();
                if (pooled == null)
                    continue;

                pooled.gameObject.SetActive(true);
                return pooled;
            }
        }
        else
        {
            _pools[prefabId] = new Queue<NetworkObject>();
        }

        return Instantiate(prefab);
    }

    /// <summary>Test helper: enqueues an inactive instance into the pool.</summary>
    public void DebugEnqueue(NetworkPrefabId prefabId, NetworkObject instance)
    {
        if (instance == null)
            return;

        if (!_pools.TryGetValue(prefabId, out Queue<NetworkObject> pool))
        {
            pool = new Queue<NetworkObject>();
            _pools[prefabId] = pool;
        }

        instance.gameObject.SetActive(false);
        pool.Enqueue(instance);
    }
}

