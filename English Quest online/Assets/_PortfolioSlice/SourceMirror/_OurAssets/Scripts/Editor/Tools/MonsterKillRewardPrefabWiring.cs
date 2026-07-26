#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EnglishKingdom.Editor.Tools
{
    /// <summary>
    /// Ensures kill-credit prefab components exist after script import.
    /// </summary>
    [InitializeOnLoad]
    internal static class MonsterKillRewardPrefabWiring
    {
        private const string NetworkedBasePath =
            "Assets/_OurAssets/Art/Prefabs/Characters/Monsters/MonsterBase_Networked.prefab";

        private const string LocalBasePath =
            "Assets/_OurAssets/Art/Prefabs/Characters/Monsters/Local/MonsterBase_Local.prefab";

        private const string CursedPriestPath =
            "Assets/_OurAssets/Art/Prefabs/Characters/Monsters/Monster_CursedPriest.prefab";

        static MonsterKillRewardPrefabWiring()
        {
            EditorApplication.delayCall += WirePrefabsIfNeeded;
        }

        private static void WirePrefabsIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            WireBasePrefab(NetworkedBasePath);
            WireBasePrefab(LocalBasePath);
            WireCursedPriestPrefab();
        }

        private static void WireBasePrefab(string prefabPath)
        {
            if (!System.IO.File.Exists(prefabPath))
                return;

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
                return;

            bool changed = EnsureComponent<DamageAttributionComponent>(root);

            if (changed)
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void WireCursedPriestPrefab()
        {
            if (!System.IO.File.Exists(CursedPriestPath))
                return;

            GameObject root = PrefabUtility.LoadPrefabContents(CursedPriestPath);
            if (root == null)
                return;

            bool changed = EnsureComponent<DamageAttributionComponent>(root);
            changed |= EnsureComponent<MonsterKillRewardListener>(root);

            if (changed)
                PrefabUtility.SaveAsPrefabAsset(root, CursedPriestPath);

            PrefabUtility.UnloadPrefabContents(root);
        }

        private static bool EnsureComponent<T>(GameObject root) where T : Component
        {
            if (root.GetComponent<T>() != null)
                return false;

            root.AddComponent<T>();
            return true;
        }
    }
}
#endif
