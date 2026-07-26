using System.Collections.Generic;
using Fusion;

public enum GuiderQuestAction : byte
{
    ForceStart = 0,
    ForceComplete = 1,
    Reset = 2,
    AdvanceStep = 3,
    GoBackStep = 4
}

public struct GuiderQuestSnapshotEntry
{
    public string QuestId;
    public QuestState State;
    public int StepIndex;

    public GuiderQuestSnapshotEntry(string questId, QuestState state, int stepIndex)
    {
        QuestId = questId;
        State = state;
        StepIndex = stepIndex;
    }
}

public readonly struct GuiderQuestSnapshotReceivedArgs
{
    public PlayerRef TargetPlayer { get; }
    public IReadOnlyList<GuiderQuestSnapshotEntry> Entries { get; }

    public GuiderQuestSnapshotReceivedArgs(PlayerRef targetPlayer, IReadOnlyList<GuiderQuestSnapshotEntry> entries)
    {
        TargetPlayer = targetPlayer;
        Entries = entries;
    }
}
