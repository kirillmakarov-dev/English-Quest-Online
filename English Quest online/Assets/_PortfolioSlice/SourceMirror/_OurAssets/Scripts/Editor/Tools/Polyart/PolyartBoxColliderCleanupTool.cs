using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EnglishKingdom.Editor.Tools.Polyart
{
    /// <summary>
    /// Fixes Polyart DreamscapeCastle colliders for open-world mirroring.
    /// Replaces BoxColliders with convex MeshColliders on collision-worthy LOD meshes.
    /// </summary>
    public static class PolyartBoxColliderCleanupTool
    {
        private const string PolyartPrefabsRoot = "Assets/Polyart/PolyartStudio/DreamscapeCastle/Prefabs";

        private static readonly string[] PrefabCategoryRoots =
        {
            PolyartPrefabsRoot + "/Building",
            PolyartPrefabsRoot + "/BuildingsFull",
            PolyartPrefabsRoot + "/Foliage",
            PolyartPrefabsRoot + "/Props",
        };

        [MenuItem("Tools/English Kingdom/Art/Polyart/Fix Environment Colliders")]
        public static void FixEnvironmentCollidersFromMenu()
        {
            var report = FixEnvironmentColliders();
            EditorUtility.DisplayDialog("Polyart Collider Fix", report, "OK");
        }

        [MenuItem("Tools/English Kingdom/Art/Polyart/Remove Environment BoxColliders")]
        public static void RemoveEnvironmentBoxCollidersFromMenu()
        {
            var report = RemoveEnvironmentBoxColliders();
            EditorUtility.DisplayDialog("Polyart BoxCollider Cleanup", report, "OK");
        }

        public static string FixEnvironmentColliders()
        {
            var prefabPaths = CollectPrefabPaths();
            int modifiedPrefabs = 0;
            int addedMeshColliders = 0;
            int skipped = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var path in prefabPaths)
                {
                    var root = PrefabUtility.LoadPrefabContents(path);
                    if (root == null)
                        continue;

                    var changes = FixColliders(root, ref skipped);
                    if (changes <= 0)
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                        continue;
                    }

                    addedMeshColliders += changes;

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    PrefabUtility.UnloadPrefabContents(root);
                    modifiedPrefabs++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            var report = new StringBuilder();
            report.AppendLine($"Modified prefabs: {modifiedPrefabs}");
            report.AppendLine($"Added MeshColliders: {addedMeshColliders}");
            report.AppendLine($"Skipped (no mesh / already collider): {skipped}");
            Debug.Log("[PolyartColliderFix] " + report.ToString().Replace("\r\n", " | "));
            return report.ToString();
        }

        public static string RemoveEnvironmentBoxColliders()
        {
            var prefabPaths = CollectPrefabPaths(PrefabCategoryRoots[0], PrefabCategoryRoots[1], PrefabCategoryRoots[2]);
            int modifiedPrefabs = 0;
            int removedColliders = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var path in prefabPaths)
                {
                    var root = PrefabUtility.LoadPrefabContents(path);
                    if (root == null)
                        continue;

                    var removedFromPrefab = RemoveBoxColliders(root);
                    if (removedFromPrefab <= 0)
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                        continue;
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    PrefabUtility.UnloadPrefabContents(root);

                    modifiedPrefabs++;
                    removedColliders += removedFromPrefab;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            var report = new StringBuilder();
            report.AppendLine($"Modified prefabs: {modifiedPrefabs}");
            report.AppendLine($"Removed BoxColliders: {removedColliders}");
            Debug.Log("[PolyartBoxColliderCleanup] " + report.ToString().Replace("\r\n", " | "));
            return report.ToString();
        }

        private static List<string> CollectPrefabPaths(params string[] roots)
        {
            var searchRoots = roots is { Length: > 0 } ? roots : PrefabCategoryRoots;
            var paths = new HashSet<string>();

            foreach (var root in searchRoots)
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { root }))
                    paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            return new List<string>(paths);
        }

        private static int FixColliders(GameObject root, ref int skipped)
        {
            int added = 0;

            foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!ShouldHaveCollider(meshFilter.gameObject.name))
                    continue;

                var gameObject = meshFilter.gameObject;
                if (gameObject.GetComponent<Collider>() != null)
                {
                    if (gameObject.GetComponent<BoxCollider>() != null)
                    {
                        Object.DestroyImmediate(gameObject.GetComponent<BoxCollider>());
                        added += AddMeshCollider(meshFilter) ? 1 : 0;
                    }

                    continue;
                }

                if (meshFilter.sharedMesh == null)
                {
                    skipped++;
                    continue;
                }

                if (AddMeshCollider(meshFilter))
                    added++;
                else
                    skipped++;
            }

            return added;
        }

        private static bool AddMeshCollider(MeshFilter meshFilter)
        {
            if (meshFilter.sharedMesh == null)
                return false;

            var meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = true;
            return true;
        }

        private static bool ShouldHaveCollider(string objectName)
        {
            if (objectName.Contains("LOD1") || objectName.Contains("LOD2") || objectName.Contains("LOD3"))
                return false;

            if (objectName.Contains("LOD0"))
                return true;

            if (objectName.Contains("_LOD"))
                return false;

            return true;
        }

        private static int RemoveBoxColliders(GameObject root)
        {
            var boxColliders = root.GetComponentsInChildren<BoxCollider>(true);
            if (boxColliders.Length == 0)
                return 0;

            foreach (var boxCollider in boxColliders)
                Object.DestroyImmediate(boxCollider);

            return boxColliders.Length;
        }

    }
}
