using UnityEditor;

namespace EnglishQuest.Editor.Tools
{
    /// <summary>
    /// EditorPrefs-backed settings for the Build Deploy pipeline.
    /// All fields are editor-only and not version-controlled.
    /// </summary>
    internal static class BuildDeploySettings
    {
        // ─── Prefs keys ───────────────────────────────────────────────────────
        private const string KeyUploadOnFinish   = "EK.BuildDeploy.UploadOnFinish";
        private const string KeyDeployScriptPath = "EK.BuildDeploy.DeployScriptPath";
        private const string KeyGameOutputFolder  = "EK.BuildDeploy.GameOutputFolder";

        // ─── Defaults ─────────────────────────────────────────────────────────
        private const string DefaultDeployScriptPath =
            @"C:\Users\123ne\source\repos\EnglishRoomLuncherWPF\EnglishRoomLuncherWPF\Tools\Deploy.ps1";

        private const string DefaultGameOutputFolder =
            @"C:\Users\123ne\Downloads\FullGame";

        // ─── Properties ───────────────────────────────────────────────────────
        internal static bool UploadOnFinish
        {
            get => EditorPrefs.GetBool(KeyUploadOnFinish, false);
            set => EditorPrefs.SetBool(KeyUploadOnFinish, value);
        }

        internal static string DeployScriptPath
        {
            get => EditorPrefs.GetString(KeyDeployScriptPath, DefaultDeployScriptPath);
            set => EditorPrefs.SetString(KeyDeployScriptPath, value);
        }

        internal static string GameOutputFolder
        {
            get => EditorPrefs.GetString(KeyGameOutputFolder, DefaultGameOutputFolder);
            set => EditorPrefs.SetString(KeyGameOutputFolder, value);
        }
    }
}

