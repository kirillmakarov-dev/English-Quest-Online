using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using Unity.Profiling;

namespace EnglishKingdom.Editor.Tools.Profiler
{
    internal static class ProfilerFrameExporter
    {
        private const int MainThreadIndex = 0;

        private static readonly List<int> ChildrenCache = new();
        private static readonly List<ExpensiveSample> ExpensiveSamples = new();

        internal readonly struct ExportResult
        {
            public readonly bool Success;
            public readonly string Message;
            public readonly string FilePath;

            public ExportResult(bool success, string message, string filePath = null)
            {
                Success = success;
                Message = message;
                FilePath = filePath;
            }
        }

        private struct ExpensiveSample
        {
            public string ThreadName;
            public string Name;
            public string Path;
            public double SelfTimeMs;
            public double TotalTimeMs;
            public long GcMemoryBytes;
        }

        [MenuItem("Tools/English Kingdom/Profiler/Export Selected Frame")]
        internal static void ExportSelectedFrameMenuItem()
        {
            var result = ExportCurrentOptions();
            if (result.Success)
                Debug.Log($"Profiler frame exported to: {result.FilePath}");
            else
                EditorUtility.DisplayDialog("Profiler Frame Export", result.Message, "OK");
        }

        internal static ExportResult ExportCurrentOptions()
        {
            if (!TryResolveFrameIndex(out int frameIndex, out string resolveError))
                return new ExportResult(false, resolveError);

            return ExportFrame(frameIndex);
        }

        internal static ExportResult ExportFrame(int frameIndex)
        {
            if (frameIndex < ProfilerDriver.firstFrameIndex || frameIndex > ProfilerDriver.lastFrameIndex)
            {
                return new ExportResult(false,
                    $"Frame {frameIndex} is out of range. Available frames: " +
                    $"{ProfilerDriver.firstFrameIndex}–{ProfilerDriver.lastFrameIndex}.");
            }

            if (!ProfilerFrameExportOptions.IncludeHierarchy &&
                !ProfilerFrameExportOptions.IncludeTimeline &&
                !ProfilerFrameExportOptions.IncludeFrameSummary &&
                !ProfilerFrameExportOptions.IncludeCategories &&
                !ProfilerFrameExportOptions.IncludeTopExpensiveSamples)
            {
                return new ExportResult(false, "Select at least one section to export.");
            }

            try
            {
                string content = ProfilerFrameExportOptions.Format == ProfilerFrameExportFormat.Json
                    ? BuildJsonExport(frameIndex)
                    : BuildMarkdownExport(frameIndex);

                string folder = ResolveOutputFolder();
                Directory.CreateDirectory(folder);

                string extension = ProfilerFrameExportOptions.Format == ProfilerFrameExportFormat.Json ? "json" : "md";
                string fileName = $"profiler-frame-{frameIndex}-{DateTime.Now:yyyyMMdd-HHmmss}.{extension}";
                string filePath = Path.Combine(folder, fileName);
                File.WriteAllText(filePath, content, Encoding.UTF8);

                EditorUtility.RevealInFinder(filePath);
                return new ExportResult(true, "Export completed.", filePath);
            }
            catch (Exception ex)
            {
                return new ExportResult(false, $"Export failed: {ex.Message}");
            }
        }

        internal static bool TryResolveFrameIndex(out int frameIndex, out string error)
        {
            frameIndex = -1;
            error = null;

            if (ProfilerDriver.lastFrameIndex < 0)
            {
                error = "No profiler data is loaded. Open the Profiler window, record or load a capture, then try again.";
                return false;
            }

            if (ProfilerFrameExportOptions.FrameSource == ProfilerFrameSource.ManualIndex)
            {
                frameIndex = ProfilerFrameExportOptions.ManualFrameIndex;
                return true;
            }

            var profilerWindow = EditorWindow.GetWindow<ProfilerWindow>(false, null, false);
            if (profilerWindow == null)
            {
                error = "Profiler window is not open. Open Window → Analysis → Profiler, select a frame, then export.";
                return false;
            }

            long selected = profilerWindow.selectedFrameIndex;
            if (selected < 0)
            {
                error = "No frame is selected in the Profiler window.";
                return false;
            }

            frameIndex = (int)selected;
            return true;
        }

        internal static int GetThreadCount(int frameIndex)
        {
            using var iterator = new ProfilerFrameDataIterator();
            iterator.SetRoot(frameIndex, MainThreadIndex);
            return iterator.GetThreadCount(frameIndex);
        }

        private static string ResolveOutputFolder()
        {
            string configured = ProfilerFrameExportOptions.OutputFolder?.Trim();
            if (string.IsNullOrEmpty(configured))
                return Path.Combine(Application.dataPath, "..", "ProfilerExports");

            if (Path.IsPathRooted(configured))
                return configured;

            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", configured));
        }

        private static string BuildJsonExport(int frameIndex)
        {
            var sb = new StringBuilder(256 * 1024);
            sb.Append("{\n");
            AppendJsonPair(sb, "exportVersion", 1, 1, true);
            AppendJsonPair(sb, "exportedAtUtc", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), 1);
            AppendJsonPair(sb, "project", Application.productName, 1);
            AppendJsonPair(sb, "unityVersion", Application.unityVersion, 1);
            AppendJsonPair(sb, "frameIndex", frameIndex, 1);

            int sectionCount = CountEnabledSections();
            int sectionIndex = 0;

            if (ProfilerFrameExportOptions.IncludeFrameSummary)
            {
                sb.Append("  \"frameSummary\": ");
                AppendFrameSummaryJson(sb, frameIndex);
                if (++sectionIndex < sectionCount) sb.Append(',');
                sb.Append('\n');
            }

            if (ProfilerFrameExportOptions.IncludeCategories)
            {
                sb.Append("  \"categories\": ");
                AppendCategoriesJson(sb, frameIndex);
                if (++sectionIndex < sectionCount) sb.Append(',');
                sb.Append('\n');
            }

            if (ProfilerFrameExportOptions.IncludeTopExpensiveSamples)
            {
                sb.Append("  \"topExpensiveSamples\": ");
                AppendTopExpensiveSamplesJson(sb, frameIndex);
                if (++sectionIndex < sectionCount) sb.Append(',');
                sb.Append('\n');
            }

            if (ProfilerFrameExportOptions.IncludeHierarchy || ProfilerFrameExportOptions.IncludeTimeline)
            {
                sb.Append("  \"threads\": [\n");
                AppendThreadsJson(sb, frameIndex);
                sb.Append("  ]");
                if (++sectionIndex < sectionCount) sb.Append(',');
                sb.Append('\n');
            }

            sb.Append("}\n");
            return sb.ToString();
        }

        private static string BuildMarkdownExport(int frameIndex)
        {
            var sb = new StringBuilder(256 * 1024);
            sb.AppendLine("# Unity Profiler Frame Export");
            sb.AppendLine();
            sb.AppendLine($"- **Project:** {Application.productName}");
            sb.AppendLine($"- **Unity:** {Application.unityVersion}");
            sb.AppendLine($"- **Frame index:** {frameIndex}");
            sb.AppendLine($"- **Exported (UTC):** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            if (ProfilerFrameExportOptions.IncludeFrameSummary)
            {
                sb.AppendLine("## Frame Summary");
                AppendFrameSummaryMarkdown(sb, frameIndex);
                sb.AppendLine();
            }

            if (ProfilerFrameExportOptions.IncludeCategories)
            {
                sb.AppendLine("## Profiler Categories");
                AppendCategoriesMarkdown(sb, frameIndex);
                sb.AppendLine();
            }

            if (ProfilerFrameExportOptions.IncludeTopExpensiveSamples)
            {
                sb.AppendLine($"## Top {ProfilerFrameExportOptions.TopExpensiveSampleCount} Expensive Samples (by self time)");
                AppendTopExpensiveSamplesMarkdown(sb, frameIndex);
                sb.AppendLine();
            }

            if (ProfilerFrameExportOptions.IncludeHierarchy || ProfilerFrameExportOptions.IncludeTimeline)
            {
                int threadCount = GetThreadCount(frameIndex);
                int startThread = 0;
                int endThread = ProfilerFrameExportOptions.IncludeAllThreads ? threadCount - 1 : MainThreadIndex;

                for (int threadIndex = startThread; threadIndex <= endThread; threadIndex++)
                {
                    using var hierarchy = ProfilerDriver.GetHierarchyFrameDataView(
                        frameIndex, threadIndex,
                        HierarchyFrameDataView.ViewModes.Default,
                        HierarchyFrameDataView.columnDontSort, false);

                    if (!hierarchy.valid)
                        continue;

                    sb.AppendLine($"## Thread: {hierarchy.threadGroupName} / {hierarchy.threadName}");
                    sb.AppendLine($"- CPU frame time: {hierarchy.frameTimeMs:F3} ms");
                    sb.AppendLine($"- GPU frame time: {hierarchy.frameGpuTimeMs:F3} ms");
                    sb.AppendLine($"- FPS: {hierarchy.frameFps:F1}");
                    sb.AppendLine($"- Sample count: {hierarchy.sampleCount}");
                    sb.AppendLine();

                    if (ProfilerFrameExportOptions.IncludeHierarchy)
                    {
                        sb.AppendLine("### Hierarchy");
                        AppendHierarchyMarkdown(sb, hierarchy);
                        sb.AppendLine();
                    }

                    if (ProfilerFrameExportOptions.IncludeTimeline)
                    {
                        sb.AppendLine("### Timeline (raw samples)");
                        AppendTimelineMarkdown(sb, frameIndex, threadIndex);
                        sb.AppendLine();
                    }
                }
            }

            return sb.ToString();
        }

        private static int CountEnabledSections()
        {
            int count = 0;
            if (ProfilerFrameExportOptions.IncludeFrameSummary) count++;
            if (ProfilerFrameExportOptions.IncludeCategories) count++;
            if (ProfilerFrameExportOptions.IncludeTopExpensiveSamples) count++;
            if (ProfilerFrameExportOptions.IncludeHierarchy || ProfilerFrameExportOptions.IncludeTimeline) count++;
            return count;
        }

        private static void AppendFrameSummaryJson(StringBuilder sb, int frameIndex)
        {
            using var view = ProfilerDriver.GetHierarchyFrameDataView(
                frameIndex, MainThreadIndex,
                HierarchyFrameDataView.ViewModes.Default,
                HierarchyFrameDataView.columnDontSort, false);

            sb.Append("{\n");
            if (view.valid)
            {
                AppendJsonPair(sb, "fps", view.frameFps, 2);
                AppendJsonPair(sb, "cpuFrameTimeMs", view.frameTimeMs, 2);
                AppendJsonPair(sb, "gpuFrameTimeMs", view.frameGpuTimeMs, 2);
                AppendJsonPair(sb, "frameStartTimeMs", view.frameStartTimeMs, 2);
                AppendJsonPair(sb, "threadCount", GetThreadCount(frameIndex), 2, false);
            }
            else
            {
                AppendJsonPair(sb, "error", "No valid frame data on main thread.", 2, false);
            }

            sb.Append("  }");
        }

        private static void AppendFrameSummaryMarkdown(StringBuilder sb, int frameIndex)
        {
            using var view = ProfilerDriver.GetHierarchyFrameDataView(
                frameIndex, MainThreadIndex,
                HierarchyFrameDataView.ViewModes.Default,
                HierarchyFrameDataView.columnDontSort, false);

            if (!view.valid)
            {
                sb.AppendLine("_No valid frame data on main thread._");
                return;
            }

            sb.AppendLine($"- **FPS:** {view.frameFps:F1}");
            sb.AppendLine($"- **CPU frame time:** {view.frameTimeMs:F3} ms");
            sb.AppendLine($"- **GPU frame time:** {view.frameGpuTimeMs:F3} ms");
            sb.AppendLine($"- **Frame start time:** {view.frameStartTimeMs:F3} ms");
            sb.AppendLine($"- **Thread count:** {GetThreadCount(frameIndex)}");
        }

        private static void AppendCategoriesJson(StringBuilder sb, int frameIndex)
        {
            using var view = ProfilerDriver.GetHierarchyFrameDataView(
                frameIndex, MainThreadIndex,
                HierarchyFrameDataView.ViewModes.Default,
                HierarchyFrameDataView.columnDontSort, false);

            sb.Append("[\n");
            if (view.valid)
            {
                var categories = new List<ProfilerCategoryInfo>();
                view.GetAllCategories(categories);
                for (int i = 0; i < categories.Count; i++)
                {
                    var info = categories[i];
                    sb.Append("    {\n");
                    AppendJsonPair(sb, "id", info.id, 3);
                    AppendJsonPair(sb, "name", info.name, 3);
                    AppendJsonPair(sb, "color", ColorToHex(info.color), 3, false);
                    sb.Append("    }");
                    if (i < categories.Count - 1) sb.Append(',');
                    sb.Append('\n');
                }
            }

            sb.Append("  ]");
        }

        private static void AppendCategoriesMarkdown(StringBuilder sb, int frameIndex)
        {
            using var view = ProfilerDriver.GetHierarchyFrameDataView(
                frameIndex, MainThreadIndex,
                HierarchyFrameDataView.ViewModes.Default,
                HierarchyFrameDataView.columnDontSort, false);

            if (!view.valid)
            {
                sb.AppendLine("_No category data available._");
                return;
            }

            sb.AppendLine("| ID | Name | Color |");
            sb.AppendLine("|---:|------|-------|");
            var categories = new List<ProfilerCategoryInfo>();
            view.GetAllCategories(categories);
            foreach (var info in categories)
                sb.AppendLine($"| {info.id} | {EscapeMarkdown(info.name)} | `{ColorToHex(info.color)}` |");
        }

        private static void CollectExpensiveSamples(int frameIndex)
        {
            ExpensiveSamples.Clear();
            int threadCount = GetThreadCount(frameIndex);
            int startThread = 0;
            int endThread = ProfilerFrameExportOptions.IncludeAllThreads ? threadCount - 1 : MainThreadIndex;

            for (int threadIndex = startThread; threadIndex <= endThread; threadIndex++)
            {
                using var view = ProfilerDriver.GetHierarchyFrameDataView(
                    frameIndex, threadIndex,
                    HierarchyFrameDataView.ViewModes.Default,
                    HierarchyFrameDataView.columnDontSort, false);

                if (!view.valid)
                    continue;

                int rootId = view.GetRootItemID();
                CollectExpensiveFromHierarchy(view, rootId, view.threadName);
            }

            ExpensiveSamples.Sort((a, b) => b.SelfTimeMs.CompareTo(a.SelfTimeMs));
        }

        private static int[] GetChildItemIds(HierarchyFrameDataView view, int itemId)
        {
            view.GetItemChildren(itemId, ChildrenCache);
            return ChildrenCache.Count == 0 ? Array.Empty<int>() : ChildrenCache.ToArray();
        }

        private static void CollectExpensiveFromHierarchy(HierarchyFrameDataView view, int itemId, string threadName)
        {
            double selfTimeMs = view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnSelfTime);
            if (selfTimeMs >= ProfilerFrameExportOptions.MinSelfTimeMs)
            {
                ExpensiveSamples.Add(new ExpensiveSample
                {
                    ThreadName = threadName,
                    Name = view.GetItemName(itemId),
                    Path = ProfilerFrameExportOptions.IncludeSamplePath ? view.GetItemPath(itemId) : null,
                    SelfTimeMs = selfTimeMs,
                    TotalTimeMs = view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnTotalTime),
                    GcMemoryBytes = (long)view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnGcMemory)
                });
            }

            foreach (int childId in GetChildItemIds(view, itemId))
                CollectExpensiveFromHierarchy(view, childId, threadName);
        }

        private static void AppendTopExpensiveSamplesJson(StringBuilder sb, int frameIndex)
        {
            CollectExpensiveSamples(frameIndex);
            int count = Mathf.Min(ProfilerFrameExportOptions.TopExpensiveSampleCount, ExpensiveSamples.Count);

            sb.Append("[\n");
            for (int i = 0; i < count; i++)
            {
                var sample = ExpensiveSamples[i];
                sb.Append("    {\n");
                AppendJsonPair(sb, "rank", i + 1, 3);
                AppendJsonPair(sb, "thread", sample.ThreadName, 3);
                AppendJsonPair(sb, "name", sample.Name, 3);
                if (ProfilerFrameExportOptions.IncludeSamplePath)
                    AppendJsonPair(sb, "path", sample.Path, 3);
                AppendJsonPair(sb, "selfTimeMs", sample.SelfTimeMs, 3);
                AppendJsonPair(sb, "totalTimeMs", sample.TotalTimeMs, 3);
                AppendJsonPair(sb, "gcMemoryBytes", sample.GcMemoryBytes, 3, false);
                sb.Append("    }");
                if (i < count - 1) sb.Append(',');
                sb.Append('\n');
            }

            sb.Append("  ]");
        }

        private static void AppendTopExpensiveSamplesMarkdown(StringBuilder sb, int frameIndex)
        {
            CollectExpensiveSamples(frameIndex);
            int count = Mathf.Min(ProfilerFrameExportOptions.TopExpensiveSampleCount, ExpensiveSamples.Count);

            sb.AppendLine("| Rank | Thread | Name | Self (ms) | Total (ms) | GC (bytes) |");
            sb.AppendLine("|-----:|--------|------|----------:|-----------:|-----------:|");
            for (int i = 0; i < count; i++)
            {
                var sample = ExpensiveSamples[i];
                sb.AppendLine(
                    $"| {i + 1} | {EscapeMarkdown(sample.ThreadName)} | {EscapeMarkdown(sample.Name)} | " +
                    $"{sample.SelfTimeMs:F3} | {sample.TotalTimeMs:F3} | {sample.GcMemoryBytes} |");
            }
        }

        private static void AppendThreadsJson(StringBuilder sb, int frameIndex)
        {
            int threadCount = GetThreadCount(frameIndex);
            int startThread = 0;
            int endThread = ProfilerFrameExportOptions.IncludeAllThreads ? threadCount - 1 : MainThreadIndex;
            bool wroteThread = false;

            for (int threadIndex = startThread; threadIndex <= endThread; threadIndex++)
            {
                using var hierarchy = ProfilerDriver.GetHierarchyFrameDataView(
                    frameIndex, threadIndex,
                    HierarchyFrameDataView.ViewModes.Default,
                    HierarchyFrameDataView.columnDontSort, false);

                if (!hierarchy.valid)
                    continue;

                if (wroteThread) sb.Append(",\n");
                wroteThread = true;

                sb.Append("    {\n");
                AppendJsonPair(sb, "threadIndex", threadIndex, 3);
                AppendJsonPair(sb, "threadGroup", hierarchy.threadGroupName, 3);
                AppendJsonPair(sb, "threadName", hierarchy.threadName, 3);
                AppendJsonPair(sb, "cpuFrameTimeMs", hierarchy.frameTimeMs, 3);
                AppendJsonPair(sb, "gpuFrameTimeMs", hierarchy.frameGpuTimeMs, 3);
                AppendJsonPair(sb, "fps", hierarchy.frameFps, 3);
                AppendJsonPair(sb, "sampleCount", hierarchy.sampleCount, 3, ProfilerFrameExportOptions.IncludeHierarchy || ProfilerFrameExportOptions.IncludeTimeline);

                if (ProfilerFrameExportOptions.IncludeHierarchy)
                {
                    sb.Append("      \"hierarchy\": ");
                    AppendHierarchyJson(sb, hierarchy);
                    if (ProfilerFrameExportOptions.IncludeTimeline) sb.Append(',');
                    sb.Append('\n');
                }

                if (ProfilerFrameExportOptions.IncludeTimeline)
                {
                    sb.Append("      \"timeline\": ");
                    AppendTimelineJson(sb, frameIndex, threadIndex);
                    sb.Append('\n');
                }

                sb.Append("    }");
            }
        }

        private static void AppendHierarchyJson(StringBuilder sb, HierarchyFrameDataView view)
        {
            var items = new List<int>();
            TraverseHierarchy(view, view.GetRootItemID(), items, 0);
            var filtered = new List<int>();
            foreach (int itemId in items)
            {
                double selfTimeMs = view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnSelfTime);
                if (selfTimeMs >= ProfilerFrameExportOptions.MinSelfTimeMs)
                    filtered.Add(itemId);
            }

            sb.Append("[\n");
            for (int i = 0; i < filtered.Count; i++)
            {
                AppendHierarchyItemJson(sb, view, filtered[i], i);
                if (i < filtered.Count - 1) sb.Append(',');
                sb.Append('\n');
            }

            sb.Append("      ]");
        }

        private static void TraverseHierarchy(HierarchyFrameDataView view, int itemId, List<int> items, int depth)
        {
            int maxDepth = ProfilerFrameExportOptions.MaxHierarchyDepth;
            if (maxDepth > 0 && depth > maxDepth)
                return;

            items.Add(itemId);
            foreach (int childId in GetChildItemIds(view, itemId))
                TraverseHierarchy(view, childId, items, depth + 1);
        }

        private static void AppendHierarchyItemJson(StringBuilder sb, HierarchyFrameDataView view, int itemId, int flatIndex)
        {
            double selfTimeMs = view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnSelfTime);
            sb.Append("        {\n");
            AppendJsonPair(sb, "flatIndex", flatIndex, 5);
            AppendJsonPair(sb, "itemId", itemId, 5);
            AppendJsonPair(sb, "depth", view.GetItemDepth(itemId), 5);
            AppendJsonPair(sb, "name", view.GetItemName(itemId), 5);

            if (ProfilerFrameExportOptions.IncludeSamplePath)
                AppendJsonPair(sb, "path", view.GetItemPath(itemId), 5);

            if (ProfilerFrameExportOptions.IncludeTimingColumns)
            {
                AppendJsonPair(sb, "selfTimeMs", selfTimeMs, 5);
                AppendJsonPair(sb, "totalTimeMs", view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnTotalTime), 5);
                AppendJsonPair(sb, "selfPercent", view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnSelfPercent), 5);
                AppendJsonPair(sb, "totalPercent", view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnTotalPercent), 5);
            }

            if (ProfilerFrameExportOptions.IncludeGcMemory)
                AppendJsonPair(sb, "gcMemoryBytes", (long)view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnGcMemory), 5);

            if (ProfilerFrameExportOptions.IncludeCalls)
                AppendJsonPair(sb, "calls", (long)view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnCalls), 5);

            if (ProfilerFrameExportOptions.IncludeCategory)
            {
                ushort categoryIndex = view.GetItemCategoryIndex(itemId);
                var categoryInfo = view.GetCategoryInfo(categoryIndex);
                AppendJsonPair(sb, "category", categoryInfo.name, 5);
            }

            if (ProfilerFrameExportOptions.IncludeObjectInfo)
                AppendObjectInfoJson(sb, view, itemId, 5,
                    ProfilerFrameExportOptions.IncludeMetadata || ProfilerFrameExportOptions.IncludeCallstacks);

            if (ProfilerFrameExportOptions.IncludeMetadata)
                AppendItemMetadataJson(sb, view, itemId, 5, ProfilerFrameExportOptions.IncludeCallstacks);

            if (ProfilerFrameExportOptions.IncludeCallstacks)
                AppendCallstackJson(sb, view, itemId, 5);
            else
                TrimTrailingComma(sb);

            sb.Append("        }");
        }

        private static void AppendHierarchyMarkdown(StringBuilder sb, HierarchyFrameDataView view)
        {
            var header = new StringBuilder("| Depth | Name ");
            if (ProfilerFrameExportOptions.IncludeTimingColumns) header.Append("| Self (ms) | Total (ms) ");
            if (ProfilerFrameExportOptions.IncludeGcMemory) header.Append("| GC (bytes) ");
            if (ProfilerFrameExportOptions.IncludeCategory) header.Append("| Category ");
            header.Append('|');
            sb.AppendLine(header.ToString());

            var separator = new StringBuilder("|---:|------");
            if (ProfilerFrameExportOptions.IncludeTimingColumns) separator.Append("|----------:|-----------:");
            if (ProfilerFrameExportOptions.IncludeGcMemory) separator.Append("|-----------:");
            if (ProfilerFrameExportOptions.IncludeCategory) separator.Append("|----------");
            separator.Append('|');
            sb.AppendLine(separator.ToString());

            var items = new List<int>();
            TraverseHierarchy(view, view.GetRootItemID(), items, 0);
            foreach (int itemId in items)
            {
                double selfTimeMs = view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnSelfTime);
                if (selfTimeMs < ProfilerFrameExportOptions.MinSelfTimeMs)
                    continue;

                var row = new StringBuilder($"| {view.GetItemDepth(itemId)} | {EscapeMarkdown(view.GetItemName(itemId))} ");
                if (ProfilerFrameExportOptions.IncludeTimingColumns)
                {
                    row.Append($"| {selfTimeMs:F3} | {view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnTotalTime):F3} ");
                }

                if (ProfilerFrameExportOptions.IncludeGcMemory)
                {
                    row.Append($"| {(long)view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnGcMemory)} ");
                }

                if (ProfilerFrameExportOptions.IncludeCategory)
                {
                    ushort categoryIndex = view.GetItemCategoryIndex(itemId);
                    row.Append($"| {EscapeMarkdown(view.GetCategoryInfo(categoryIndex).name)} ");
                }

                row.Append('|');
                sb.AppendLine(row.ToString());
            }
        }

        private static void AppendTimelineJson(StringBuilder sb, int frameIndex, int threadIndex)
        {
            using var raw = ProfilerDriver.GetRawFrameDataView(frameIndex, threadIndex);
            var sampleIndices = new List<int>();

            if (raw.valid)
            {
                for (int i = 0; i < raw.sampleCount; i++)
                {
                    if (raw.GetSampleTimeMs(i) >= ProfilerFrameExportOptions.MinSelfTimeMs)
                        sampleIndices.Add(i);
                }
            }

            sb.Append("[\n");
            for (int listIndex = 0; listIndex < sampleIndices.Count; listIndex++)
            {
                int i = sampleIndices[listIndex];
                double durationMs = raw.GetSampleTimeMs(i);

                sb.Append("        {\n");
                AppendJsonPair(sb, "sampleIndex", i, 5);
                AppendJsonPair(sb, "name", raw.GetSampleName(i), 5);
                AppendJsonPair(sb, "startTimeMs", raw.GetSampleStartTimeMs(i), 5);
                AppendJsonPair(sb, "durationMs", durationMs, 5);

                if (ProfilerFrameExportOptions.IncludeCategory)
                {
                    ushort categoryIndex = raw.GetSampleCategoryIndex(i);
                    AppendJsonPair(sb, "category", raw.GetCategoryInfo(categoryIndex).name, 5);
                }

                if (ProfilerFrameExportOptions.IncludeMetadata)
                    AppendRawSampleMetadataJson(sb, raw, i, 5);
                else
                    TrimTrailingComma(sb);

                sb.Append("        }");
                if (listIndex < sampleIndices.Count - 1) sb.Append(',');
                sb.Append('\n');
            }

            sb.Append("      ]");
        }

        private static void AppendTimelineMarkdown(StringBuilder sb, int frameIndex, int threadIndex)
        {
            using var raw = ProfilerDriver.GetRawFrameDataView(frameIndex, threadIndex);
            if (!raw.valid)
            {
                sb.AppendLine("_No raw timeline data._");
                return;
            }

            sb.AppendLine("| Index | Start (ms) | Duration (ms) | Name |");
            sb.AppendLine("|------:|-----------:|--------------:|------|");
            for (int i = 0; i < raw.sampleCount; i++)
            {
                double durationMs = raw.GetSampleTimeMs(i);
                if (durationMs < ProfilerFrameExportOptions.MinSelfTimeMs)
                    continue;

                sb.AppendLine(
                    $"| {i} | {raw.GetSampleStartTimeMs(i):F3} | {durationMs:F3} | {EscapeMarkdown(raw.GetSampleName(i))} |");
            }
        }

        private static void AppendObjectInfoJson(StringBuilder sb, HierarchyFrameDataView view, int itemId, int indent, bool hasMoreFields)
        {
            var entityId = view.GetItemEntityId(itemId);
            if (!view.GetUnityObjectInfo(entityId, out var objectInfo))
            {
                AppendJsonPair(sb, "unityObject", null, indent, hasMoreFields);
                return;
            }

            string typeName = view.GetUnityObjectNativeTypeInfo(objectInfo.nativeTypeIndex, out var typeInfo)
                ? typeInfo.name
                : null;

            string indentStr = new(' ', indent * 2);
            sb.Append(indentStr).Append("\"unityObject\": {\n");
            AppendJsonPair(sb, "name", objectInfo.name, indent + 1);
            AppendJsonPair(sb, "typeName", typeName, indent + 1);
            AppendJsonPair(sb, "nativeTypeIndex", objectInfo.nativeTypeIndex, indent + 1, false);
            sb.Append(indentStr).Append("  }");
            if (hasMoreFields) sb.Append(',');
            sb.Append('\n');
        }

        private static void AppendItemMetadataJson(StringBuilder sb, HierarchyFrameDataView view, int itemId, int indent, bool hasMoreFields)
        {
            int count = view.GetItemMetadataCount(itemId);
            string indentStr = new(' ', indent * 2);
            sb.Append(indentStr).Append("\"metadata\": [");
            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append('"').Append(EscapeJson(view.GetItemMetadata(itemId, i))).Append('"');
            }

            sb.Append(']');
            if (hasMoreFields) sb.Append(',');
            sb.Append('\n');
        }

        private static void AppendRawSampleMetadataJson(StringBuilder sb, RawFrameDataView raw, int sampleIndex, int indent)
        {
            int count = raw.GetSampleMetadataCount(sampleIndex);
            string indentStr = new(' ', indent * 2);
            sb.Append(indentStr).Append("\"metadata\": [");
            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append('"').Append(EscapeJson(raw.GetSampleMetadataAsString(sampleIndex, i))).Append('"');
            }

            sb.Append("]\n");
        }

        private static void AppendCallstackJson(StringBuilder sb, HierarchyFrameDataView view, int itemId, int indent)
        {
            string callstack = view.ResolveItemCallstack(itemId) ?? string.Empty;
            AppendJsonPair(sb, "callstack", callstack, indent, comma: false);
        }

        private static void TrimTrailingComma(StringBuilder sb)
        {
            if (sb.Length >= 2 && sb[^2] == ',' && sb[^1] == '\n')
            {
                sb.Length -= 2;
                sb.Append('\n');
            }
        }

        private static void AppendJsonPair(StringBuilder sb, string key, string value, int indent, bool comma = true)
        {
            string indentStr = new(' ', indent * 2);
            sb.Append(indentStr).Append('"').Append(key).Append("\": ");
            if (value == null)
                sb.Append("null");
            else
                sb.Append('"').Append(EscapeJson(value)).Append('"');
            if (comma) sb.Append(",\n");
            else sb.Append('\n');
        }

        private static void AppendJsonPair(StringBuilder sb, string key, double value, int indent, bool comma = true)
        {
            string indentStr = new(' ', indent * 2);
            sb.Append(indentStr).Append('"').Append(key).Append("\": ");
            sb.Append(value.ToString("R", CultureInfo.InvariantCulture));
            if (comma) sb.Append(",\n");
            else sb.Append('\n');
        }

        private static void AppendJsonPair(StringBuilder sb, string key, long value, int indent, bool comma = true)
        {
            string indentStr = new(' ', indent * 2);
            sb.Append(indentStr).Append('"').Append(key).Append("\": ").Append(value);
            if (comma) sb.Append(",\n");
            else sb.Append('\n');
        }

        private static void AppendJsonPair(StringBuilder sb, string key, int value, int indent, bool comma = true)
        {
            AppendJsonPair(sb, key, (long)value, indent, comma);
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        private static string EscapeMarkdown(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Replace("|", "\\|");
        }

        private static string ColorToHex(Color color)
        {
            var c = (Color32)color;
            return $"#{c.r:X2}{c.g:X2}{c.b:X2}";
        }
    }
}
