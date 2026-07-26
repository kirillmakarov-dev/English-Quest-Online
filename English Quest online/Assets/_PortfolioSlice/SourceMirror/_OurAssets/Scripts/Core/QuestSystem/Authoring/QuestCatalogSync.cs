using System.Collections.Generic;

public static class QuestCatalogSync
{
    public static List<QuestDefinitionSO> BuildSyncedDefinitions(QuestLineSO line)
    {
        var synced = new List<QuestDefinitionSO>();
        if (line?.quests == null)
            return synced;

        var seen = new HashSet<string>();
        foreach (QuestDefinitionSO definition in line.quests)
        {
            if (definition == null || string.IsNullOrEmpty(definition.id))
                continue;

            if (!seen.Add(definition.id))
                continue;

            synced.Add(definition);
        }

        return synced;
    }
}
