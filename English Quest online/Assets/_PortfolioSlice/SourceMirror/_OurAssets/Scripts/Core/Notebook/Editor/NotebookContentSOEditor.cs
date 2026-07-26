using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

[CustomEditor(typeof(NotebookContentSO))]
public class NotebookContentSOEditor : Editor
{
    private const string PrefKey = "NotebookContentSOEditor_SelectedFolder";
    private string _selectedFolder;

    private void OnEnable()
    {
        _selectedFolder = EditorPrefs.GetString(PrefKey, "Assets/_OurAssets/Art/Sprites/Notebooks");
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Auto-Fill Tool", EditorStyles.boldLabel);

        // Folder picker row
        EditorGUILayout.BeginHorizontal();
        _selectedFolder = EditorGUILayout.TextField("Sprites Folder", _selectedFolder);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string absolute = EditorUtility.OpenFolderPanel(
                "Select Sprites Folder",
                Path.Combine(Application.dataPath, "../" + _selectedFolder),
                "");

            if (!string.IsNullOrEmpty(absolute))
            {
                // Convert absolute path back to Assets-relative
                string relative = "Assets" + absolute.Replace(Application.dataPath, "").Replace("\\", "/");
                _selectedFolder = relative;
                EditorPrefs.SetString(PrefKey, _selectedFolder);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "Each subfolder → one topic (name = folder name).\n" +
            "All sprites inside (including sliced sheets), sorted by name → pages.\n" +
            "First sprite → thumbnail.",
            MessageType.Info);

        EditorGUI.BeginDisabledGroup(!AssetDatabase.IsValidFolder(_selectedFolder));
        if (GUILayout.Button("Auto-Fill Topics From Selected Folder", GUILayout.Height(36)))
        {
            EditorPrefs.SetString(PrefKey, _selectedFolder);
            AutoFill((NotebookContentSO)target, _selectedFolder);
        }
        EditorGUI.EndDisabledGroup();

        if (!AssetDatabase.IsValidFolder(_selectedFolder))
            EditorGUILayout.HelpBox("Selected folder does not exist in the project.", MessageType.Warning);
    }

    private static void AutoFill(NotebookContentSO so, string rootFolder)
    {
        // Each PNG in the folder is a sprite sheet sliced into 4 sprites:
        //   _1 = thumbnail
        //   _0, _3, _2 = pages in that fixed order

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { rootFolder });

        // Load all slices and group by parent PNG name (strip trailing _N suffix)
        var allSlices = guids
            .SelectMany(guid =>
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>();
            })
            .Distinct()
            .ToList();

        // Group by parent name: "Animals-4 - Farm Amimals_0" → key "Animals-4 - Farm Amimals"
        var groups = allSlices
            .GroupBy(s => System.Text.RegularExpressions.Regex.Replace(s.name, @"_\d+$", ""))
            .OrderBy(g => g.Key)
            .ToList();

        var topics = new List<NotebookTopic>();

        foreach (var group in groups)
        {
            var sliceDict = group.ToDictionary(s =>
            {
                var match = System.Text.RegularExpressions.Regex.Match(s.name, @"_(\d+)$");
                return match.Success ? int.Parse(match.Groups[1].Value) : -1;
            });

            if (!sliceDict.TryGetValue(1, out var thumbnail))
            {
                AppLog.Warning($"[NotebookTool] '{group.Key}': missing _1 slice (thumbnail) — skipping.");
                continue;
            }

            // Fixed page order: _0, _3, _2
            var pages = new List<Sprite>();
            foreach (int idx in new[] { 0, 3, 2 })
            {
                if (sliceDict.TryGetValue(idx, out var page))
                    pages.Add(page);
                else
                    AppLog.Warning($"[NotebookTool] '{group.Key}': missing _{idx} slice.");
            }

            topics.Add(new NotebookTopic
            {
                topicName = group.Key,
                thumnail  = thumbnail,
                pages     = pages
            });

            AppLog.Info($"[NotebookTool] '{group.Key}': thumbnail=_1, {pages.Count} pages (_0,_3,_2).");
        }

        Undo.RecordObject(so, "Auto-Fill Notebook Topics");
        so.notebookTopics = topics;
        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();

        AppLog.Info($"[NotebookTool] Done — {topics.Count} topics filled into {so.name}.");
    }
}

#endif
