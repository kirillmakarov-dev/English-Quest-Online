#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishKingdom.Editor
{
    public static class LogLevelDebugTools
    {
        private const string AssetDirectory = "Assets/_OurAssets/Resources/Editor";
        private const string AssetPath = AssetDirectory + "/LogServiceDebugConfig.asset";

        [MenuItem("Tools/English Kingdom/Debug/Log Level/Open Config Asset")]
        private static void OpenConfigAsset()
        {
            Selection.activeObject = GetOrCreateConfig();
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        [MenuItem("Tools/English Kingdom/Debug/Log Level/None")]
        private static void SetNone() => SetLevel(LogLevel.None);

        [MenuItem("Tools/English Kingdom/Debug/Log Level/Error")]
        private static void SetError() => SetLevel(LogLevel.Error);

        [MenuItem("Tools/English Kingdom/Debug/Log Level/Warning")]
        private static void SetWarning() => SetLevel(LogLevel.Warning);

        [MenuItem("Tools/English Kingdom/Debug/Log Level/Info")]
        private static void SetInfo() => SetLevel(LogLevel.Info);

        private static void SetLevel(LogLevel level)
        {
            LogServiceDebugConfig config = GetOrCreateConfig();
            if (config == null)
            {
                Debug.LogError("[LogLevelDebugTools] Could not load or create LogServiceDebugConfig.");
                return;
            }

            if (config.initialLevel != level)
            {
                config.initialLevel = level;
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }

            if (Application.isPlaying &&
                ServiceLocator.Global.TryGet(out ILogService logService))
            {
                logService.Level = level;
            }

            Debug.Log($"[LogLevelDebugTools] Log level set to {level}.");
        }

        private static LogServiceDebugConfig GetOrCreateConfig()
        {
            LogServiceDebugConfig config = AssetDatabase.LoadAssetAtPath<LogServiceDebugConfig>(AssetPath);
            if (config != null)
                return config;

            EnsureDirectory(AssetDirectory);
            config = ScriptableObject.CreateInstance<LogServiceDebugConfig>();
            AssetDatabase.CreateAsset(config, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return config;
        }

        private static void EnsureDirectory(string directoryPath)
        {
            if (AssetDatabase.IsValidFolder(directoryPath))
                return;

            string[] parts = directoryPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
#endif
