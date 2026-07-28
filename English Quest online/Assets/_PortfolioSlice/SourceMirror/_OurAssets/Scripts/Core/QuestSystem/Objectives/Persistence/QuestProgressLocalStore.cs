using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnglishQuest.QuestSystem
{
    [Serializable]
    internal sealed class QuestProgressSavePayload
    {
        public int Version = 1;
        public bool LevelCompleted;
        public List<QuestProgressSaveRecord> Quests = new();
    }

    [Serializable]
    internal sealed class QuestProgressSaveRecord
    {
        public string QuestId;
        public bool Finished;
        public QuestProgressEntry Progress;
    }

    internal static class QuestProgressLocalStore
    {
        private const string DefaultSaveKey = "englishquest.portfolio.questprogress";

        public static bool HasSavedData(string saveKey = null)
        {
            return PlayerPrefs.HasKey(ResolveKey(saveKey));
        }

        public static bool TryLoad(out QuestProgressSavePayload payload, string saveKey = null)
        {
            payload = null;
            string key = ResolveKey(saveKey);
            if (!PlayerPrefs.HasKey(key))
                return false;

            string json = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                payload = JsonUtility.FromJson<QuestProgressSavePayload>(json);
                return payload != null;
            }
            catch (Exception ex)
            {
                AppLog.Warning($"[QuestProgressLocalStore] Failed to read save payload: {ex.Message}");
                return false;
            }
        }

        public static void Save(QuestProgressSavePayload payload, string saveKey = null)
        {
            if (payload == null)
                return;

            string json = JsonUtility.ToJson(payload);
            PlayerPrefs.SetString(ResolveKey(saveKey), json);
            PlayerPrefs.Save();
        }

        public static void Clear(string saveKey = null)
        {
            PlayerPrefs.DeleteKey(ResolveKey(saveKey));
            PlayerPrefs.Save();
        }

        private static string ResolveKey(string saveKey)
        {
            return string.IsNullOrWhiteSpace(saveKey) ? DefaultSaveKey : saveKey;
        }
    }
}
