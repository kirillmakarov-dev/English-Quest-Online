using Newtonsoft.Json;

namespace EnglishKingdom.SaveSystem.Data
{
    /// <summary>
    /// Stores the player's spendable currency (Coins) balance.
    /// Persisted independently so currency changes don't re-write the full inventory blob.
    /// </summary>
    public sealed class CurrencySaveData
    {
        /// <summary>Current schema version for this data class.</summary>
        public const int CurrentVersion = 1;

        /// <summary>Cloud Save / local storage key.</summary>
        public const string Key = "player_currency";

        // ── Fields ────────────────────────────────────────────────────────────────

        /// <summary>Current spendable coin balance. Always >= 0.</summary>
        [JsonProperty("balance")]
        public int Balance { get; set; }
    }
}
