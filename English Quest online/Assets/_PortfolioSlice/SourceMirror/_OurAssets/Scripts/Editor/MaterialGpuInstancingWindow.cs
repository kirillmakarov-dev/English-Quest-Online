using System;
using UnityEditor;
using UnityEngine;

namespace EnglishQuest.Editor.Tools
{
    public class MaterialGpuInstancingWindow : EditorWindow
    {
        [MenuItem("Tools/English Kingdom/Optimization/Enable GPU Instancing")]
        public static void ShowWindow()
        {
            var window = GetWindow<MaterialGpuInstancingWindow>("GPU Instancing");
            window.minSize = new Vector2(360f, 140f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Material GPU Instancing", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            EditorGUILayout.HelpBox(
                "Finds all Material assets in the project and enables GPU instancing on each one. " +
                "Materials that are already enabled are skipped. Materials using incompatible shaders are reported and skipped.",
                MessageType.Info);

            EditorGUILayout.Space(10f);

            if (GUILayout.Button("Enable GPU Instancing For All Materials", GUILayout.Height(32f)))
            {
                EnableGpuInstancingForAllMaterials();
            }
        }

        private static void EnableGpuInstancingForAllMaterials()
        {
            string[] materialGuids = AssetDatabase.FindAssets("t:Material");

            int changedCount = 0;
            int alreadyEnabledCount = 0;
            int skippedCount = 0;

            for (int index = 0; index < materialGuids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(materialGuids[index]);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material == null)
                {
                    skippedCount++;
                    continue;
                }

                if (material.enableInstancing)
                {
                    alreadyEnabledCount++;
                    continue;
                }

                try
                {
                    Undo.RecordObject(material, "Enable GPU Instancing");
                    material.enableInstancing = true;
                    EditorUtility.SetDirty(material);
                    changedCount++;
                }
                catch (Exception exception)
                {
                    skippedCount++;
                    AppLog.Warning($"[GPU Instancing] Skipped material at {path}. Reason: {exception.Message}", material);
                }
            }

            if (changedCount > 0)
            {
                AssetDatabase.SaveAssets();
            }

            AssetDatabase.Refresh();

            string message =
                $"Updated: {changedCount}\n" +
                $"Already enabled: {alreadyEnabledCount}\n" +
                $"Skipped: {skippedCount}\n" +
                $"Total materials found: {materialGuids.Length}";

            AppLog.Info($"[GPU Instancing] {message}");
            EditorUtility.DisplayDialog("GPU Instancing Complete", message, "OK");
        }
    }
}
