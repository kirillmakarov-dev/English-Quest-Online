#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EnglishKingdom.Editor
{
    /// <summary>
    /// Player builds always use Fusion Single peer so scene loading matches Development
    /// (normal LoadSceneMode.Single, correct skybox/lighting). Editor can keep Multiple for co-op tests.
    /// </summary>
    public sealed class FusionPeerModeBuildPreprocessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        const string ConfigAssetPath = "Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion";
        const int SinglePeerMode = 0;

        static int? _peerModeBeforeBuild;

        public int callbackOrder => 10;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!TryReadPeerMode(ConfigAssetPath, out int currentPeerMode))
            {
                Debug.LogWarning(
                    $"[FusionPeerModeBuildPreprocessor] Could not read PeerMode from '{ConfigAssetPath}'. " +
                    "Build will use whatever PeerMode is already in the asset.");
                return;
            }

            if (currentPeerMode == SinglePeerMode)
                return;

            _peerModeBeforeBuild = currentPeerMode;
            if (!TryWritePeerMode(ConfigAssetPath, SinglePeerMode))
            {
                throw new BuildFailedException(
                    $"[FusionPeerModeBuildPreprocessor] Failed to set PeerMode to Single on '{ConfigAssetPath}' before build.");
            }

            AssetDatabase.ImportAsset(ConfigAssetPath, ImportAssetOptions.ForceUpdate);
            Debug.Log(
                $"[FusionPeerModeBuildPreprocessor] Set Fusion PeerMode to Single for build " +
                $"(restoring editor value {_peerModeBeforeBuild} after build).");
        }

        public void OnPostprocessBuild(BuildReport report) => RestoreEditorPeerMode();

        [InitializeOnLoadMethod]
        private static void RestorePeerModeAfterDomainReload()
        {
            // If a build/domain reload interrupted post-process, restore on next editor tick.
            if (_peerModeBeforeBuild.HasValue)
                EditorApplication.delayCall += RestoreEditorPeerMode;
        }

        static void RestoreEditorPeerMode()
        {
            if (!_peerModeBeforeBuild.HasValue)
                return;

            int restoreValue = _peerModeBeforeBuild.Value;
            _peerModeBeforeBuild = null;

            if (!File.Exists(ConfigAssetPath))
                return;

            if (!TryWritePeerMode(ConfigAssetPath, restoreValue))
            {
                Debug.LogWarning(
                    $"[FusionPeerModeBuildPreprocessor] Failed to restore PeerMode {restoreValue} on '{ConfigAssetPath}'. " +
                    "Set it manually under Fusion → Network Project Config if needed.");
                return;
            }

            AssetDatabase.ImportAsset(ConfigAssetPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[FusionPeerModeBuildPreprocessor] Restored Fusion PeerMode to {restoreValue} in the editor.");
        }

        static bool TryReadPeerMode(string assetPath, out int peerMode)
        {
            peerMode = SinglePeerMode;
            if (!File.Exists(assetPath))
                return false;

            Match match = Regex.Match(File.ReadAllText(assetPath), "\"PeerMode\"\\s*:\\s*(\\d+)");
            if (!match.Success)
                return false;

            return int.TryParse(match.Groups[1].Value, out peerMode);
        }

        static bool TryWritePeerMode(string assetPath, int peerMode)
        {
            if (!File.Exists(assetPath))
                return false;

            string text = File.ReadAllText(assetPath);
            if (!Regex.IsMatch(text, "\"PeerMode\"\\s*:\\s*\\d+"))
                return false;

            text = Regex.Replace(text, "\"PeerMode\"\\s*:\\s*\\d+", $"\"PeerMode\": {peerMode}");
            File.WriteAllText(assetPath, text);
            return true;
        }
    }
}
#endif
