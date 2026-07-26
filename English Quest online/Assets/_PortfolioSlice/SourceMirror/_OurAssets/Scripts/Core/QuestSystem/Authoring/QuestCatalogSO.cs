using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.CoreQuestCatalog, fileName = "QuestCatalog_")]
public class QuestCatalogSO : ScriptableObject
{
    public List<QuestDefinitionSO> definitions = new();

    public bool TryGetDefinition(string questId, out QuestDefinitionSO definition)
    {
        definition = null;
        if (string.IsNullOrEmpty(questId) || definitions == null)
            return false;

        foreach (QuestDefinitionSO entry in definitions)
        {
            if (entry != null && entry.id == questId)
            {
                definition = entry;
                return true;
            }
        }

        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        var seenIds = new HashSet<string>();
        foreach (QuestDefinitionSO entry in definitions)
        {
            if (entry == null || string.IsNullOrEmpty(entry.id))
                continue;

            if (!seenIds.Add(entry.id))
                Debug.LogWarning($"[QuestCatalogSO] Duplicate quest id '{entry.id}' in catalog '{name}'.", this);
        }
    }
#endif
}
