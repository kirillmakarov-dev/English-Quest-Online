using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Data-driven Fusion session configuration (Social Hub ConnectionData pattern).
/// </summary>
[CreateAssetMenu(
    fileName = "NetworkSessionProfile",
    menuName = ScriptableObjectMenuPaths.CoreNetworking + "/Session Profile")]
public class NetworkSessionProfile : ScriptableObject
{
    [SerializeField] private string _displayName = "Open World";
    [SerializeField] private string _sessionName = "OpenWorld";
    [SerializeField] private int _maxPlayers = 2;
    [SerializeField] private GameMode _gameMode = GameMode.Shared;
    [SerializeField] private string _initialSceneName = "OpenWorld";
    [SerializeField] private string _sessionPropertyKey;
    [SerializeField] private int _sessionPropertyValue;

    public string DisplayName => _displayName;
    public string SessionName => _sessionName;
    public int MaxPlayers => _maxPlayers;
    public GameMode GameMode => _gameMode;
    public string InitialSceneName => _initialSceneName;
    public bool HasSessionProperty =>
        !string.IsNullOrWhiteSpace(_sessionPropertyKey);

    public int ResolveInitialSceneBuildIndex()
    {
        if (string.IsNullOrWhiteSpace(_initialSceneName))
            return -1;

        int buildIndex = SceneUtility.GetBuildIndexByScenePath(_initialSceneName);
        if (buildIndex >= 0)
            return buildIndex;

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(scenePath))
                continue;

            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneName == _initialSceneName)
                return i;
        }

        return -1;
    }

    public Dictionary<string, SessionProperty> BuildSessionProperties()
    {
        if (!HasSessionProperty)
            return null;

        return new Dictionary<string, SessionProperty>
        {
            { _sessionPropertyKey, _sessionPropertyValue }
        };
    }

    public StartGameArgs BuildStartGameArgs(
        NetworkSceneInfo sceneInfo,
        INetworkSceneManager sceneManager,
        bool enableClientSessionCreation = true)
    {
        return new StartGameArgs
        {
            GameMode = _gameMode,
            SessionName = _sessionName,
            PlayerCount = _maxPlayers,
            Scene = sceneInfo,
            SceneManager = sceneManager,
            SessionProperties = BuildSessionProperties(),
            EnableClientSessionCreation = enableClientSessionCreation
        };
    }
}
