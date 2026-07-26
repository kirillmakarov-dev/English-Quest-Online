using Newtonsoft.Json;

namespace EnglishKingdom.SaveSystem.Data
{
    /// <summary>
    /// Player-configurable preferences that affect audio, graphics, and
    /// accessibility.  Loaded eagerly at boot so the first frame honours the
    /// player's choices.
    /// </summary>
    public sealed class SettingsSaveData
    {
        /// <summary>Current schema version for this data class.</summary>
        public const int CurrentVersion = 1;

        /// <summary>Cloud Save / local storage key.</summary>
        public const string Key = "player_settings";

        // ── Audio ─────────────────────────────────────────────────────────────────

        /// <summary>Master volume in the range [0, 1].</summary>
        [JsonProperty("masterVolume")]
        public float MasterVolume { get; set; } = 1f;

        /// <summary>Music volume in the range [0, 1].</summary>
        [JsonProperty("musicVolume")]
        public float MusicVolume { get; set; } = 0.8f;

        /// <summary>Sound-effects volume in the range [0, 1].</summary>
        [JsonProperty("sfxVolume")]
        public float SfxVolume { get; set; } = 1f;

        // ── Graphics ─────────────────────────────────────────────────────────────

        /// <summary>Selected quality preset index (maps to Unity Quality Settings levels).</summary>
        [JsonProperty("qualityLevel")]
        public int QualityLevel { get; set; } = 2;

        // ── Accessibility ─────────────────────────────────────────────────────────

        /// <summary>BCP-47 language tag for the UI locale (e.g. "en", "he").</summary>
        [JsonProperty("languageCode")]
        public string LanguageCode { get; set; } = "en";

        /// <summary>Camera sensitivity multiplier in the range [0.1, 3.0].</summary>
        [JsonProperty("cameraSensitivity")]
        public float CameraSensitivity { get; set; } = 1f;
    }
}
