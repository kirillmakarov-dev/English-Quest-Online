using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Shared <see cref="ObjectPool{T}"/> registry for <see cref="DamageFloatingText"/> prefab instances.
/// </summary>
public static class DamageFloatingTextPool
{
    private static readonly Dictionary<int, PoolEntry> PoolsByPrefabId = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
        => PoolsByPrefabId.Clear();

    public static void Spawn(
        DamageFloatingText prefab,
        int defaultCapacity,
        int maxSize,
        Transform poolRoot,
        int damage,
        Vector3 worldPosition)
    {
        if (prefab == null) return;

        PoolEntry entry = GetOrCreateEntry(prefab, defaultCapacity, maxSize, poolRoot);
        DamageFloatingText instance = entry.Pool.Get();
        instance.Play(damage, worldPosition, () => entry.Pool.Release(instance));
    }

    private static PoolEntry GetOrCreateEntry(
        DamageFloatingText prefab,
        int defaultCapacity,
        int maxSize,
        Transform poolRoot)
    {
        int prefabId = prefab.GetInstanceID();
        if (PoolsByPrefabId.TryGetValue(prefabId, out PoolEntry existing))
            return existing;

        Transform root = poolRoot != null ? poolRoot : CreatePoolRoot(prefab.name);
        int capacity = Mathf.Max(1, defaultCapacity);
        int ceiling = Mathf.Max(capacity, maxSize);

        var pool = new ObjectPool<DamageFloatingText>(
            createFunc: () => CreateInstance(prefab, root),
            actionOnGet: OnGetFromPool,
            actionOnRelease: OnReleaseToPool,
            actionOnDestroy: OnDestroyPooledObject,
            collectionCheck: true,
            defaultCapacity: capacity,
            maxSize: ceiling);

        Prewarm(pool, capacity);

        var entry = new PoolEntry(pool, root);
        PoolsByPrefabId[prefabId] = entry;
        return entry;
    }

    private static void Prewarm(ObjectPool<DamageFloatingText> pool, int count)
    {
        var warmed = new DamageFloatingText[count];
        for (int i = 0; i < count; i++)
            warmed[i] = pool.Get();

        for (int i = 0; i < count; i++)
            pool.Release(warmed[i]);
    }

    private static Transform CreatePoolRoot(string prefabName)
    {
        var rootObject = new GameObject($"{prefabName}_Pool");
        rootObject.hideFlags = HideFlags.HideInHierarchy;
        return rootObject.transform;
    }

    private static DamageFloatingText CreateInstance(DamageFloatingText prefab, Transform root)
    {
        DamageFloatingText instance = Object.Instantiate(prefab, root);
        instance.gameObject.SetActive(false);
        return instance;
    }

    private static void OnGetFromPool(DamageFloatingText instance)
    {
        if (instance == null) return;
        instance.gameObject.SetActive(true);
    }

    private static void OnReleaseToPool(DamageFloatingText instance)
    {
        if (instance == null) return;
        instance.PrepareForPool();
    }

    private static void OnDestroyPooledObject(DamageFloatingText instance)
    {
        if (instance != null)
            Object.Destroy(instance.gameObject);
    }

    private sealed class PoolEntry
    {
        public readonly ObjectPool<DamageFloatingText> Pool;
        public readonly Transform Root;

        public PoolEntry(ObjectPool<DamageFloatingText> pool, Transform root)
        {
            Pool = pool;
            Root = root;
        }
    }
}
