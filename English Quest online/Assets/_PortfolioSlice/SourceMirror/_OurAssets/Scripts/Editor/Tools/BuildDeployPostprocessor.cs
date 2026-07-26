using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace EnglishKingdom.Editor.Tools
{
    /// <summary>
    /// Post-build hook. If "Upload on Finish" is enabled and the build succeeded,
    /// launches Deploy.ps1 automatically.
    /// </summary>
    internal class BuildDeployPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            // Use Debug.Log — AppLog defaults to LogLevel.None outside play mode, so deploy
            // diagnostics would otherwise be invisible in the Editor console after a build.
            UnityEngine.Debug.Log(
                $"[BuildDeploy] Postprocessor fired. UploadOnFinish={BuildDeploySettings.UploadOnFinish}  Result={report.summary.result}  Errors={report.summary.totalErrors}");

            if (!BuildDeploySettings.UploadOnFinish)
            {
                UnityEngine.Debug.Log(
                    "[BuildDeploy] Skipping — 'Upload on Finish' is OFF. Enable it in Tools → English Kingdom → Build → Deploy Settings.");
                return;
            }

            // Build Profiles often report Unknown (sometimes with non-zero totalErrors) here,
            // then log Succeeded afterward. Only abort on explicit Failed / Cancelled.
            bool resultOk = report.summary.result != BuildResult.Failed
                         && report.summary.result != BuildResult.Cancelled;

            if (!resultOk)
            {
                UnityEngine.Debug.LogWarning(
                    $"[BuildDeploy] Skipping — build result was '{report.summary.result}' with {report.summary.totalErrors} error(s).");
                return;
            }

            if (report.summary.result == BuildResult.Unknown)
            {
                UnityEngine.Debug.Log(
                    $"[BuildDeploy] Result is Unknown (errors={report.summary.totalErrors}) — continuing deploy; Unity often finalizes as Succeeded after this callback.");
            }

            string buildFolder = ResolveBuildOutputFolder(report);
            UnityEngine.Debug.Log($"[BuildDeploy] Build output folder: {buildFolder}");

            // IL2CPP creates large *_DoNotShip folders after the build report; wait, then delete.
            UnityDoNotShipUtility.WaitForArtifactsAndCleanup(buildFolder);

            UnityEngine.Debug.Log("[BuildDeploy] Build succeeded and Upload on Finish is ON — launching deploy...");
            BuildDeployWindow.TriggerDeployFromPostprocessor(buildFolder);
        }

        private static string ResolveBuildOutputFolder(BuildReport report)
        {
            string outputPath = report.summary.outputPath;
            if (!string.IsNullOrEmpty(outputPath))
            {
                string directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                    return directory;
            }

            return BuildDeploySettings.GameOutputFolder;
        }
    }
}
