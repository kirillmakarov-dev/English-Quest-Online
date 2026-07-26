using Fusion;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene-local bootstrap for player spawn: prefab reference and follow camera.
/// Auto-registers from <see cref="PlayerSpawner"/> when not placed manually.
/// </summary>
public class PlayerSceneContext : MonoBehaviour
{
    private static readonly Dictionary<int, PlayerSceneContext> s_bySceneHandle = new();

    [SerializeField] private NetworkPrefabRef _playerPrefab;
    [SerializeField] private CinemachineCamera _followCamera;

    public NetworkPrefabRef PlayerPrefab => _playerPrefab;
    public CinemachineCamera FollowCamera => _followCamera;

    private void Awake()
    {
        RegisterForCurrentScene();
        ResolveReferences();

        if (_playerPrefab.IsValid)
            PlayerSpawnCoordinator.RegisterPlayerPrefab(_playerPrefab);
    }

    private void OnEnable()
    {
        RegisterForCurrentScene();
    }

    private void OnDestroy()
    {
        if (!gameObject.scene.IsValid())
            return;

        if (s_bySceneHandle.TryGetValue(gameObject.scene.handle, out PlayerSceneContext registered)
            && registered == this)
        {
            s_bySceneHandle.Remove(gameObject.scene.handle);
        }
    }

    public static void RefreshForScene(Scene scene)
    {
        if (!scene.IsValid())
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            PlayerSceneContext context = roots[i].GetComponentInChildren<PlayerSceneContext>(true);
            if (context != null)
                context.RegisterForCurrentScene();
        }
    }

    private void RegisterForCurrentScene()
    {
        if (!gameObject.scene.IsValid())
            return;

        var staleHandles = new List<int>();
        foreach (KeyValuePair<int, PlayerSceneContext> entry in s_bySceneHandle)
        {
            if (entry.Value == this && entry.Key != gameObject.scene.handle)
                staleHandles.Add(entry.Key);
        }

        for (int i = 0; i < staleHandles.Count; i++)
            s_bySceneHandle.Remove(staleHandles[i]);

        s_bySceneHandle[gameObject.scene.handle] = this;
    }

    public void Configure(NetworkPrefabRef playerPrefab, CinemachineCamera followCamera = null)
    {
        if (playerPrefab.IsValid)
            _playerPrefab = playerPrefab;

        if (followCamera != null)
            _followCamera = followCamera;
    }

    public static void EnsureRegistered(GameObject host, NetworkPrefabRef playerPrefab)
    {
        if (host == null)
            return;

        Scene scene = host.scene;
        if (!scene.IsValid())
            return;

        PlayerSceneContext context = host.GetComponent<PlayerSceneContext>();
        if (context == null)
            context = host.AddComponent<PlayerSceneContext>();

        context.Configure(playerPrefab);
    }

    public static bool TryGet(Scene scene, out PlayerSceneContext context)
    {
        context = null;
        if (!scene.IsValid())
            return false;

        return s_bySceneHandle.TryGetValue(scene.handle, out context) && context != null;
    }

    public static NetworkPrefabRef ResolvePlayerPrefab(Scene scene)
    {
        if (TryGet(scene, out PlayerSceneContext context) && context._playerPrefab.IsValid)
            return context._playerPrefab;

        return default;
    }

    public static CinemachineCamera ResolveFollowCamera(Scene scene)
    {
        if (TryGet(scene, out PlayerSceneContext context) && context._followCamera != null)
            return context._followCamera;

        return FindFollowCameraInScene(scene);
    }

    private void ResolveReferences()
    {
        if (!_playerPrefab.IsValid)
        {
            PlayerSpawner spawner = GetComponent<PlayerSpawner>();
            if (spawner != null && spawner.PlayerPrefab.IsValid)
                _playerPrefab = spawner.PlayerPrefab;
        }

        if (_followCamera == null && gameObject.scene.IsValid())
            _followCamera = FindFollowCameraInScene(gameObject.scene);
    }

    private static CinemachineCamera FindFollowCameraInScene(Scene scene)
    {
        if (!scene.IsValid())
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            CinemachineCamera[] cameras = roots[i].GetComponentsInChildren<CinemachineCamera>(true);
            for (int c = 0; c < cameras.Length; c++)
            {
                if (cameras[c].GetComponent<CinemachineOrbitalFollow>() != null)
                    return cameras[c];
            }
        }

        return null;
    }
}
