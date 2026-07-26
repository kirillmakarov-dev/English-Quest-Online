using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EnglishKingdom.Editor.Tools.BuildSize
{
    public static class BuildSizeTerrainTuner
    {
        private static readonly string[] TerrainPaths =
        {
            "Assets/_OurAssets/Data/Terrain/CastleTerrain_OpenWorld.asset",
        };

        [MenuItem("Tools/English Kingdom/Optimization/Build Size/Tune Terrain Resolutions")]
        public static void TuneAll()
        {
            var report = new StringBuilder();
            int changed = 0;

            foreach (string path in TerrainPaths)
                changed += TuneTerrain(path, report);

            foreach (string guid in AssetDatabase.FindAssets("t:TerrainData", new[] { "Assets/_OurAssets/Art/Prefabs/Levels" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                changed += TuneTerrain(path, report);
            }

            if (changed > 0)
                AssetDatabase.SaveAssets();

            Debug.Log($"[BuildSize] Terrain tuning complete ({changed} assets).\n{report}");
        }

        private static int TuneTerrain(string path, StringBuilder report)
        {
            var terrain = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if (terrain == null)
                return 0;

            bool changed = false;

            if (terrain.alphamapResolution > 1024)
            {
                report.AppendLine($"{path}: alphamap {terrain.alphamapResolution} → 1024");
                terrain.alphamapResolution = 1024;
                changed = true;
            }

            if (terrain.baseMapResolution > 1024)
            {
                report.AppendLine($"{path}: baseMap {terrain.baseMapResolution} → 1024");
                terrain.baseMapResolution = 1024;
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(terrain);
                return 1;
            }

            report.AppendLine($"{path}: no changes (heightmap {terrain.heightmapResolution})");
            return 0;
        }
    }
}
