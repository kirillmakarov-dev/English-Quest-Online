#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Debug-only component for testing the quest system at runtime.
/// Prefer Tools → English Kingdom → Quests → Quest System → Live Debug for the full dual-mode hub.
/// Attach to any GameObject for Inspector context menus / quick target selection.
/// Only compiled in Editor and Development Build configurations.
/// </summary>
[AddComponentMenu(QuestSystemComponentMenuPaths.Debug + "/Quest Debug Tools")]
public class QuestDebugTools : MonoBehaviour
{
    [Header("Target Quest")]
    [Tooltip("The quest to operate on. Leave empty to pick from AllQuests by id at runtime.")]
    [SerializeField] private QuestInfo targetQuest;

    [Tooltip("When Target Quest is empty, resolve by this id from QuestManager.AllQuests in Play Mode.")]
    [SerializeField] private string targetQuestId;

    // ── Context Menu Actions ───────────────────────────────────────────────

    [ContextMenu("Debug: Force Start Quest")]
    public void ForceStartQuest()
    {
        if (!TryResolveTarget(out QuestInfo quest)) return;
        QuestManager.Instance.Debug_ForceStartQuest(quest);
    }

    [ContextMenu("Debug: Complete Current Step")]
    public void CompleteCurrentStep()
    {
        if (!TryResolveTarget(out QuestInfo quest)) return;
        QuestManager.Instance.Debug_CompleteCurrentStep(quest);
    }

    [ContextMenu("Debug: Skip Current Step")]
    public void SkipCurrentStep()
    {
        if (!TryResolveTarget(out QuestInfo quest)) return;
        QuestManager.Instance.Debug_SkipCurrentStep(quest);
    }

    [ContextMenu("Debug: Force Complete Quest")]
    public void ForceCompleteQuest()
    {
        if (!TryResolveTarget(out QuestInfo quest)) return;
        QuestManager.Instance.Debug_ForceCompleteQuest(quest);
    }

    [ContextMenu("Debug: Reset Quest")]
    public void ResetQuest()
    {
        if (!TryResolveTarget(out QuestInfo quest)) return;
        QuestManager.Instance.Debug_ResetQuest(quest);
    }

    [ContextMenu("Debug: Complete ALL Quests")]
    public void CompleteAllQuests()
    {
        if (!ValidateManager()) return;
        QuestManager.Instance.Debug_CompleteAllQuests();
    }

    [ContextMenu("Debug: Dump All Quest States")]
    public void DumpAllQuestStates()
    {
        if (!ValidateManager()) return;
        QuestManager.Instance.LogAllQuestDebugStates();
    }

    [ContextMenu("Debug: Log Target Snapshot")]
    public void LogTargetSnapshot()
    {
        if (!TryResolveTarget(out QuestInfo quest)) return;
        AppLog.Info($"[QuestDebugTools] {QuestManager.Instance.FormatDebugState(quest)}");
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Keep serialized id in sync when a scene QuestInfo is assigned.
        if (targetQuest != null && !string.IsNullOrEmpty(targetQuest.id))
            targetQuestId = targetQuest.id;
    }
#endif

    // ── Helpers ────────────────────────────────────────────────────────────

    private bool TryResolveTarget(out QuestInfo quest)
    {
        quest = null;
        if (!ValidateManager())
            return false;

        if (targetQuest != null)
        {
            quest = targetQuest;
            return true;
        }

        if (!string.IsNullOrEmpty(targetQuestId))
        {
            quest = QuestManager.Instance.GetQuestById(targetQuestId);
            if (quest != null)
                return true;

            AppLog.Warning($"[QuestDebugTools] No quest with id '{targetQuestId}' in QuestManager.AllQuests.");
            return false;
        }

        // Fallback: first in-progress, else first registered.
        IReadOnlyList<QuestInfo> all = QuestManager.Instance.AllQuests;
        if (all != null)
        {
            foreach (QuestInfo candidate in all)
            {
                if (candidate != null && candidate.state == QuestState.IN_PROGRESS)
                {
                    quest = candidate;
                    return true;
                }
            }

            foreach (QuestInfo candidate in all)
            {
                if (candidate != null)
                {
                    quest = candidate;
                    return true;
                }
            }
        }

        AppLog.Warning("[QuestDebugTools] No target quest assigned and AllQuests is empty.");
        return false;
    }

    private bool ValidateManager()
    {
        if (!QuestManager.HasInstance)
        {
            AppLog.Warning("[QuestDebugTools] QuestManager not found in scene.");
            return false;
        }
        return true;
    }
}
#endif
