using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EnglishKingdom.Editor.Tools.BuildSize
{
    public static class BuildSizeValidation
    {
        private const string ValidationPath = "Logs/BuildSizeValidationChecklist.txt";

        [MenuItem("Tools/English Kingdom/Optimization/Build Size/Write Validation Checklist")]
        public static void WriteChecklist()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Build Size Validation Checklist");
            sb.AppendLine($"Generated: {System.DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine();
            sb.AppendLine("[ ] Rebuild FullGame — compare folder size to 8.7 GB baseline");
            sb.AppendLine("[ ] OpenWorld: sky, mountains, foliage — no banding");
            sb.AppendLine("[ ] Castle / PaulosCreations portals — normals and glow OK");
            sb.AppendLine("[ ] Teacher Window / Secret Code UI — sharp at intended size");
            sb.AppendLine("[ ] Owl and lion lessons — animations play after FBX compression");
            sb.AppendLine("[ ] Profiler: draw calls / CPU in OpenWorld >= baseline (static batching unchanged)");
            sb.AppendLine("[ ] No GetPixels / readable texture errors in console");
            sb.AppendLine();
            sb.AppendLine("After meta changes: focus Unity on project to reimport, or run:");
            sb.AppendLine("  Tools → English Kingdom → Optimization → Build Size → Apply All Optimizations");

            string full = Path.Combine(Directory.GetCurrentDirectory(), ValidationPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, sb.ToString());
            Debug.Log($"[BuildSize] Validation checklist written to {ValidationPath}");
        }
    }
}
