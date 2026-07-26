using System.Collections.Generic;
using Newtonsoft.Json;

namespace EnglishKingdom.SaveSystem.Data
{
    /// <summary>
    /// Generic key/value store for gameplay statistics (e.g. enemies defeated, items collected).
    /// Stat identity and aggregation rules live in <c>StatDefinitionSO</c> assets; this class only
    /// persists the resulting integer values, keyed by <c>StatDefinitionSO.StatId</c>.
    /// </summary>
    public sealed class StatsSaveData
    {
        /// <summary>Current schema version for this data class.</summary>
        public const int CurrentVersion = 1;

        /// <summary>Cloud Save / local storage key.</summary>
        public const string Key = "player_stats";

        /// <summary>Map of stat id → current value.</summary>
        [JsonProperty("values")]
        public Dictionary<string, int> Values { get; set; } = new Dictionary<string, int>();
    }
}
