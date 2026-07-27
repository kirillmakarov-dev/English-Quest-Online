using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace EnglishQuest.Editor.Tools.Profiler
{
    /// <summary>
    /// Exports the currently selected Profiler frame to JSON or Markdown for AI analysis.
    /// Open via Tools → English Kingdom → Profiler → Frame Export Settings
    /// </summary>
    internal sealed class ProfilerFrameExportWindow : EditorWindow
    {
        private Vector2 _scroll;
        private string _statusMessage;
        private bool _statusIsError;

        [MenuItem("Tools/English Kingdom/Profiler/Frame Export Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<ProfilerFrameExportWindow>("Profiler Export");
            window.minSize = new Vector2(520, 640);
        }

        private void OnFocus() => Repaint();

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawHeader();
            EditorGUILayout.Space(6);
            DrawFrameSection();
            EditorGUILayout.Space(8);
            DrawOutputSection();
            EditorGUILayout.Space(8);
            DrawContentSection();
            EditorGUILayout.Space(8);
            DrawSampleDetailSection();
            EditorGUILayout.Space(8);
            DrawFilterSection();
            EditorGUILayout.Space(12);
            DrawActions();
            DrawStatus();

            EditorGUILayout.EndScrollView();
        }

        private static void DrawHeader()
        {
            EditorGUILayout.Space(8);
            var header = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Profiler Frame Export", header);
            EditorGUILayout.LabelField(
                "Export a Profiler frame with hierarchy and timeline details for AI analysis.",
                EditorStyles.centeredGreyMiniLabel);
            DrawSeparator();
        }

        private void DrawFrameSection()
        {
            EditorGUILayout.LabelField("Frame Source", EditorStyles.boldLabel);

            var source = (ProfilerFrameSource)EditorGUILayout.EnumPopup("Source", ProfilerFrameExportOptions.FrameSource);
            if (source != ProfilerFrameExportOptions.FrameSource)
                ProfilerFrameExportOptions.FrameSource = source;

            if (ProfilerFrameExportOptions.FrameSource == ProfilerFrameSource.ProfilerSelected)
            {
                if (ProfilerFrameExporter.TryResolveFrameIndex(out int frameIndex, out _))
                {
                    EditorGUILayout.HelpBox(
                        $"Will export Profiler-selected frame {frameIndex} " +
                        $"(UI shows frame {frameIndex + 1}).",
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Open the Profiler window, load or record data, and select a frame.",
                        MessageType.Warning);
                }
            }
            else
            {
                int manual = EditorGUILayout.IntField("Frame Index", ProfilerFrameExportOptions.ManualFrameIndex);
                if (manual != ProfilerFrameExportOptions.ManualFrameIndex)
                    ProfilerFrameExportOptions.ManualFrameIndex = manual;

                if (ProfilerDriver.lastFrameIndex >= 0)
                {
                    EditorGUILayout.LabelField(
                        "Available range",
                        $"{ProfilerDriver.firstFrameIndex} – {ProfilerDriver.lastFrameIndex}");
                }
            }
        }

        private void DrawOutputSection()
        {
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

            var format = (ProfilerFrameExportFormat)EditorGUILayout.EnumPopup("Format", ProfilerFrameExportOptions.Format);
            if (format != ProfilerFrameExportOptions.Format)
                ProfilerFrameExportOptions.Format = format;

            EditorGUILayout.BeginHorizontal();
            string folder = EditorGUILayout.TextField("Folder", ProfilerFrameExportOptions.OutputFolder);
            if (folder != ProfilerFrameExportOptions.OutputFolder)
                ProfilerFrameExportOptions.OutputFolder = folder;

            if (GUILayout.Button("...", GUILayout.Width(28)))
            {
                string selected = EditorUtility.OpenFolderPanel("Profiler export folder", ResolveFolderForPicker(), "");
                if (!string.IsNullOrEmpty(selected))
                    ProfilerFrameExportOptions.OutputFolder = selected;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "JSON is best for AI tools. Markdown is easier to skim manually. " +
                "Exports open in the file browser when complete.",
                MessageType.None);
        }

        private void DrawContentSection()
        {
            EditorGUILayout.LabelField("Sections", EditorStyles.boldLabel);
            DrawToggle("Frame summary", () => ProfilerFrameExportOptions.IncludeFrameSummary, v => ProfilerFrameExportOptions.IncludeFrameSummary = v);
            DrawToggle("Profiler categories", () => ProfilerFrameExportOptions.IncludeCategories, v => ProfilerFrameExportOptions.IncludeCategories = v);
            DrawToggle("Top expensive samples", () => ProfilerFrameExportOptions.IncludeTopExpensiveSamples, v => ProfilerFrameExportOptions.IncludeTopExpensiveSamples = v);

            if (ProfilerFrameExportOptions.IncludeTopExpensiveSamples)
            {
                EditorGUI.indentLevel++;
                int topCount = EditorGUILayout.IntSlider(
                    "Top sample count",
                    ProfilerFrameExportOptions.TopExpensiveSampleCount,
                    5,
                    100);
                if (topCount != ProfilerFrameExportOptions.TopExpensiveSampleCount)
                    ProfilerFrameExportOptions.TopExpensiveSampleCount = topCount;
                EditorGUI.indentLevel--;
            }

            DrawToggle("Hierarchy (CPU tree)", () => ProfilerFrameExportOptions.IncludeHierarchy, v => ProfilerFrameExportOptions.IncludeHierarchy = v);
            DrawToggle("Timeline (raw samples)", () => ProfilerFrameExportOptions.IncludeTimeline, v => ProfilerFrameExportOptions.IncludeTimeline = v);
            DrawToggle("All threads", () => ProfilerFrameExportOptions.IncludeAllThreads, v => ProfilerFrameExportOptions.IncludeAllThreads = v);
        }

        private void DrawSampleDetailSection()
        {
            EditorGUILayout.LabelField("Sample Fields", EditorStyles.boldLabel);
            DrawToggle("Sample path", () => ProfilerFrameExportOptions.IncludeSamplePath, v => ProfilerFrameExportOptions.IncludeSamplePath = v);
            DrawToggle("Timing (self/total ms & %)", () => ProfilerFrameExportOptions.IncludeTimingColumns, v => ProfilerFrameExportOptions.IncludeTimingColumns = v);
            DrawToggle("GC memory", () => ProfilerFrameExportOptions.IncludeGcMemory, v => ProfilerFrameExportOptions.IncludeGcMemory = v);
            DrawToggle("Call count", () => ProfilerFrameExportOptions.IncludeCalls, v => ProfilerFrameExportOptions.IncludeCalls = v);
            DrawToggle("Category", () => ProfilerFrameExportOptions.IncludeCategory, v => ProfilerFrameExportOptions.IncludeCategory = v);
            DrawToggle("Unity object info", () => ProfilerFrameExportOptions.IncludeObjectInfo, v => ProfilerFrameExportOptions.IncludeObjectInfo = v);
            DrawToggle("Sample metadata", () => ProfilerFrameExportOptions.IncludeMetadata, v => ProfilerFrameExportOptions.IncludeMetadata = v);
            DrawToggle("Callstacks", () => ProfilerFrameExportOptions.IncludeCallstacks, v => ProfilerFrameExportOptions.IncludeCallstacks = v);

            if (ProfilerFrameExportOptions.IncludeCallstacks)
            {
                EditorGUILayout.HelpBox(
                    "Callstacks require callstack collection in the Profiler and can greatly increase file size.",
                    MessageType.Info);
            }
        }

        private void DrawFilterSection()
        {
            EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);

            float minSelf = EditorGUILayout.FloatField("Min self/duration (ms)", ProfilerFrameExportOptions.MinSelfTimeMs);
            if (!Mathf.Approximately(minSelf, ProfilerFrameExportOptions.MinSelfTimeMs))
                ProfilerFrameExportOptions.MinSelfTimeMs = Mathf.Max(0f, minSelf);

            int maxDepth = EditorGUILayout.IntField("Max hierarchy depth (0 = unlimited)", ProfilerFrameExportOptions.MaxHierarchyDepth);
            if (maxDepth != ProfilerFrameExportOptions.MaxHierarchyDepth)
                ProfilerFrameExportOptions.MaxHierarchyDepth = Mathf.Max(0, maxDepth);
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Export Selected Frame", GUILayout.Height(30), GUILayout.Width(180)))
                Export(showDialogOnError: true);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatus()
        {
            if (string.IsNullOrEmpty(_statusMessage))
                return;

            EditorGUILayout.Space(6);
            var style = new GUIStyle(EditorStyles.helpBox)
            {
                wordWrap = true,
                normal = { textColor = _statusIsError ? new Color(1f, 0.45f, 0.45f) : new Color(0.35f, 0.85f, 0.45f) }
            };
            EditorGUILayout.LabelField(_statusMessage, style);
        }

        private void Export(bool showDialogOnError)
        {
            var result = ProfilerFrameExporter.ExportCurrentOptions();
            _statusIsError = !result.Success;
            _statusMessage = result.Success
                ? $"Exported to:\n{result.FilePath}"
                : result.Message;

            if (result.Success)
                Debug.Log($"Profiler frame exported to: {result.FilePath}");
            else if (showDialogOnError)
                EditorUtility.DisplayDialog("Profiler Frame Export", result.Message, "OK");
        }

        private static void DrawToggle(string label, System.Func<bool> getter, System.Action<bool> setter)
        {
            EditorGUI.BeginChangeCheck();
            bool value = EditorGUILayout.ToggleLeft(label, getter());
            if (EditorGUI.EndChangeCheck())
                setter(value);
        }

        private static string ResolveFolderForPicker()
        {
            string configured = ProfilerFrameExportOptions.OutputFolder;
            if (string.IsNullOrWhiteSpace(configured))
                return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ProfilerExports"));

            return Path.IsPathRooted(configured)
                ? configured
                : Path.GetFullPath(Path.Combine(Application.dataPath, "..", configured));
        }

        private static void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.35f));
        }
    }
}

