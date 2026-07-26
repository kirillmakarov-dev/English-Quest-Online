public sealed class ActiveQuestJournalEntry
{
    public string QuestId { get; }
    public string DisplayName { get; }
    public QuestState State { get; }
    public string ObjectiveText { get; }
    public string ProgressText { get; }

    public ActiveQuestJournalEntry(
        string questId,
        string displayName,
        QuestState state,
        string objectiveText,
        string progressText)
    {
        QuestId = questId;
        DisplayName = displayName;
        State = state;
        ObjectiveText = objectiveText;
        ProgressText = progressText;
    }
}
