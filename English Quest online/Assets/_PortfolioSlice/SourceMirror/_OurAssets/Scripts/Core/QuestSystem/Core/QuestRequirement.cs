using System.Collections.Generic;

[System.Serializable]
public class QuestRequirement
{
    public int minPlayerLevel;
    public List<QuestInfo> requiredQuests;
    public List<string> requiredQuestIds;

    public bool IsMet(IQuestService questService = null, IPlayerLevelProvider levelProvider = null)
    {
        if (minPlayerLevel > 0)
        {
            int level = levelProvider != null ? levelProvider.CurrentLevel : 1;
            if (level < minPlayerLevel)
                return false;
        }

        if (requiredQuests != null)
        {
            foreach (QuestInfo req in requiredQuests)
            {
                if (req == null || req.state != QuestState.FINISHED)
                    return false;
            }
        }

        if (requiredQuestIds != null && requiredQuestIds.Count > 0)
        {
            if (questService == null)
                return false;

            foreach (string questId in requiredQuestIds)
            {
                if (string.IsNullOrEmpty(questId))
                    continue;

                QuestInfo req = questService.GetQuestById(questId);
                if (req == null || req.state != QuestState.FINISHED)
                    return false;
            }
        }

        return true;
    }
}
