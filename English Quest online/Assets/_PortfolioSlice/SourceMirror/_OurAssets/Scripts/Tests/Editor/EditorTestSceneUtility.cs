using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EnglishKingdom.Tests.Editor
{
    internal static class EditorTestSceneUtility
    {
        const string TempSceneRoot = "Assets/Temp/EditorTestScenes";

        public static Scene CreateEmptyScene(string name)
        {
            EnsureNoUnsavedUntitledScenes();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SaveSceneToTemp(scene, SanitizeName(name));
            return scene;
        }

        public static void CloseScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            string path = scene.path;
            EditorSceneManager.CloseScene(scene, removeScene: true);

            if (IsManagedTempScene(path))
                AssetDatabase.DeleteAsset(path);
        }

        static void EnsureNoUnsavedUntitledScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded && string.IsNullOrEmpty(scene.path))
                    SaveSceneToTemp(scene, $"bootstrap_{Guid.NewGuid():N}");
            }
        }

        static void SaveSceneToTemp(Scene scene, string name)
        {
            EnsureTempFolderExists();
            string path = $"{TempSceneRoot}/{name}_{Guid.NewGuid():N}.unity";
            EditorSceneManager.SaveScene(scene, path);
        }

        static void EnsureTempFolderExists()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Temp"))
                AssetDatabase.CreateFolder("Assets", "Temp");

            if (!AssetDatabase.IsValidFolder(TempSceneRoot))
                AssetDatabase.CreateFolder("Assets/Temp", "EditorTestScenes");
        }

        static bool IsManagedTempScene(string path) =>
            !string.IsNullOrEmpty(path)
            && path.Replace('\\', '/').StartsWith(TempSceneRoot, StringComparison.Ordinal);

        static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "TestScene";

            foreach (char invalid in Path.GetInvalidFileNameChars())
                name = name.Replace(invalid, '_');

            return name;
        }
    }
}
