using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EnglishKingdom.Editor.Tools.Git
{
    [Serializable]
    internal class GitSkipEntryDto
    {
        public string repoPath;
    }

    [Serializable]
    internal class GitSkipWorktreeFileDto
    {
        public GitSkipEntryDto[] entries = Array.Empty<GitSkipEntryDto>();
        public bool autoIncludeMeta = true;
    }

    internal class GitSkipWorktreeEntry
    {
        public string RepoPath;

        public GitSkipWorktreeEntry(string repoPath)
        {
            RepoPath = repoPath;
        }
    }

    internal static class GitSkipWorktreeSettings
    {
        private static readonly string UserFilePath = Path.Combine(
            GitSkipWorktreeService.GetUnityProjectRoot(),
            "UserSettings",
            "GitSkipWorktree.json");

        private static readonly string SharedSuggestionsPath = Path.Combine(
            Application.dataPath,
            "_OurAssets",
            "Scripts",
            "Editor",
            "EditorConfig",
            "GitSkipSuggestions.json");

        internal static string SharedSuggestionsFilePath => SharedSuggestionsPath.Replace('\\', '/');

        internal static bool AutoIncludeMeta { get; set; } = true;

        internal static List<GitSkipWorktreeEntry> Entries { get; private set; } = new List<GitSkipWorktreeEntry>();

        internal static void Load()
        {
            AutoIncludeMeta = true;
            Entries = new List<GitSkipWorktreeEntry>();

            if (!File.Exists(UserFilePath))
                return;

            try
            {
                string json = File.ReadAllText(UserFilePath);
                var data = JsonUtility.FromJson<GitSkipWorktreeFileDto>(json);
                if (data == null)
                    return;

                AutoIncludeMeta = data.autoIncludeMeta;
                if (data.entries == null)
                    return;

                foreach (var entry in data.entries)
                {
                    if (string.IsNullOrWhiteSpace(entry?.repoPath))
                        continue;

                    string path = entry.repoPath.Replace('\\', '/').Trim();
                    if (!Entries.Any(e => string.Equals(e.RepoPath, path, StringComparison.OrdinalIgnoreCase)))
                        Entries.Add(new GitSkipWorktreeEntry(path));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GitSkipWorktree] Failed to load user settings: {ex.Message}");
            }
        }

        internal static void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(UserFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var data = new GitSkipWorktreeFileDto
                {
                    autoIncludeMeta = AutoIncludeMeta,
                    entries = Entries
                        .Select(e => new GitSkipEntryDto { repoPath = e.RepoPath })
                        .ToArray(),
                };

                File.WriteAllText(UserFilePath, JsonUtility.ToJson(data, prettyPrint: true));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GitSkipWorktree] Failed to save user settings: {ex.Message}");
            }
        }

        internal static bool AddRepoPath(string repoPath, bool includeMeta, out string message)
        {
            message = null;
            repoPath = repoPath?.Replace('\\', '/').Trim();
            if (string.IsNullOrWhiteSpace(repoPath))
            {
                message = "Path is empty.";
                return false;
            }

            if (!GitSkipWorktreeService.IsTracked(repoPath, out string trackError))
            {
                message = string.IsNullOrWhiteSpace(trackError)
                    ? $"Not tracked by Git:\n{repoPath}"
                    : trackError;
                return false;
            }

            bool added = TryAddUnique(repoPath);

            if (includeMeta)
            {
                string metaPath = GitSkipWorktreeService.GetMetaRepoPath(repoPath);
                if (!string.IsNullOrEmpty(metaPath))
                    added |= TryAddUnique(metaPath);
            }

            if (!added)
            {
                message = "Path is already in the list.";
                return false;
            }

            Save();
            return true;
        }

        internal static int ImportSuggestions(out string message)
        {
            message = null;
            if (!File.Exists(SharedSuggestionsPath))
            {
                message = $"Shared suggestions file not found:\n{SharedSuggestionsFilePath}";
                return 0;
            }

            try
            {
                string json = File.ReadAllText(SharedSuggestionsPath);
                var data = JsonUtility.FromJson<GitSkipWorktreeFileDto>(json);
                if (data?.entries == null || data.entries.Length == 0)
                {
                    message = "Shared suggestions file is empty.";
                    return 0;
                }

                int imported = 0;
                foreach (var entry in data.entries)
                {
                    if (string.IsNullOrWhiteSpace(entry?.repoPath))
                        continue;

                    if (AddRepoPath(entry.repoPath, includeMeta: false, out _))
                        imported++;
                }

                message = imported > 0
                    ? $"Imported {imported} suggestion(s)."
                    : "No new suggestions to import.";
                return imported;
            }
            catch (Exception ex)
            {
                message = $"Failed to import suggestions: {ex.Message}";
                return 0;
            }
        }

        internal static bool ExportToSuggestions(out string message)
        {
            message = null;
            try
            {
                string directory = Path.GetDirectoryName(SharedSuggestionsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var data = new GitSkipWorktreeFileDto
                {
                    autoIncludeMeta = AutoIncludeMeta,
                    entries = Entries
                        .Select(e => new GitSkipEntryDto { repoPath = e.RepoPath })
                        .ToArray(),
                };

                File.WriteAllText(SharedSuggestionsPath, JsonUtility.ToJson(data, prettyPrint: true));
                message = $"Exported {Entries.Count} path(s) to shared suggestions.";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Failed to export suggestions: {ex.Message}";
                return false;
            }
        }

        internal static void RemoveAt(int index)
        {
            if (index < 0 || index >= Entries.Count)
                return;

            Entries.RemoveAt(index);
            Save();
        }

        internal static int RemoveMissing()
        {
            int removed = Entries.RemoveAll(e => !GitSkipWorktreeService.IsTracked(e.RepoPath, out _));
            if (removed > 0)
                Save();
            return removed;
        }

        private static bool TryAddUnique(string repoPath)
        {
            if (Entries.Any(e => string.Equals(e.RepoPath, repoPath, StringComparison.OrdinalIgnoreCase)))
                return false;

            Entries.Add(new GitSkipWorktreeEntry(repoPath));
            return true;
        }
    }
}
