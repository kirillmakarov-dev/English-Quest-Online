using System.Collections.Generic;
using UnityEngine;

namespace EnglishKingdom.StatsSystem
{
    /// <summary>
    /// Registry of every <see cref="StatDefinitionSO"/> in the game. Used for editor-time
    /// duplicate-id validation today, and will back stat lookups for the future achievement
    /// system (mapping stat ids back to their definitions).
    /// </summary>
    [CreateAssetMenu(fileName = "StatCatalog", menuName = ScriptableObjectMenuPaths.CoreStats + "/Stat Catalog")]
    public class StatCatalogSO : ScriptableObject
    {
        [SerializeField] private List<StatDefinitionSO> _stats = new();

        public IReadOnlyList<StatDefinitionSO> Stats => _stats;

        public StatDefinitionSO GetByStatId(string statId)
        {
            if (string.IsNullOrEmpty(statId))
                return null;

            foreach (StatDefinitionSO stat in _stats)
            {
                if (stat != null && stat.StatId == statId)
                    return stat;
            }

            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var seenIds = new HashSet<string>();
            foreach (StatDefinitionSO stat in _stats)
            {
                if (stat == null || string.IsNullOrEmpty(stat.StatId))
                    continue;

                if (!seenIds.Add(stat.StatId))
                    Debug.LogWarning($"[StatCatalogSO] Duplicate stat id '{stat.StatId}' found in catalog '{name}'.", this);
            }
        }
#endif
    }
}
