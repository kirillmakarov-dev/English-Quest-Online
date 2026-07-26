using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public partial class TeacherTeleport
{
    public event Action<GuiderQuestSnapshotReceivedArgs> OnGuiderQuestSnapshotReceived;

    private readonly List<GuiderQuestSnapshotEntry> _pendingSnapshotEntries = new();
    private PlayerRef _pendingSnapshotTarget;
    private int _expectedSnapshotCount;

    public void RequestQuestSnapshot(PlayerRef targetPlayer)
    {
        if (Runner == null || !Runner.IsRunning || !IsLocalPlayerGuider)
            return;

        if (!targetPlayer.IsRealPlayer)
            return;

        RPC_RequestQuestSnapshot(Runner.LocalPlayer, targetPlayer);
    }

    public void ApplyQuestAction(PlayerRef targetPlayer, string questId, GuiderQuestAction action)
    {
        if (Runner == null || !Runner.IsRunning || !IsLocalPlayerGuider)
            return;

        if (string.IsNullOrEmpty(questId) || !targetPlayer.IsRealPlayer)
            return;

        RPC_ApplyQuestAction(Runner.LocalPlayer, targetPlayer, questId, action);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_RequestQuestSnapshot(PlayerRef guider, PlayerRef target, RpcInfo info = default)
    {
        if (!IsGuider(guider))
            return;

        if (Runner == null || Runner.LocalPlayer != target)
            return;

        IReadOnlyList<GuiderQuestSnapshotEntry> entries = BuildLocalOpenWorldSnapshot();
        SendSnapshotToGuider(guider, target, entries);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_ApplyQuestAction(
        PlayerRef guider,
        PlayerRef target,
        string questId,
        GuiderQuestAction action,
        RpcInfo info = default)
    {
        if (!IsGuider(guider))
            return;

        if (Runner == null || Runner.LocalPlayer != target)
            return;

        QuestManager manager = QuestManager.Instance;
        if (manager == null)
        {
            AppLog.Warning("[GuiderQuest] QuestManager not found on target client.");
            return;
        }

        if (!manager.Guider_ApplyAction(questId, action))
            AppLog.Warning($"[GuiderQuest] Failed to apply action '{action}' to quest '{questId}' on target client.");

        IReadOnlyList<GuiderQuestSnapshotEntry> entries = BuildLocalOpenWorldSnapshot();
        SendSnapshotToGuider(guider, target, entries);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_SnapshotBegin(PlayerRef guider, PlayerRef target, int count, RpcInfo info = default)
    {
        if (Runner == null || Runner.LocalPlayer != guider)
            return;

        _pendingSnapshotEntries.Clear();
        _pendingSnapshotTarget = target;
        _expectedSnapshotCount = count;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_SnapshotEntry(
        PlayerRef guider,
        NetworkString<_32> questId,
        int state,
        int stepIndex,
        RpcInfo info = default)
    {
        if (Runner == null || Runner.LocalPlayer != guider)
            return;

        _pendingSnapshotEntries.Add(new GuiderQuestSnapshotEntry(
            questId.ToString(),
            (QuestState)state,
            stepIndex));
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_SnapshotEnd(PlayerRef guider, PlayerRef target, RpcInfo info = default)
    {
        if (Runner == null || Runner.LocalPlayer != guider)
            return;

        if (_pendingSnapshotTarget != target)
            return;

        if (_expectedSnapshotCount != _pendingSnapshotEntries.Count)
        {
            AppLog.Warning(
                $"[GuiderQuest] Snapshot count mismatch for {target}: expected {_expectedSnapshotCount}, got {_pendingSnapshotEntries.Count}.");
        }

        OnGuiderQuestSnapshotReceived?.Invoke(
            new GuiderQuestSnapshotReceivedArgs(target, _pendingSnapshotEntries.ToArray()));
    }

    private void SendSnapshotToGuider(PlayerRef guider, PlayerRef target, IReadOnlyList<GuiderQuestSnapshotEntry> entries)
    {
        RPC_SnapshotBegin(guider, target, entries.Count);

        for (int i = 0; i < entries.Count; i++)
        {
            GuiderQuestSnapshotEntry entry = entries[i];
            RPC_SnapshotEntry(guider, entry.QuestId, (int)entry.State, entry.StepIndex);
        }

        RPC_SnapshotEnd(guider, target);
    }

    private static IReadOnlyList<GuiderQuestSnapshotEntry> BuildLocalOpenWorldSnapshot()
    {
        var entries = new List<GuiderQuestSnapshotEntry>();
        QuestManager manager = QuestManager.Instance;
        if (manager == null)
            return entries;

        foreach (QuestInfo quest in manager.GetOpenWorldQuests())
        {
            if (quest == null || string.IsNullOrEmpty(quest.id))
                continue;

            entries.Add(new GuiderQuestSnapshotEntry(quest.id, quest.state, quest.currentStepIndex));
        }

        return entries;
    }
}
