using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace EnglishKingdom.Editor.Tools
{
    /// <summary>
    /// IL2CPP / Burst builds emit large diagnostic folders beside the player exe.
    /// They must not be uploaded and may still be written after IPostprocessBuildWithReport.
    /// </summary>
    internal static class UnityDoNotShipUtility
    {
        private static readonly string[] KnownFolderSuffixes =
        {
            "_BurstDebugInformation_DoNotShip",
            "_BackUpThisFolder_ButDontShipItWithYourGame",
        };

        internal static bool IsDoNotShipFolderName(string folderName)
        {
            if (string.IsNullOrEmpty(folderName))
                return false;

            foreach (string suffix in KnownFolderSuffixes)
            {
                if (folderName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return folderName.EndsWith("_DoNotShip", StringComparison.OrdinalIgnoreCase);
        }

        internal static IReadOnlyList<string> FindDoNotShipFolders(string buildRoot)
        {
            if (string.IsNullOrEmpty(buildRoot) || !Directory.Exists(buildRoot))
                return Array.Empty<string>();

            return Directory.GetDirectories(buildRoot)
                .Select(Path.GetFileName)
                .Where(IsDoNotShipFolderName)
                .Select(name => Path.Combine(buildRoot, name))
                .ToArray();
        }

        /// <summary>
        /// Waits for IL2CPP diagnostic folders to appear and finish writing, then deletes them.
        /// If nothing appears within <paramref name="appearTimeoutSeconds"/>, exits quickly
        /// (Mono / no diagnostics) instead of blocking for the full write timeout.
        /// </summary>
        internal static void WaitForArtifactsAndCleanup(
            string buildRoot,
            int appearTimeoutSeconds = 45,
            int writeTimeoutSeconds = 900)
        {
            if (string.IsNullOrEmpty(buildRoot) || !Directory.Exists(buildRoot))
            {
                Debug.LogWarning($"[BuildDeploy] DoNotShip cleanup skipped — folder not found: {buildRoot}");
                return;
            }

            Debug.Log($"[BuildDeploy] Waiting for IL2CPP do-not-ship folders in: {buildRoot}");

            DateTime appearDeadline = DateTime.UtcNow.AddSeconds(appearTimeoutSeconds);
            DateTime writeDeadline = DateTime.UtcNow.AddSeconds(writeTimeoutSeconds);
            bool sawDoNotShip = false;
            int lastFileCount = -1;
            int stablePolls = 0;
            const int pollsNeededForStable = 5;
            const int pollMs = 2000;

            while (true)
            {
                DateTime now = DateTime.UtcNow;
                if (!sawDoNotShip && now >= appearDeadline)
                    break;
                if (sawDoNotShip && now >= writeDeadline)
                    break;

                IReadOnlyList<string> folders = FindDoNotShipFolders(buildRoot);
                if (folders.Count > 0)
                {
                    sawDoNotShip = true;
                    int fileCount = CountFiles(folders);
                    if (fileCount == lastFileCount)
                    {
                        stablePolls++;
                        if (stablePolls >= pollsNeededForStable)
                        {
                            Debug.Log($"[BuildDeploy] Do-not-ship folders stable ({fileCount} files). Removing...");
                            break;
                        }
                    }
                    else
                    {
                        stablePolls = 0;
                        lastFileCount = fileCount;
                        Debug.Log($"[BuildDeploy] Do-not-ship folders still growing ({fileCount} files)...");
                    }
                }
                else if (sawDoNotShip)
                {
                    Debug.Log("[BuildDeploy] Do-not-ship folders already removed.");
                    return;
                }

                Thread.Sleep(pollMs);
            }

            if (!sawDoNotShip)
                Debug.Log("[BuildDeploy] No do-not-ship folders detected within appear timeout — continuing deploy.");

            RemoveDoNotShipFolders(buildRoot);
        }

        internal static void RemoveDoNotShipFolders(string buildRoot)
        {
            foreach (string folder in FindDoNotShipFolders(buildRoot))
            {
                try
                {
                    Directory.Delete(folder, recursive: true);
                    Debug.Log($"[BuildDeploy] Removed do-not-ship folder: {folder}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[BuildDeploy] Could not remove {folder}: {ex.Message}");
                }
            }
        }

        private static int CountFiles(IReadOnlyList<string> folders)
        {
            int count = 0;
            foreach (string folder in folders)
            {
                if (!Directory.Exists(folder))
                    continue;

                try
                {
                    count += Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Count();
                }
                catch
                {
                    // Folder may still be locked while Unity writes.
                }
            }

            return count;
        }
    }
}
