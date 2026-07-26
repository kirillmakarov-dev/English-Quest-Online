using Newtonsoft.Json;

namespace EnglishKingdom.SaveSystem.Data
{
    /// <summary>
    /// Core player identity and progression snapshot persisted at boot and
    /// updated whenever the player's primary state changes.
    /// </summary>
    public sealed class CoreSaveData
    {
        /// <summary>Current schema version for this data class.</summary>
        public const int CurrentVersion = 1;

        /// <summary>Cloud Save / local storage key.</summary>
        public const string Key = "player_core";

        // ── Fields ────────────────────────────────────────────────────────────────

        /// <summary>Unity Authentication player ID.</summary>
        [JsonProperty("playerId")]
        public string PlayerId { get; set; } = string.Empty;

        /// <summary>Display name chosen or assigned to the player.</summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; } = "Player";

        /// <summary>Total time the player has spent in-game, in seconds.</summary>
        [JsonProperty("totalPlaytimeSeconds")]
        public float TotalPlaytimeSeconds { get; set; }

        /// <summary>UTC unix timestamp of the first session.</summary>
        [JsonProperty("firstLoginAt")]
        public long FirstLoginAt { get; set; }

        /// <summary>UTC unix timestamp of the most recent session start.</summary>
        [JsonProperty("lastLoginAt")]
        public long LastLoginAt { get; set; }

        /// <summary>Index of the avatar/character skin the player has selected.</summary>
        [JsonProperty("selectedAvatarIndex")]
        public int SelectedAvatarIndex { get; set; }

        /// <summary>Current experience points accumulated by the player.</summary>
        [JsonProperty("experiencePoints")]
        public int ExperiencePoints { get; set; }

        /// <summary>Player level derived from <see cref="ExperiencePoints"/>.</summary>
        [JsonProperty("level")]
        public int Level { get; set; } = 1;
    }
}
