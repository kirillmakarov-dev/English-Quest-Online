using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MenuHostLevelCatalog", menuName = ScriptableObjectMenuPaths.UI + "/Menu Host Level Catalog")]
public class MenuHostLevelCatalogSO : ScriptableObject
{
    [Serializable]
    public struct HostableLevel
    {
        public string displayName;
        public SceneReference scene;
    }

    [SerializeField] private HostableLevel[] _levels;

    public int LevelCount => _levels?.Length ?? 0;

    public string GetDisplayName(int index)
    {
        if (_levels == null || index < 0 || index >= _levels.Length)
            return string.Empty;

        var level = _levels[index];
        return string.IsNullOrEmpty(level.displayName) ? level.scene.SceneName : level.displayName;
    }

    public int GetBuildIndex(int index)
    {
        if (_levels == null || index < 0 || index >= _levels.Length)
            return -1;

        return _levels[index].scene.BuildIndex;
    }

    public bool TryGetLevel(int index, out int buildIndex, out string displayName)
    {
        buildIndex = -1;
        displayName = string.Empty;

        if (_levels == null || index < 0 || index >= _levels.Length)
            return false;

        var level = _levels[index];
        if (!level.scene.IsValid)
            return false;

        buildIndex = level.scene.BuildIndex;
        displayName = GetDisplayName(index);
        return true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_levels == null)
            return;

        for (int i = 0; i < _levels.Length; i++)
        {
            var level = _levels[i];
            level.scene.SyncFromAsset();
            _levels[i] = level;

            if (!level.scene.IsValid && !string.IsNullOrEmpty(level.scene.ScenePath))
            {
                AppLog.Warning(
                    $"[MenuHostLevelCatalog] Level '{level.displayName}' scene '{level.scene.ScenePath}' is not in Build Settings.");
            }
        }
    }
#endif
}
