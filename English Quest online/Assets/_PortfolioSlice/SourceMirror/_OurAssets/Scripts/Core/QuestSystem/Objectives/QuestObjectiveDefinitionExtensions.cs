using System.Collections.Generic;

namespace EnglishQuest.QuestSystem
{
    public static class QuestObjectiveDefinitionExtensions
    {
        public static int GetTargetCount(this QuestObjectiveDefinition definition)
        {
            if (definition == null)
                return 1;
            return definition.count > 0 ? definition.count : 1;
        }

        public static string GetParameter(this QuestObjectiveDefinition definition, string key, string defaultValue = "")
        {
            if (definition?.parameters == null || string.IsNullOrEmpty(key))
                return defaultValue;

            foreach (QuestObjectiveParameter p in definition.parameters)
            {
                if (p != null && p.key == key)
                    return p.value ?? defaultValue;
            }

            return defaultValue;
        }

        public static int GetParameterInt(this QuestObjectiveDefinition definition, string key, int defaultValue = 0)
        {
            string raw = definition.GetParameter(key);
            return int.TryParse(raw, out int value) ? value : defaultValue;
        }
    }
}

