using UnityEngine;

namespace EnglishKingdom.StatsSystem
{
    /// <summary>
    /// Stub for the future achievement system. Declares the shape of an achievement rule
    /// (a stat reaching a threshold) so the catalog format is stable ahead of time.
    /// Not consumed at runtime yet — no <c>IAchievementService</c> exists in this milestone.
    /// </summary>
    [CreateAssetMenu(fileName = "AchievementDefinition", menuName = ScriptableObjectMenuPaths.CoreStats + "/Achievement Definition")]
    public class AchievementDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _achievementId;
        [SerializeField] private StatDefinitionSO _requiredStat;
        [SerializeField] private int _threshold = 1;
        // RewardDefinition reference — wire to IRewardService when AchievementService ships.

        public string AchievementId => _achievementId;
        public StatDefinitionSO RequiredStat => _requiredStat;
        public int Threshold => _threshold;
    }
}
