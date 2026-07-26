using System.Collections.Generic;
using Newtonsoft.Json;

namespace EnglishKingdom.SaveSystem.Data
{
    /// <summary>
    /// Tracks which ability set the player has been granted via quest events.
    /// Persisted so the override survives scene reloads and game restarts.
    /// </summary>
    public sealed class AbilitySaveData
    {
        public const int CurrentVersion = 1;
        public const string Key = "player_abilities";

        /// <summary>
        /// The <see cref="AbilityDefinitionSO.abilityId"/> values of every ability
        /// in the granted set. Empty means no override — use the prefab defaults.
        /// </summary>
        [JsonProperty("grantedAbilityIds")]
        public List<string> GrantedAbilityIds { get; set; } = new List<string>();
    }
}
