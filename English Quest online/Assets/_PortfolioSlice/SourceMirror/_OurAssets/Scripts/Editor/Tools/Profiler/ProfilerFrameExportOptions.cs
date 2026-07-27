using UnityEditor;

namespace EnglishQuest.Editor.Tools.Profiler
{
    internal enum ProfilerFrameExportFormat
    {
        Json,
        Markdown
    }

    internal enum ProfilerFrameSource
    {
        ProfilerSelected,
        ManualIndex
    }

    /// <summary>
    /// EditorPrefs-backed options for profiler frame export.
    /// </summary>
    internal static class ProfilerFrameExportOptions
    {
        private const string Prefix = "EK.ProfilerFrameExport.";

        internal static ProfilerFrameSource FrameSource
        {
            get => (ProfilerFrameSource)EditorPrefs.GetInt(Prefix + "FrameSource", (int)ProfilerFrameSource.ProfilerSelected);
            set => EditorPrefs.SetInt(Prefix + "FrameSource", (int)value);
        }

        internal static int ManualFrameIndex
        {
            get => EditorPrefs.GetInt(Prefix + "ManualFrameIndex", 0);
            set => EditorPrefs.SetInt(Prefix + "ManualFrameIndex", value);
        }

        internal static string OutputFolder
        {
            get => EditorPrefs.GetString(Prefix + "OutputFolder", "ProfilerExports");
            set => EditorPrefs.SetString(Prefix + "OutputFolder", value);
        }

        internal static ProfilerFrameExportFormat Format
        {
            get => (ProfilerFrameExportFormat)EditorPrefs.GetInt(Prefix + "Format", (int)ProfilerFrameExportFormat.Json);
            set => EditorPrefs.SetInt(Prefix + "Format", (int)value);
        }

        internal static bool IncludeFrameSummary
        {
            get => EditorPrefs.GetBool(Prefix + "IncludeFrameSummary", true);
            set => EditorPrefs.SetBool(Prefix + "IncludeFrameSummary", value);
        }

        internal static bool IncludeCategories
        {
            get => EditorPrefs.GetBool(Prefix + "IncludeCategories", true);
            set => EditorPrefs.SetBool(Prefix + "IncludeCategories", value);
        }

        internal static bool IncludeHierarchy
        {
            get => EditorPrefs.GetBool(Prefix + "IncludeHierarchy", true);
            set => EditorPrefs.SetBool(Prefix + "IncludeHierarchy", value);
        }

        internal static bool IncludeTimeline
        {
            get => EditorPrefs.GetBool(Prefix + "IncludeTimeline", true);
            set => EditorPrefs.SetBool(Prefix + "IncludeTimeline", value);
        }

        internal static bool IncludeAllThreads
        {
            get => EditorPrefs.GetBool(Prefix + "IncludeAllThreads", true);
            set => EditorPrefs.SetBool(Prefix + "IncludeAllThreads", value);
        }

        internal static bool IncludeTopExpensiveSamples
        {
            get => EditorPrefs.GetBool(Prefix + "IncludeTopExpensiveSamples", true);
            set => EditorPrefs.SetBool(Prefix + "IncludeTopExpensiveSamples", value);
        }

        internal static int TopExpensiveSampleCount
        {
            get => EditorPrefs.GetInt(Prefix + "TopExpensiveSampleCount", 25);
            set => EditorPrefs.SetInt(Prefix + "TopExpensiveSampleCount", value);
        }

        internal static bool IncludeSamplePath
        {
            get => EditorPrefs.GetBool(Prefix + "IncludeSamplePath", true);
            set => EditorPrefs.SetBool(Prefix + "IncludeSamplePath", value);
        }

        internal static bool IncludeTimingColumns
        {
            get => EditorPrefs.GetBool(Prefix + "IncludeTimingColumns", true);
            set => EditorPrefs.SetBool(Prefix + "IncludeTimingColumns", value);
        }

        internal static bool IncludeGcMemory
        {
            get => EditorPrefs.GetBool(Prefix + "IncludeGcMemory", true);
            set => EditorPrefs.SetBool(Prefix + "IncludeGcMemory", value);
        }

        internal static bool IncludeCalls
        {
            get => EditorPrefs.GetBool(Prefix + "IncludeCalls", true);
            set => EditorPrefs.SetBool(Prefix + "IncludeCalls", value);
        }

        internal static bool IncludeCategory
        {
            get => EditorPrefs.GetBool(Prefix + "IncludeCategory", true);
            set => EditorPrefs.SetBool(Prefix + "IncludeCategory", value);
        }

        internal static bool IncludeObjectInfo
        {
            get => EditorPrefs.GetBool(Prefix + "IncludeObjectInfo", false);
            set => EditorPrefs.SetBool(Prefix + "IncludeObjectInfo", value);
        }

        internal static bool IncludeMetadata
        {
            get => EditorPrefs.GetBool(Prefix + "IncludeMetadata", false);
            set => EditorPrefs.SetBool(Prefix + "IncludeMetadata", value);
        }

        internal static bool IncludeCallstacks
        {
            get => EditorPrefs.GetBool(Prefix + "IncludeCallstacks", false);
            set => EditorPrefs.SetBool(Prefix + "IncludeCallstacks", value);
        }

        internal static float MinSelfTimeMs
        {
            get => EditorPrefs.GetFloat(Prefix + "MinSelfTimeMs", 0f);
            set => EditorPrefs.SetFloat(Prefix + "MinSelfTimeMs", value);
        }

        internal static int MaxHierarchyDepth
        {
            get => EditorPrefs.GetInt(Prefix + "MaxHierarchyDepth", 0);
            set => EditorPrefs.SetInt(Prefix + "MaxHierarchyDepth", value);
        }
    }
}

