using UnityEngine;
using UnityServiceLocator;

public static class GuiderQuestDisplayHelper
{
    public static string GetDisplayName(string questId, MonoBehaviour context)
    {
        if (string.IsNullOrEmpty(questId))
            return "(unknown quest)";

        if (context != null && ServiceLocator.For(context).TryGet(out IQuestService questService))
        {
            QuestInfo quest = questService.GetQuestById(questId);
            if (quest != null && TryGetDefinition(context, quest, out QuestDefinitionSO definition))
                return string.IsNullOrEmpty(definition.displayName) ? questId : definition.displayName;
        }

        return questId;
    }

    public static string GetStateLabel(QuestState state)
    {
        return state switch
        {
            QuestState.REQUIREMENTS_NOT_MET => "Locked",
            QuestState.CAN_START => "Available",
            QuestState.IN_PROGRESS => "In Progress",
            QuestState.CAN_FINISH => "Ready to Finish",
            QuestState.FINISHED => "Finished",
            _ => state.ToString()
        };
    }

    private static bool TryGetDefinition(MonoBehaviour context, QuestInfo quest, out QuestDefinitionSO definition)
    {
        definition = null;
        if (context == null || quest == null)
            return false;

        if (ServiceLocator.For(context).TryGet(out IQuestAvailabilityService availability)
            && availability.TryGetDefinition(quest, out definition))
        {
            return definition != null;
        }

        if (quest.TryGetDefinition(out definition))
            return definition != null;

        return false;
    }
}
