using System.Collections.Generic;
using EnglishKingdom.QuestSystem;
using UnityEngine;

/// <summary>
/// Canonical open-world quest registration source. Assign all <see cref="QuestLineSO"/>
/// assets here once and reference this registry on <see cref="QuestLineRegistrar"/>
/// so adding a questline does not require scene edits.
/// </summary>
[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.CoreQuestLineRegistry, fileName = "QuestLineRegistry_")]
public class QuestLineRegistrySO : ScriptableObject
{
    public List<QuestLineSO> questLines = new();

    [Header("Shared catalogs")]
    public QuestCatalogSO questCatalog;
    public QuestWorldCatalogSetSO worldCatalogSet;

    [Header("Runtime prefabs")]
    public GameObject questRuntimeShellPrefab;
    public GameObject activeQuestJournalCanvasPrefab;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (questLines == null)
            return;

        var seenLineIds = new HashSet<string>();
        var seenQuestIds = new HashSet<string>();

        for (int i = 0; i < questLines.Count; i++)
        {
            QuestLineSO line = questLines[i];
            if (line == null)
            {
                Debug.LogWarning($"[QuestLineRegistrySO] Null quest line at index {i} in '{name}'.", this);
                continue;
            }

            if (string.IsNullOrEmpty(line.lineId))
            {
                Debug.LogWarning($"[QuestLineRegistrySO] Quest line '{line.name}' has an empty lineId.", this);
            }
            else if (!seenLineIds.Add(line.lineId))
            {
                Debug.LogWarning($"[QuestLineRegistrySO] Duplicate lineId '{line.lineId}' in '{name}'.", this);
            }

            if (line.quests == null)
                continue;

            foreach (QuestDefinitionSO definition in line.quests)
            {
                if (definition == null || string.IsNullOrEmpty(definition.id))
                    continue;

                if (!seenQuestIds.Add(definition.id))
                    Debug.LogWarning(
                        $"[QuestLineRegistrySO] Duplicate quest id '{definition.id}' across lines in '{name}'.",
                        this);
            }
        }
    }
#endif
}
