using System.Diagnostics;
using UnityEngine;

namespace EnglishQuest.Editor.Tools
{
    /// <summary>
    /// Single responsibility: knows how to invoke Deploy.ps1 in a visible
    /// PowerShell window.  Update only this class if the deploy interface changes.
    /// </summary>
    internal static class DeployRunner
    {
        /// <summary>
        /// Opens a visible PowerShell window and runs Deploy.ps1 with the
        /// -Dev and -DevFolder flags (equivalent to selecting option 6 in
        /// the interactive menu, then providing the folder path).
        /// Deploys to the dev/QA environment — NOT production.
        /// </summary>
        /// <param name="scriptPath">Absolute path to Deploy.ps1.</param>
        /// <param name="gameFolder">Absolute path to the built game folder.</param>
        internal static void Run(string scriptPath, string gameFolder)
        {
            // Wrap paths in quotes to handle spaces
            string args = $"-NoProfile -ExecutionPolicy Bypass -NoExit " +
                          $"-File \"{scriptPath}\" -Dev -DevFolder \"{gameFolder}\"";

            UnityEngine.Debug.Log($"[BuildDeploy] Script : {scriptPath}");
            UnityEngine.Debug.Log($"[BuildDeploy] Folder : {gameFolder}");
            UnityEngine.Debug.Log($"[BuildDeploy] Full args: powershell.exe {args}");

            var psi = new ProcessStartInfo
            {
                FileName        = "powershell.exe",
                Arguments       = args,
                UseShellExecute = true,   // opens a visible window
            };

            Process.Start(psi);
            UnityEngine.Debug.Log("[BuildDeploy] PowerShell window launched — watch for it on your taskbar.");
        }
    }
}

