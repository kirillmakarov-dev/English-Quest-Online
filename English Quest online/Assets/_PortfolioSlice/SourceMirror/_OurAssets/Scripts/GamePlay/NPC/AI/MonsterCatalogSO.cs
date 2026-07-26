using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registry of every <see cref="MonsterDefinitionSO"/> in the game for spawners and encounter design.
/// </summary>
[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.GameplayMonsters + "/Monster Catalog", fileName = "MonsterCatalog")]
public class MonsterCatalogSO : ScriptableObject
{
    [SerializeField] private List<MonsterDefinitionSO> _monsters = new();

    public IReadOnlyList<MonsterDefinitionSO> Monsters => _monsters;

    public bool TryGetById(string monsterId, out MonsterDefinitionSO monster)
    {
        monster = null;
        if (string.IsNullOrEmpty(monsterId))
            return false;

        foreach (MonsterDefinitionSO entry in _monsters)
        {
            if (entry == null || string.IsNullOrEmpty(entry.MonsterId))
                continue;

            if (entry.MonsterId == monsterId)
            {
                monster = entry;
                return true;
            }
        }

        return false;
    }

    public MonsterDefinitionSO GetByDisplayName(string displayName)
    {
        if (string.IsNullOrEmpty(displayName))
            return null;

        foreach (MonsterDefinitionSO monster in _monsters)
        {
            if (monster != null && monster.DisplayName == displayName)
                return monster;
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        var seenIds = new HashSet<string>();
        var seenNames = new HashSet<string>();

        foreach (MonsterDefinitionSO monster in _monsters)
        {
            if (monster == null)
                continue;

            if (!string.IsNullOrEmpty(monster.MonsterId))
            {
                if (!seenIds.Add(monster.MonsterId))
                    Debug.LogWarning($"[MonsterCatalogSO] Duplicate monsterId '{monster.MonsterId}' in catalog '{name}'.", this);
            }

            if (!string.IsNullOrEmpty(monster.DisplayName))
            {
                if (!seenNames.Add(monster.DisplayName))
                    Debug.LogWarning($"[MonsterCatalogSO] Duplicate display name '{monster.DisplayName}' in catalog '{name}'.", this);
            }
        }
    }
#endif
}
