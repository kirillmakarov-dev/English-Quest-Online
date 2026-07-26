using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EnglishKingdom.Editor.Tools.Polyart
{
    /// <summary>
    /// Reverses expensive convex MeshCollider spam on DreamscapeCastle prefabs:
    /// strip foliage/prop colliders, convert structure MeshColliders to BoxColliders.
    /// Processes one prefab at a time (no StartAssetEditing batch) to avoid corrupting large assets.
    /// Never touches Open World Environment.prefab — only Polyart vendor prefab roots.
    /// </summary>
    public static class PolyartEnvironmentColliderOptimizer
    {
        private const string PolyartPrefabsRoot = "Assets/Polyart/PolyartStudio/DreamscapeCastle/Prefabs";

        public const string CategoryFoliage = "Foliage";
        public const string CategoryProps = "Props";
        public const string CategoryStones = "Stones";
        public const string CategoryBuilding = "Building";
        public const string CategoryBuildingsFull = "BuildingsFull";

        [MenuItem("Tools/English Kingdom/Art/Polyart/Optimize Environment Colliders/All")]
        public static void OptimizeAllFromMenu()
        {
            var report = OptimizeAllCategories();
            EditorUtility.DisplayDialog("Polyart Environment Collider Optimize", report, "OK");
        }

        [MenuItem("Tools/English Kingdom/Art/Polyart/Optimize Environment Colliders/Foliage")]
        public static void OptimizeFoliageFromMenu() => ShowCategoryResult(CategoryFoliage);

        [MenuItem("Tools/English Kingdom/Art/Polyart/Optimize Environment Colliders/Props")]
        public static void OptimizePropsFromMenu() => ShowCategoryResult(CategoryProps);

        [MenuItem("Tools/English Kingdom/Art/Polyart/Optimize Environment Colliders/Stones")]
        public static void OptimizeStonesFromMenu() => ShowCategoryResult(CategoryStones);

        [MenuItem("Tools/English Kingdom/Art/Polyart/Optimize Environment Colliders/Building")]
        public static void OptimizeBuildingFromMenu() => ShowCategoryResult(CategoryBuilding);

        [MenuItem("Tools/English Kingdom/Art/Polyart/Optimize Environment Colliders/BuildingsFull")]
        public static void OptimizeBuildingsFullFromMenu() => ShowCategoryResult(CategoryBuildingsFull);

        [MenuItem("Tools/English Kingdom/Art/Polyart/Count Environment Colliders")]
        public static void CountEnvironmentCollidersFromMenu()
        {
            var report = CountEnvironmentColliders();
            EditorUtility.DisplayDialog("Polyart Collider Count", report, "OK");
        }

        private static void ShowCategoryResult(string category)
        {
            var report = OptimizeCategory(category);
            EditorUtility.DisplayDialog($"Polyart Optimize — {category}", report, "OK");
        }

        public static string OptimizeAllCategories()
        {
            var sb = new StringBuilder();
            string[] categories =
            {
                CategoryFoliage,
                CategoryProps,
                CategoryStones,
                CategoryBuilding,
                CategoryBuildingsFull
            };

            for (int i = 0; i < categories.Length; i++)
            {
                sb.AppendLine($"=== {categories[i]} ===");
                sb.AppendLine(OptimizeCategory(categories[i]));
            }

            string report = sb.ToString();
            Debug.Log("[PolyartEnvironmentColliderOptimize] ALL\n" + report);
            return report;
        }

        public static string OptimizeCategory(string category)
        {
            string root = PolyartPrefabsRoot + "/" + category;
            if (!AssetDatabase.IsValidFolder(root))
                return $"Folder missing: {root}";

            var prefabPaths = CollectPrefabPaths(root);
            int modifiedPrefabs = 0;
            int removedMesh = 0;
            int removedOther = 0;
            int addedBoxes = 0;
            int keptMesh = 0;
            ColliderCategory colliderCategory = ClassifyCategoryName(category);

            try
            {
                for (int i = 0; i < prefabPaths.Count; i++)
                {
                    string path = prefabPaths[i];
                    if (path.IndexOf("/Environment/Environment.prefab", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;

                    EditorUtility.DisplayProgressBar(
                        $"Optimize Colliders — {category}",
                        path,
                        prefabPaths.Count == 0 ? 1f : (float)i / prefabPaths.Count);

                    if (!OptimizeSinglePrefab(
                            path,
                            colliderCategory,
                            ref removedMesh,
                            ref removedOther,
                            ref addedBoxes,
                            ref keptMesh))
                        continue;

                    modifiedPrefabs++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
            }

            var report = new StringBuilder();
            report.AppendLine($"Category: {category}");
            report.AppendLine($"Prefabs scanned: {prefabPaths.Count}");
            report.AppendLine($"Modified prefabs: {modifiedPrefabs}");
            report.AppendLine($"Removed MeshColliders: {removedMesh}");
            report.AppendLine($"Removed other colliders: {removedOther}");
            report.AppendLine($"Added BoxColliders: {addedBoxes}");
            report.AppendLine($"Kept MeshColliders (stairs/walkables): {keptMesh}");
            Debug.Log("[PolyartEnvironmentColliderOptimize] " + report.ToString().Replace("\r\n", " | "));
            return report.ToString();
        }

        /// <summary>Backward-compatible entry used by MCP / older menu paths. </summary>
        public static string OptimizeEnvironmentColliders() => OptimizeAllCategories();

        public static string CountEnvironmentColliders()
        {
            var prefabPaths = CollectPrefabPaths(
                PolyartPrefabsRoot + "/Building",
                PolyartPrefabsRoot + "/BuildingsFull",
                PolyartPrefabsRoot + "/Foliage",
                PolyartPrefabsRoot + "/Props",
                PolyartPrefabsRoot + "/Stones");

            int mesh = 0;
            int box = 0;
            int other = 0;

            for (int i = 0; i < prefabPaths.Count; i++)
            {
                string path = prefabPaths[i];
                EditorUtility.DisplayProgressBar("Count Colliders", path, (float)i / prefabPaths.Count);
                var root = PrefabUtility.LoadPrefabContents(path);
                if (root == null)
                    continue;

                mesh += CountComponents<MeshCollider>(root);
                box += CountComponents<BoxCollider>(root);
                other += CountComponents<CapsuleCollider>(root) + CountComponents<SphereCollider>(root);
                PrefabUtility.UnloadPrefabContents(root);
            }

            EditorUtility.ClearProgressBar();

            var report = new StringBuilder();
            report.AppendLine($"Prefabs scanned: {prefabPaths.Count}");
            report.AppendLine($"MeshColliders: {mesh}");
            report.AppendLine($"BoxColliders: {box}");
            report.AppendLine($"Capsule+Sphere: {other}");
            Debug.Log("[PolyartColliderCount] " + report.ToString().Replace("\r\n", " | "));
            return report.ToString();
        }

        private static bool OptimizeSinglePrefab(
            string path,
            ColliderCategory category,
            ref int removedMesh,
            ref int removedOther,
            ref int addedBoxes,
            ref int keptMesh)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
                return false;

            int localRemovedMesh = 0;
            int localRemovedOther = 0;
            int localAddedBoxes = 0;
            int localKeptMesh = 0;

            try
            {
                OptimizePrefab(root, category, ref localRemovedMesh, ref localRemovedOther, ref localAddedBoxes, ref localKeptMesh);
                if (localRemovedMesh == 0 && localRemovedOther == 0 && localAddedBoxes == 0)
                    return false;

                PrefabUtility.SaveAsPrefabAsset(root, path);
                removedMesh += localRemovedMesh;
                removedOther += localRemovedOther;
                addedBoxes += localAddedBoxes;
                keptMesh += localKeptMesh;
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void OptimizePrefab(
            GameObject root,
            ColliderCategory category,
            ref int removedMesh,
            ref int removedOther,
            ref int addedBoxes,
            ref int keptMesh)
        {
            if (category == ColliderCategory.StripAll || category == ColliderCategory.Props)
            {
                removedMesh += RemoveAllOfType<MeshCollider>(root);
                removedOther += RemoveAllOfType<BoxCollider>(root);
                removedOther += RemoveAllOfType<CapsuleCollider>(root);
                removedOther += RemoveAllOfType<SphereCollider>(root);
                return;
            }

            var meshColliders = root.GetComponentsInChildren<MeshCollider>(true);
            for (int i = 0; i < meshColliders.Length; i++)
            {
                MeshCollider meshCollider = meshColliders[i];
                if (meshCollider == null)
                    continue;

                GameObject go = meshCollider.gameObject;
                if (IsLodNonZero(go.name))
                {
                    UnityEngine.Object.DestroyImmediate(meshCollider);
                    removedMesh++;
                    continue;
                }

                if (category == ColliderCategory.Stairs || IsStairsName(go.name) || IsStairsName(root.name))
                {
                    if (meshCollider.sharedMesh != null)
                    {
                        meshCollider.convex = false;
                        keptMesh++;
                        continue;
                    }
                }

                Bounds localBounds;
                if (meshCollider.sharedMesh != null)
                {
                    localBounds = meshCollider.sharedMesh.bounds;
                }
                else
                {
                    Renderer renderer = go.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Bounds world = renderer.bounds;
                        Vector3 localCenter = go.transform.InverseTransformPoint(world.center);
                        Vector3 localSize = go.transform.InverseTransformVector(world.size);
                        localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
                        localBounds = new Bounds(localCenter, localSize);
                    }
                    else
                    {
                        localBounds = new Bounds(Vector3.zero, Vector3.one);
                    }
                }

                UnityEngine.Object.DestroyImmediate(meshCollider);
                removedMesh++;

                if (go.GetComponent<Collider>() != null)
                    continue;

                var box = go.AddComponent<BoxCollider>();
                box.center = localBounds.center;
                box.size = localBounds.size;
                addedBoxes++;
            }

            StripLodNonZeroColliders(root, ref removedOther);
        }

        private static void StripLodNonZeroColliders(GameObject root, ref int removedOther)
        {
            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];
                if (col == null || col is MeshCollider)
                    continue;

                if (!IsLodNonZero(col.gameObject.name))
                    continue;

                UnityEngine.Object.DestroyImmediate(col);
                removedOther++;
            }
        }

        private static ColliderCategory ClassifyCategoryName(string category)
        {
            if (string.Equals(category, CategoryFoliage, StringComparison.OrdinalIgnoreCase))
                return ColliderCategory.StripAll;
            if (string.Equals(category, CategoryProps, StringComparison.OrdinalIgnoreCase))
                return ColliderCategory.Props;
            if (category.IndexOf("Stairs", StringComparison.OrdinalIgnoreCase) >= 0)
                return ColliderCategory.Stairs;
            return ColliderCategory.Structure;
        }

        private static bool IsStairsName(string name)
        {
            return !string.IsNullOrEmpty(name)
                   && name.IndexOf("Stairs", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsLodNonZero(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return false;

            if (objectName.Contains("LOD1") || objectName.Contains("LOD2") || objectName.Contains("LOD3"))
                return true;

            if (objectName.Contains("LOD0"))
                return false;

            return objectName.Contains("_LOD");
        }

        private static int RemoveAllOfType<T>(GameObject root) where T : Component
        {
            var components = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                    UnityEngine.Object.DestroyImmediate(components[i]);
            }

            return components.Length;
        }

        private static int CountComponents<T>(GameObject root) where T : Component
        {
            return root.GetComponentsInChildren<T>(true).Length;
        }

        private static List<string> CollectPrefabPaths(params string[] roots)
        {
            var paths = new HashSet<string>();
            foreach (string root in roots)
            {
                if (!AssetDatabase.IsValidFolder(root))
                    continue;

                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { root }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.IndexOf("/Environment/Environment.prefab", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    paths.Add(path);
                }
            }

            var list = new List<string>(paths);
            list.Sort(StringComparer.Ordinal);
            return list;
        }

        private enum ColliderCategory
        {
            StripAll,
            Props,
            Stairs,
            Structure
        }
    }
}
