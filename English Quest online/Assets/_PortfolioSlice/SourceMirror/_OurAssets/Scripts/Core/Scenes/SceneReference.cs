using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor-assignable scene reference that stores the asset path for runtime use.
/// Drag a scene asset in the Inspector; the path is synced via the owning object's OnValidate.
/// </summary>
[Serializable]
public struct SceneReference
{
#if UNITY_EDITOR
    [SerializeField] private UnityEditor.SceneAsset _sceneAsset;
#endif

    [SerializeField, HideInInspector] private string _scenePath;

    public string ScenePath => _scenePath;

    public int BuildIndex =>
        string.IsNullOrEmpty(_scenePath) ? -1 : SceneUtility.GetBuildIndexByScenePath(_scenePath);

    public bool IsValid => !string.IsNullOrEmpty(_scenePath) && BuildIndex >= 0;

    public string SceneName =>
        string.IsNullOrEmpty(_scenePath) ? string.Empty : Path.GetFileNameWithoutExtension(_scenePath);

#if UNITY_EDITOR
    public void SyncFromAsset()
    {
        if (_sceneAsset != null)
            _scenePath = UnityEditor.AssetDatabase.GetAssetPath(_sceneAsset);
    }
#endif
}
