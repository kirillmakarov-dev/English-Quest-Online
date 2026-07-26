using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Resolves the networked player prefab for a simulation scene.
/// </summary>
public static class PlayerPrefabResolver
{
    private static NetworkPrefabRef s_cachedPrefab;

    public static void Register(NetworkPrefabRef prefab)
    {
        if (prefab.IsValid)
            s_cachedPrefab = prefab;
    }

    public static NetworkPrefabRef Resolve(Scene scene, NetworkRunner runner = null)
    {
        NetworkPrefabRef fromContext = PlayerSceneContext.ResolvePlayerPrefab(scene);
        if (fromContext.IsValid)
            return fromContext;

        if (s_cachedPrefab.IsValid)
            return s_cachedPrefab;

        Scene searchScene = scene;
        if (runner != null && runner.SimulationUnityScene.IsValid())
            searchScene = runner.SimulationUnityScene;

        PlayerSpawner sceneSpawner = FindSpawnerInScene(searchScene);
        if (sceneSpawner != null && sceneSpawner.PlayerPrefab.IsValid)
        {
            s_cachedPrefab = sceneSpawner.PlayerPrefab;
            return s_cachedPrefab;
        }

        return default;
    }

    private static PlayerSpawner FindSpawnerInScene(Scene scene)
    {
        if (!scene.IsValid())
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            PlayerSpawner spawner = roots[i].GetComponentInChildren<PlayerSpawner>(true);
            if (spawner != null)
                return spawner;
        }

        return null;
    }
}
