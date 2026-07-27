using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

namespace EnglishQuest.Editor.Tools.Git
{
    internal static class GitSkipWorktreeService
    {
        internal const int CommandTimeoutMs = 10_000;
        internal const int FolderAddConfirmThreshold = 50;

        private static string _cachedRepoRoot;
        private static string _cachedUnityProjectFolderName;

        internal readonly struct GitCommandResult
        {
            public bool Success { get; }
            public string Output { get; }
            public string Error { get; }
            public bool TimedOut { get; }

            public GitCommandResult(bool success, string output, string error, bool timedOut)
            {
                Success = success;
                Output = output ?? string.Empty;
                Error = error ?? string.Empty;
                TimedOut = timedOut;
            }
        }

        internal static string GetUnityProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
                .Replace('\\', '/');
        }

        internal static bool TryGetRepositoryRoot(out string repoRoot, out string error)
        {
            if (!string.IsNullOrEmpty(_cachedRepoRoot))
            {
                repoRoot = _cachedRepoRoot;
                error = null;
                return true;
            }

            var result = RunGitCore("rev-parse", "--show-toplevel", useRepositoryRoot: false);
            if (result.TimedOut)
            {
                repoRoot = null;
                error = "Git timed out while locating the repository root.";
                return false;
            }

            if (!result.Success || string.IsNullOrWhiteSpace(result.Output))
            {
                repoRoot = null;
                error = string.IsNullOrWhiteSpace(result.Error)
                    ? "This Unity project is not inside a Git repository."
                    : result.Error.Trim();
                return false;
            }

            _cachedRepoRoot = result.Output.Trim().Replace('\\', '/');
            repoRoot = _cachedRepoRoot;
            error = null;
            return true;
        }

        internal static void ClearCache()
        {
            _cachedRepoRoot = null;
            _cachedUnityProjectFolderName = null;
        }

        internal static bool TryAssetPathToRepoRelative(string assetPath, out string repoRelativePath, out string error)
        {
            repoRelativePath = null;
            error = null;

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                error = "Asset path is empty.";
                return false;
            }

            assetPath = assetPath.Replace('\\', '/');

            if (!TryGetRepositoryRoot(out string repoRoot, out error))
                return false;

            string projectRoot = GetUnityProjectRoot();
            string fullPath = assetPath.StartsWith("Assets/", StringComparison.Ordinal)
                ? Path.GetFullPath(Path.Combine(projectRoot, assetPath))
                : Path.GetFullPath(assetPath);

            fullPath = fullPath.Replace('\\', '/');
            repoRoot = repoRoot.Replace('\\', '/');

            if (!fullPath.StartsWith(repoRoot, StringComparison.OrdinalIgnoreCase))
            {
                error = $"Asset is outside the Git repository:\n{assetPath}";
                return false;
            }

            repoRelativePath = fullPath.Substring(repoRoot.Length).TrimStart('/');
            return true;
        }

        internal static string RepoRelativeToAssetPath(string repoRelativePath)
        {
            if (string.IsNullOrWhiteSpace(repoRelativePath))
                return null;

            repoRelativePath = repoRelativePath.Replace('\\', '/');
            string projectFolder = GetUnityProjectFolderName(repoRelativePath);

            if (string.IsNullOrEmpty(projectFolder))
                return null;

            string prefix = projectFolder + "/";
            if (!repoRelativePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return null;

            string underProject = repoRelativePath.Substring(prefix.Length);
            return underProject.StartsWith("Assets/", StringComparison.Ordinal) ? underProject : null;
        }

        internal static GitCommandResult SetSkipWorktree(string repoRelativePath, bool skip)
        {
            repoRelativePath = NormalizeRepoPath(repoRelativePath);
            string flag = skip ? "--skip-worktree" : "--no-skip-worktree";
            return RunGit("update-index", $"{flag} \"{repoRelativePath}\"");
        }

        internal static bool IsTracked(string repoRelativePath, out string error)
        {
            repoRelativePath = NormalizeRepoPath(repoRelativePath);
            var result = RunGit("ls-files", $"--error-unmatch \"{repoRelativePath}\"");
            if (result.TimedOut)
            {
                error = "Git timed out while checking if the file is tracked.";
                return false;
            }

            error = result.Success ? null : result.Error.Trim();
            return result.Success;
        }

        internal static HashSet<string> GetSkipWorktreePaths()
        {
            var skipped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = RunGit("ls-files", "-v");
            if (!result.Success)
                return skipped;

            foreach (string line in result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Length < 3 || line[0] != 'S')
                    continue;

                skipped.Add(NormalizeRepoPath(line.Substring(2).Trim()));
            }

            return skipped;
        }

        internal static HashSet<string> GetModifiedRepoPaths()
        {
            var modified = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = RunGit("status", "--porcelain");
            if (!result.Success)
                return modified;

            foreach (string line in result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Length < 4)
                    continue;

                string path = line.Substring(3).Trim();
                int arrow = path.IndexOf(" -> ", StringComparison.Ordinal);
                if (arrow >= 0)
                    path = path.Substring(arrow + 4);

                modified.Add(NormalizeRepoPath(path));
            }

            return modified;
        }

        internal static List<string> GetTrackedFilesUnderAssetFolder(string assetFolderPath)
        {
            var paths = new List<string>();
            if (string.IsNullOrWhiteSpace(assetFolderPath))
                return paths;

            assetFolderPath = assetFolderPath.Replace('\\', '/').TrimEnd('/');
            if (!assetFolderPath.StartsWith("Assets/", StringComparison.Ordinal))
                return paths;

            if (!TryGetRepositoryRoot(out _, out _))
                return paths;

            string projectFolder = GetUnityProjectFolderNameFromProjectRoot();
            if (string.IsNullOrEmpty(projectFolder))
                return paths;

            string repoPrefix = $"{projectFolder}/{assetFolderPath}";
            var result = RunGit("ls-files", $"\"{repoPrefix}\"");
            if (!result.Success)
                return paths;

            foreach (string line in result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    paths.Add(NormalizeRepoPath(line));
            }

            if (paths.Count == 0 && TryAssetPathToRepoRelative(assetFolderPath, out string folderRepoPath, out _))
            {
                result = RunGit("ls-files", $"\"{folderRepoPath}/\"");
                if (result.Success)
                {
                    foreach (string line in result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                            paths.Add(NormalizeRepoPath(line));
                    }
                }
            }

            return paths;
        }

        internal static string GetMetaRepoPath(string repoRelativePath)
        {
            repoRelativePath = NormalizeRepoPath(repoRelativePath);
            if (repoRelativePath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                return null;

            string metaPath = repoRelativePath + ".meta";
            if (!IsTracked(metaPath, out _))
                return null;

            return metaPath;
        }

        internal static GitCommandResult RunGit(string command, string arguments)
        {
            return RunGitCore(command, arguments, useRepositoryRoot: true);
        }

        private static GitCommandResult RunGitCore(string command, string arguments, bool useRepositoryRoot)
        {
            Process process = null;
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = command + " " + arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                };

                if (useRepositoryRoot && !string.IsNullOrEmpty(_cachedRepoRoot))
                    startInfo.WorkingDirectory = _cachedRepoRoot;

                process = new Process { StartInfo = startInfo };

                process.Start();
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();

                if (!process.WaitForExit(CommandTimeoutMs))
                {
                    try { process.Kill(); } catch { /* ignored */ }
                    return new GitCommandResult(false, stdout, stderr, timedOut: true);
                }

                bool success = process.ExitCode == 0;
                return new GitCommandResult(success, stdout, stderr, timedOut: false);
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 2)
            {
                return new GitCommandResult(false, null, "Git executable was not found on PATH.", timedOut: false);
            }
            catch (Exception ex)
            {
                return new GitCommandResult(false, null, ex.Message, timedOut: false);
            }
            finally
            {
                process?.Dispose();
            }
        }

        private static string NormalizeRepoPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            path = path.Replace('\\', '/').Trim();
            if (path.Length >= 2 && path[0] == '"' && path[path.Length - 1] == '"')
                path = path.Substring(1, path.Length - 2);

            return path;
        }

        private static string GetUnityProjectFolderName(string repoRelativePath)
        {
            if (!string.IsNullOrEmpty(_cachedUnityProjectFolderName))
                return _cachedUnityProjectFolderName;

            string fromRoot = GetUnityProjectFolderNameFromProjectRoot();
            if (!string.IsNullOrEmpty(fromRoot))
                return fromRoot;

            int slash = repoRelativePath.IndexOf('/');
            return slash > 0 ? repoRelativePath.Substring(0, slash) : null;
        }

        private static string GetUnityProjectFolderNameFromProjectRoot()
        {
            if (!string.IsNullOrEmpty(_cachedUnityProjectFolderName))
                return _cachedUnityProjectFolderName;

            if (!TryGetRepositoryRoot(out string repoRoot, out _))
                return null;

            string projectRoot = GetUnityProjectRoot();
            if (!projectRoot.StartsWith(repoRoot, StringComparison.OrdinalIgnoreCase))
                return null;

            string relative = projectRoot.Substring(repoRoot.Length).TrimStart('/', '\\');
            int slash = relative.IndexOf('/');
            _cachedUnityProjectFolderName = slash >= 0 ? relative.Substring(0, slash) : relative;
            return _cachedUnityProjectFolderName;
        }
    }
}

