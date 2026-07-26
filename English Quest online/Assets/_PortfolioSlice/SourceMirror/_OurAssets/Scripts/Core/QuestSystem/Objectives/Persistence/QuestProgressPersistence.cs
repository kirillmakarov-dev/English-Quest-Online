using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EnglishKingdom.LevelSystem;
using EnglishKingdom.SaveSystem;
using EnglishKingdom.SaveSystem.Data;
using EnglishKingdom.QuestSystem;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishKingdom.QuestSystem
{
    [DefaultExecutionOrder(20)]
    public class QuestProgressPersistence : MonoBehaviour
    {
        private const float LevelServiceWaitTimeoutSeconds = 2f;

        [SerializeField] private bool autoSave = true;
        [SerializeField] private float saveDebounceSeconds = 1f;

        private ProgressSaveData _progressData;
        private float _saveScheduledAt = -1f;
        private bool _dirty;
        private QuestManager _questManager;

        public bool IsLoaded { get; private set; }

        private void Start()
        {
            if (!ServiceLocator.For(this).TryGet(out QuestManager questManager))
                return;

            _questManager = questManager;
            questManager.BeginProgressLoad(questManager.LoadQuestStateEnabled);
        }

        public async UniTaskVoid LoadAndApplyAsync(QuestManager questManager, bool loadQuestState)
        {
            _questManager = questManager;
            if (!loadQuestState || questManager == null)
            {
                IsLoaded = true;
                return;
            }

            try
            {
                _progressData = await SaveManager.LoadProgressAsync();
                MigrateLegacyCompletion(_progressData);
                ApplyToQuests(questManager);
                questManager.RefreshMiniGameBindings();
                await WaitForLevelServiceReadyAsync();
                questManager.ReevaluateQuestRequirements();
            }
            catch (Exception ex)
            {
                AppLog.Warning($"[QuestProgressPersistence] Failed to load progress: {ex.Message}");
                _progressData = new ProgressSaveData();
            }

            IsLoaded = true;
        }

        private static async UniTask WaitForLevelServiceReadyAsync()
        {
            float deadline = Time.realtimeSinceStartup + LevelServiceWaitTimeoutSeconds;

            while (Time.realtimeSinceStartup < deadline)
            {
                if (ServiceLocator.Global != null &&
                    ServiceLocator.Global.TryGet(out ILevelService levelService) &&
                    levelService.IsReady)
                    return;

                await UniTask.Yield();
            }
        }

        public void MarkDirty() => _dirty = true;

        private void Update()
        {
            if (!autoSave || !_dirty || _questManager == null || _progressData == null)
                return;

            if (Time.unscaledTime < _saveScheduledAt)
                return;

            _dirty = false;
            CaptureFromQuests(_questManager);
            SaveManager.SaveProgressAsync(_progressData).Forget();
        }

        public void ScheduleSave()
        {
            _dirty = true;
            _saveScheduledAt = Time.unscaledTime + saveDebounceSeconds;
        }

        public void CaptureFromQuests(QuestManager questManager)
        {
            if (_progressData == null)
                _progressData = new ProgressSaveData();

            foreach (QuestInfo quest in questManager.AllQuests)
            {
                if (quest == null || string.IsNullOrEmpty(quest.id))
                    continue;

                _progressData.QuestProgress[quest.id] = quest.CreateProgressEntry();
                if (quest.state == QuestState.FINISHED)
                    _progressData.QuestCompletion[quest.id] = true;
            }
        }

        private void ApplyToQuests(QuestManager questManager)
        {
            foreach (QuestInfo quest in questManager.AllQuests)
            {
                if (quest == null || string.IsNullOrEmpty(quest.id))
                    continue;

                if (_progressData.QuestProgress.TryGetValue(quest.id, out QuestProgressEntry entry))
                    quest.ApplyProgressEntry(entry);
            }
        }

        private static void MigrateLegacyCompletion(ProgressSaveData data)
        {
            if (data.QuestCompletion == null)
                return;

            foreach (KeyValuePair<string, bool> pair in data.QuestCompletion)
            {
                if (!pair.Value || data.QuestProgress.ContainsKey(pair.Key))
                    continue;

                data.QuestProgress[pair.Key] = new QuestProgressEntry
                {
                    State = (int)QuestState.FINISHED,
                    CurrentStepIndex = 0,
                    Steps = new List<StepProgressEntry>()
                };
            }
        }
    }
}
