using System.Collections.Generic;
using Newtonsoft.Json;

namespace EnglishKingdom.SaveSystem.Data
{
    /// <summary>
    /// Records the player's advancement through the game's content: quests,
    /// rooms unlocked, achievements, and milestones.
    /// Loaded lazily after the main menu to avoid blocking the boot sequence.
    /// </summary>
    public sealed class ProgressSaveData
    {
        /// <summary>Current schema version for this data class.</summary>
        public const int CurrentVersion = 2;

        /// <summary>Cloud Save / local storage key.</summary>
        public const string Key = "player_progress";

        /// <summary>Map of quest ID to completion state.</summary>
        [JsonProperty("questCompletion")]
        public Dictionary<string, bool> QuestCompletion { get; set; } = new Dictionary<string, bool>();

        /// <summary>Map of quest ID to detailed runtime progress.</summary>
        [JsonProperty("questProgress")]
        public Dictionary<string, QuestProgressEntry> QuestProgress { get; set; } = new Dictionary<string, QuestProgressEntry>();

        /// <summary>IDs of rooms and zones the player has discovered.</summary>
        [JsonProperty("unlockedRoomIds")]
        public List<string> UnlockedRoomIds { get; set; } = new List<string>();

        /// <summary>Index of the current main-story chapter (0-based).</summary>
        [JsonProperty("currentChapterIndex")]
        public int CurrentChapterIndex { get; set; }

        /// <summary>IDs of achievements the player has earned.</summary>
        [JsonProperty("earnedAchievementIds")]
        public List<string> EarnedAchievementIds { get; set; } = new List<string>();

        /// <summary>Map of mini-game ID to star rating (0-3).</summary>
        [JsonProperty("miniGameStars")]
        public Dictionary<string, int> MiniGameStars { get; set; } = new Dictionary<string, int>();

        /// <summary>Name of the scene the player was in when they last saved.</summary>
        [JsonProperty("lastSceneName")]
        public string LastSceneName { get; set; } = string.Empty;

        /// <summary>UTC unix timestamp of when this progress snapshot was last written.</summary>
        [JsonProperty("lastSavedAt")]
        public long LastSavedAt { get; set; }
    }
}
