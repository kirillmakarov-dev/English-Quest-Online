using System.Collections.Generic;

public interface IQuestAvailabilityService
{
    IReadOnlyList<QuestInfo> GetAvailableToStart(string npcId);
    IReadOnlyList<QuestInfo> GetInProgress(string npcId);
    IReadOnlyList<QuestInfo> GetReadyToTurnIn(string npcId);
    QuestNpcIndicatorState GetBestIndicator(string npcId);
    bool TryGetDefinition(QuestInfo quest, out QuestDefinitionSO definition);
}
