namespace EnglishKingdom.QuestSystem
{
    public interface ICustomQuestObjectiveHandler
    {
        string HandlerId { get; }
        bool TryHandle(QuestInfo quest, QuestObjectiveDefinition definition, int stepIndex, string payload);
    }
}
