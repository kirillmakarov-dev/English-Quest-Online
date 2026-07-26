#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MonsterSpawnPointPrefabBuilder
{
    public const string FolderPath = "Assets/_OurAssets/Art/Prefabs/GamePlay/Monsters";
    public const string PrefabPath = FolderPath + "/MonsterSpawnPoint.prefab";

    [MenuItem("Tools/English Kingdom/Monsters/Create Monster Spawn Point Prefab")]
    public static void CreateFromMenu()
    {
        GameObject prefab = EnsurePrefab();
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"[MonsterSpawnPointPrefabBuilder] Spawn point prefab ready at '{PrefabPath}'.");
    }

    public static GameObject EnsurePrefab()
    {
        EnsureFolder(FolderPath);

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null)
            return existing;

        GameObject root = new GameObject(
            "MonsterSpawnPoint",
            typeof(MonsterSpawnPoint));

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        return prefab;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string leaf = Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif
