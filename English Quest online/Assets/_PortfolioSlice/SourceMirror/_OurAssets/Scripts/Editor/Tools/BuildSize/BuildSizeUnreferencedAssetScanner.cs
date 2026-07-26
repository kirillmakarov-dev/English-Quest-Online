using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EnglishKingdom.Editor.Tools.BuildSize
{
    /// <summary>
    /// Lists third-party demo/example assets not referenced by game scenes in the build profile.
    /// </summary>
    public static class BuildSizeUnreferencedAssetScanner
    {
        private const string ReportPath = "Logs/BuildSizeUnreferencedAssets.txt";

        private static readonly string[] DemoFolderMarkers =
        {
            "/FeelDemos/",
            "/ExampleScene/",
            "/DemoScene",
            "/Demos/",
            "/Samples/",
        };

        [MenuItem("Tools/English Kingdom/Optimization/Build Size/Scan Unreferenced Demo Assets")]
        public static void ScanAndReport()
        {
            var gameScenePaths = LoadGameScenePaths();
            var referenced = new HashSet<string>(CollectReferencedAssetPaths(gameScenePaths));

            var sb = new StringBuilder();
            sb.AppendLine("Unreferenced third-party demo asset scan");
            sb.AppendLine($"Game scenes in build: {gameScenePaths.Count}");
            sb.AppendLine();

            int demoAssetCount = 0;
            foreach (string guid in AssetDatabase.FindAssets(string.Empty, new[] { "Assets/_ThirdParty" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path))
                    continue;
                if (!DemoFolderMarkers.Any(path.Contains))
                    continue;
                if (referenced.Contains(path))
                    continue;

                demoAssetCount++;
                sb.AppendLine(path);
            }

            sb.AppendLine();
            sb.AppendLine($"Total unreferenced demo-tagged assets: {demoAssetCount}");
            sb.AppendLine("These are typically excluded from builds unless pulled in by Resources/ references.");
            sb.AppendLine("Safe action: remove accidental references only; do not delete vendor packages.");

            string full = Path.Combine(Directory.GetCurrentDirectory(), ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, sb.ToString());
            Debug.Log($"[BuildSize] Unreferenced scan: {demoAssetCount} assets. See {ReportPath}");
        }

        private static List<string> LoadGameScenePaths()
        {
            var scenes = new List<string>();
            foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
            {
                if (entry.enabled)
                    scenes.Add(entry.path);
            }
            return scenes;
        }

        private static HashSet<string> CollectReferencedAssetPaths(IReadOnlyList<string> scenePaths)
        {
            var result = new HashSet<string>();
            var stack = new Stack<string>(scenePaths);

            while (stack.Count > 0)
            {
                string path = stack.Pop();
                if (string.IsNullOrEmpty(path) || !result.Add(path))
                    continue;

                foreach (string dependency in AssetDatabase.GetDependencies(path, false))
                {
                    if (!result.Contains(dependency))
                        stack.Push(dependency);
                }
            }

            return result;
        }
    }
}
