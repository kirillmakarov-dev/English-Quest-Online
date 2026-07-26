using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EnglishKingdom.Editor.Tools.Git
{
    /// <summary>
    /// Manages Git skip-worktree flags for noisy tracked assets.
    /// Open via Tools → English Kingdom → Source Control → Git Skip Manager
    /// </summary>
    public class GitSkipWorktreeWindow : EditorWindow
    {
        private enum EntryStatus
        {
            Hidden,
            Visible,
            Modified,
        }

        private UnityEngine.Object _assetToAdd;
        private bool _autoIncludeMeta = true;
        private string _statusMessage;
        private bool _statusIsError;
        private string _repoRoot;
        private Vector2 _listScroll;
        private Vector2 _scanScroll;

        private HashSet<string> _skippedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _modifiedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private bool _showScanResults;
        private readonly List<string> _scannedModifiedPaths = new List<string>();
        private readonly HashSet<int> _scanSelection = new HashSet<int>();

        private static readonly Color ColorOk = new Color(0.31f, 0.78f, 0.47f);
        private static readonly Color ColorError = new Color(1.00f, 0.50f, 0.50f);
        private static readonly Color ColorHidden = new Color(0.55f, 0.75f, 1.00f);
        private static readonly Color ColorModified = new Color(1.00f, 0.78f, 0.35f);

        [MenuItem("Tools/English Kingdom/Source Control/Git Skip Manager")]
        public static void ShowWindow()
        {
            var win = GetWindow<GitSkipWorktreeWindow>("Git Skip Manager");
            win.minSize = new Vector2(560, 420);
        }

        private void OnEnable()
        {
            RefreshAll();
        }

        private void OnFocus()
        {
            RefreshAll();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawRepoInfo();
            EditorGUILayout.Space(6);
            DrawAddSection();
            EditorGUILayout.Space(6);
            DrawScanSection();
            EditorGUILayout.Space(6);
            DrawBulkActions();
            EditorGUILayout.Space(6);
            DrawManagedList();
            EditorGUILayout.Space(6);
            DrawHelpBox();
            DrawStatus();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(8);
            var header = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter,
            };
            EditorGUILayout.LabelField("Git Skip-Worktree Manager", header);
            DrawSeparator();
        }

        private void DrawRepoInfo()
        {
            if (string.IsNullOrEmpty(_repoRoot))
            {
                EditorGUILayout.HelpBox(
                    "Git repository not found. Open this project from a Git clone with git on PATH.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Repository", EditorStyles.miniBoldLabel);
            EditorGUILayout.SelectableLabel(_repoRoot, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }

        private void DrawAddSection()
        {
            EditorGUILayout.LabelField("Add Asset", EditorStyles.boldLabel);

            _autoIncludeMeta = EditorGUILayout.ToggleLeft(
                new GUIContent("Include .meta file", "Also add the companion .meta file when present."),
                _autoIncludeMeta);

            GitSkipWorktreeSettings.AutoIncludeMeta = _autoIncludeMeta;

            DrawDropZone();

            using (new EditorGUILayout.HorizontalScope())
            {
                _assetToAdd = EditorGUILayout.ObjectField("Asset", _assetToAdd, typeof(UnityEngine.Object), false);
                if (GUILayout.Button("Add", GUILayout.Width(64)))
                    AddObject(_assetToAdd);
            }
        }

        private void DrawDropZone()
        {
            Rect dropRect = GUILayoutUtility.GetRect(0, 42, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "Drag assets or folders from the Project window here", EditorStyles.helpBox);

            var evt = Event.current;
            if (!dropRect.Contains(evt.mousePosition))
                return;

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
                        AddObject(obj);
                }

                evt.Use();
            }
        }

        private void DrawScanSection()
        {
            EditorGUILayout.LabelField("Quick Add from Git", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan Modified Files", GUILayout.Height(24)))
                    ScanModifiedFiles();

                if (_showScanResults && _scannedModifiedPaths.Count > 0)
                {
                    if (GUILayout.Button("Add Selected", GUILayout.Width(100), GUILayout.Height(24)))
                        AddScannedPaths(_scanSelection.OrderBy(i => i).Select(i => _scannedModifiedPaths[i]));

                    if (GUILayout.Button("Add All", GUILayout.Width(72), GUILayout.Height(24)))
                        AddScannedPaths(_scannedModifiedPaths);
                }
            }

            if (!_showScanResults || _scannedModifiedPaths.Count == 0)
                return;

            _scanScroll = EditorGUILayout.BeginScrollView(_scanScroll, GUILayout.MaxHeight(120));
            for (int i = 0; i < _scannedModifiedPaths.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool selected = _scanSelection.Contains(i);
                    bool newSelected = EditorGUILayout.ToggleLeft(_scannedModifiedPaths[i], selected);
                    if (newSelected && !selected)
                        _scanSelection.Add(i);
                    else if (!newSelected && selected)
                        _scanSelection.Remove(i);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawBulkActions()
        {
            EditorGUILayout.LabelField("Bulk Actions", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = ColorHidden;
                if (GUILayout.Button("Apply All (Hide)", GUILayout.Height(26)))
                    ApplyAll(skip: true);

                GUI.backgroundColor = new Color(0.95f, 0.75f, 0.45f);
                if (GUILayout.Button("Undo All (Unhide)", GUILayout.Height(26)))
                    ApplyAll(skip: false);

                GUI.backgroundColor = Color.white;

                if (GUILayout.Button("Refresh", GUILayout.Height(26)))
                    RefreshAll();

                if (GUILayout.Button("Remove Missing", GUILayout.Height(26)))
                {
                    int removed = GitSkipWorktreeSettings.RemoveMissing();
                    RefreshGitState();
                    SetStatus(removed > 0 ? $"Removed {removed} missing path(s)." : "No missing paths to remove.", isError: false);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Import Suggestions", GUILayout.Height(24)))
                {
                    int count = GitSkipWorktreeSettings.ImportSuggestions(out string message);
                    RefreshAll();
                    SetStatus(message, isError: count == 0 && message != null && message.Contains("not found"));
                }

                if (GUILayout.Button("Export My List to Suggestions", GUILayout.Height(24)))
                {
                    if (EditorUtility.DisplayDialog(
                            "Export to Shared Suggestions",
                            "Overwrite the shared GitSkipSuggestions.json with your current list?\n\n" +
                            "This file is committed so teammates can import it.",
                            "Export",
                            "Cancel"))
                    {
                        GitSkipWorktreeSettings.ExportToSuggestions(out string message);
                        AssetDatabase.Refresh();
                        SetStatus(message, isError: message != null && message.StartsWith("Failed"));
                    }
                }
            }
        }

        private void DrawManagedList()
        {
            EditorGUILayout.LabelField($"Managed Assets ({GitSkipWorktreeSettings.Entries.Count})", EditorStyles.boldLabel);

            if (GitSkipWorktreeSettings.Entries.Count == 0)
            {
                EditorGUILayout.HelpBox("No assets in the list yet. Add by reference or scan modified Git files.", MessageType.Info);
                return;
            }

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < GitSkipWorktreeSettings.Entries.Count; i++)
                DrawEntryRow(i, GitSkipWorktreeSettings.Entries[i]);
            EditorGUILayout.EndScrollView();
        }

        private void DrawEntryRow(int index, GitSkipWorktreeEntry entry)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string assetPath = GitSkipWorktreeService.RepoRelativeToAssetPath(entry.RepoPath);
                    UnityEngine.Object asset = !string.IsNullOrEmpty(assetPath)
                        ? AssetDatabase.LoadMainAssetAtPath(assetPath)
                        : null;

                    var iconContent = asset != null
                        ? EditorGUIUtility.ObjectContent(asset, asset.GetType())
                        : new GUIContent(EditorGUIUtility.IconContent("DefaultAsset Icon").image, entry.RepoPath);

                    if (GUILayout.Button(iconContent, EditorStyles.label, GUILayout.Width(180)))
                    {
                        if (asset != null)
                        {
                            EditorGUIUtility.PingObject(asset);
                            Selection.activeObject = asset;
                        }
                    }

                    EditorGUILayout.LabelField(entry.RepoPath, EditorStyles.miniLabel);

                    EntryStatus status = GetEntryStatus(entry.RepoPath);
                    DrawStatusBadge(status);

                    if (GUILayout.Button("Hide", GUILayout.Width(48)))
                        SetSkipForEntry(entry.RepoPath, skip: true);

                    if (GUILayout.Button("Unhide", GUILayout.Width(56)))
                        SetSkipForEntry(entry.RepoPath, skip: false);

                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        GitSkipWorktreeSettings.RemoveAt(index);
                        RefreshGitState();
                        SetStatus($"Removed from list:\n{entry.RepoPath}", isError: false);
                    }
                }
            }
        }

        private void DrawStatusBadge(EntryStatus status)
        {
            Color previous = GUI.color;
            string label = status.ToString();
            GUI.color = status switch
            {
                EntryStatus.Hidden => ColorHidden,
                EntryStatus.Modified => ColorModified,
                _ => Color.white,
            };
            GUILayout.Label(label, EditorStyles.miniBoldLabel, GUILayout.Width(64));
            GUI.color = previous;
        }

        private void DrawHelpBox()
        {
            EditorGUILayout.HelpBox(
                "Skip-worktree is local only — it hides tracked files from your git status without removing them from the repo.\n\n" +
                "When a teammate changes a hidden file: Unhide → pull/merge → fix if needed → Apply All again.\n\n" +
                $"Shared suggestions: {GitSkipWorktreeSettings.SharedSuggestionsFilePath}",
                MessageType.None);
        }

        private void DrawStatus()
        {
            if (string.IsNullOrEmpty(_statusMessage))
                return;

            EditorGUILayout.Space(4);
            GUI.color = _statusIsError ? ColorError : ColorOk;
            EditorGUILayout.HelpBox(_statusMessage, _statusIsError ? MessageType.Error : MessageType.Info);
            GUI.color = Color.white;
        }

        private static void DrawSeparator()
        {
            EditorGUILayout.Space(4);
            Rect r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, new Color(0.35f, 0.35f, 0.35f));
            EditorGUILayout.Space(4);
        }

        private void RefreshAll()
        {
            GitSkipWorktreeService.ClearCache();
            GitSkipWorktreeSettings.Load();
            _autoIncludeMeta = GitSkipWorktreeSettings.AutoIncludeMeta;

            if (!GitSkipWorktreeService.TryGetRepositoryRoot(out _repoRoot, out string error))
            {
                _repoRoot = null;
                if (!string.IsNullOrEmpty(error))
                    SetStatus(error, isError: true);
            }

            RefreshGitState();
        }

        private void RefreshGitState()
        {
            _skippedPaths = GitSkipWorktreeService.GetSkipWorktreePaths();
            _modifiedPaths = GitSkipWorktreeService.GetModifiedRepoPaths();
            Repaint();
        }

        private EntryStatus GetEntryStatus(string repoPath)
        {
            if (_skippedPaths.Contains(repoPath))
                return EntryStatus.Hidden;

            if (_modifiedPaths.Contains(repoPath))
                return EntryStatus.Modified;

            return EntryStatus.Visible;
        }

        private void AddObject(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                SetStatus("Select an asset to add.", isError: true);
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(assetPath))
            {
                SetStatus("Selected object is not a project asset.", isError: true);
                return;
            }

            if (AssetDatabase.IsValidFolder(assetPath))
                AddFolder(assetPath);
            else
                AddAssetPath(assetPath);
        }

        private void AddFolder(string assetFolderPath)
        {
            var tracked = GitSkipWorktreeService.GetTrackedFilesUnderAssetFolder(assetFolderPath);
            if (tracked.Count == 0)
            {
                SetStatus($"No tracked Git files found under:\n{assetFolderPath}", isError: true);
                return;
            }

            if (tracked.Count > GitSkipWorktreeService.FolderAddConfirmThreshold)
            {
                if (!EditorUtility.DisplayDialog(
                        "Add Folder",
                        $"Add {tracked.Count} tracked files from this folder?",
                        "Add All",
                        "Cancel"))
                    return;
            }

            int added = 0;
            foreach (string repoPath in tracked)
            {
                if (GitSkipWorktreeSettings.AddRepoPath(repoPath, includeMeta: false, out _))
                    added++;
            }

            RefreshGitState();
            SetStatus(added > 0 ? $"Added {added} tracked file(s) from folder." : "No new files were added.", isError: added == 0);
        }

        private void AddAssetPath(string assetPath)
        {
            if (!GitSkipWorktreeService.TryAssetPathToRepoRelative(assetPath, out string repoPath, out string error))
            {
                SetStatus(error, isError: true);
                return;
            }

            if (GitSkipWorktreeSettings.AddRepoPath(repoPath, _autoIncludeMeta, out string addMessage))
            {
                RefreshGitState();
                SetStatus($"Added:\n{repoPath}", isError: false);
                _assetToAdd = null;
            }
            else
            {
                SetStatus(addMessage, isError: true);
            }
        }

        private void ScanModifiedFiles()
        {
            _scannedModifiedPaths.Clear();
            _scanSelection.Clear();
            _showScanResults = true;

            foreach (string path in GitSkipWorktreeService.GetModifiedRepoPaths().OrderBy(p => p))
                _scannedModifiedPaths.Add(path);

            for (int i = 0; i < _scannedModifiedPaths.Count; i++)
                _scanSelection.Add(i);

            SetStatus(
                _scannedModifiedPaths.Count > 0
                    ? $"Found {_scannedModifiedPaths.Count} modified file(s) in git status."
                    : "No modified files in git status.",
                isError: false);
        }

        private void AddScannedPaths(IEnumerable<string> paths)
        {
            int added = 0;
            foreach (string repoPath in paths)
            {
                if (GitSkipWorktreeSettings.AddRepoPath(repoPath, _autoIncludeMeta, out _))
                    added++;
            }

            RefreshGitState();
            SetStatus(added > 0 ? $"Added {added} path(s) from scan." : "No new paths were added.", isError: added == 0);
        }

        private void ApplyAll(bool skip)
        {
            if (GitSkipWorktreeSettings.Entries.Count == 0)
            {
                SetStatus("List is empty.", isError: true);
                return;
            }

            int success = 0;
            string lastError = null;

            foreach (var entry in GitSkipWorktreeSettings.Entries)
            {
                if (TrySetSkip(entry.RepoPath, skip, out string error))
                    success++;
                else
                    lastError = error;
            }

            RefreshGitState();
            string action = skip ? "hidden" : "unhidden";
            string message = $"{success} path(s) {action}.";
            if (!string.IsNullOrEmpty(lastError) && success < GitSkipWorktreeSettings.Entries.Count)
                message += $"\nLast error:\n{lastError}";
            SetStatus(message, isError: success == 0);
        }

        private void SetSkipForEntry(string repoPath, bool skip)
        {
            if (TrySetSkip(repoPath, skip, out string error))
            {
                RefreshGitState();
                SetStatus(skip ? $"Hidden:\n{repoPath}" : $"Unhidden:\n{repoPath}", isError: false);
            }
            else
            {
                SetStatus(error, isError: true);
            }
        }

        private bool TrySetSkip(string repoPath, bool skip, out string error)
        {
            error = null;
            if (!GitSkipWorktreeService.IsTracked(repoPath, out string trackError))
            {
                error = string.IsNullOrWhiteSpace(trackError)
                    ? $"Not tracked by Git:\n{repoPath}"
                    : trackError;
                return false;
            }

            var result = GitSkipWorktreeService.SetSkipWorktree(repoPath, skip);
            if (result.TimedOut)
            {
                error = "Git timed out. Try again.";
                return false;
            }

            if (!result.Success)
            {
                error = string.IsNullOrWhiteSpace(result.Error) ? "Git command failed." : result.Error.Trim();
                return false;
            }

            return true;
        }

        private void SetStatus(string message, bool isError)
        {
            _statusMessage = message;
            _statusIsError = isError;
            Repaint();
        }
    }
}
