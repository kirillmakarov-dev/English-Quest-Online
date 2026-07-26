using System;
using System.Collections.Generic;
using EnglishKingdom.QuestSystem;

public interface IQuestService
{
    event Action<QuestInfo> OnQuestStarted;
    event Action<QuestInfo> OnQuestUpdated;
    event Action<QuestInfo> OnQuestCompleted;
    event Action<QuestInfo> OnQuestStateChanged;
    event Action<QuestObjectiveProgressEvent> OnObjectiveProgressChanged;
    event Action OnLevelCompleted;

    IReadOnlyList<QuestInfo> AllQuests { get; }
    bool IsLevelCompleted { get; }

    void StartQuest(QuestInfo questInfo);
    void FinishQuest(QuestInfo questInfo);
    void RegisterQuest(QuestInfo questInfo);
    void RegisterQuestStep(QuestStep step, QuestInfo questInfo, int stepIndex);
    bool IsQuestCompleted(QuestInfo questInfo);
    QuestInfo GetQuestById(string questId);

    void ReportObjectiveProgress(QuestInfo questInfo, int stepIndex, int current, int target);
    ObjectiveProgress GetObjectiveProgress(QuestInfo questInfo, int stepIndex);
    void CompleteObjectiveStep(QuestInfo questInfo, int stepIndex, string finalState = "");
    void ReevaluateQuestRequirements();
}
