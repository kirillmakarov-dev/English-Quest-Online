using UnityEngine;

namespace EnglishKingdom.StatsSystem
{
    /// <summary>
    /// Designer-authored definition of a single trackable statistic (e.g. "goblins defeated",
    /// "coins collected"). The asset's <see cref="StatId"/> is the persistence key; gameplay
    /// code and content reference the asset itself rather than a raw string.
    /// </summary>
    [CreateAssetMenu(fileName = "StatDefinition", menuName = ScriptableObjectMenuPaths.CoreStats + "/Stat Definition")]
    public class StatDefinitionSO : ScriptableObject
    {
        [Tooltip("Stable persistence key, e.g. \"combat.goblin_defeated\". Do not change after release.")]
        [SerializeField] private string _statId;

        [Tooltip("Human-readable name, for debugging and future UI.")]
        [SerializeField] private string _displayName;

        [Tooltip("How new values combine with the stored value on Apply().")]
        [SerializeField] private StatAggregation _aggregation = StatAggregation.Sum;

        public string StatId => _statId;
        public string DisplayName => _displayName;
        public StatAggregation Aggregation => _aggregation;
    }
}
