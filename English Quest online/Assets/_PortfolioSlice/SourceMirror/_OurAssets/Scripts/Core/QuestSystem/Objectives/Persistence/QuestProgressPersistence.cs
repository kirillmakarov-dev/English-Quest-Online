using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EnglishQuest.LevelSystem;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishQuest.QuestSystem
{
    [DefaultExecutionOrder(20)]
    public class QuestProgressPersistence : MonoBehaviour
    {
        private const float LevelServiceWaitTimeoutSeconds = 2f;

        [SerializeField] private bool autoSave = true;
        [SerializeField] private float saveDebounceSeconds = 1f;
        [SerializeField] private string saveKey = "englishquest.portfolio.questprogress";

        private readonly Dictionary<string, QuestProgressEntry> _progress = new Dictionary<string, QuestProgressEntry>();
        private readonly Dictionary<string, bool> _completion = new Dictionary<string, bool>();
        private float _saveScheduledAt = -1f;
        private bool _dirty;
        private QuestManager _questManager;

        public bool IsLoaded { get; private set; }

        private void Start()
        {
            IQuestService questService = null;
            ServiceLocator.For(this)?.TryGet(out questService);
            _questManager = questService as QuestManager;
            if (_questManager == null && QuestManager.HasInstance)
                _questManager = QuestManager.Instance;
            if (_questManager == null)
                return;

            _questManager.BeginProgressLoad(_questManager.LoadQuestStateEnabled);
        }

        public async UniTaskVoid LoadAndApplyAsync(QuestManager questManager, bool loadQuestState)
        {
            _questManager = questManager;
            if (questManager == null)
            {
                IsLoaded = true;
                return;
            }

            ReadPersistedPayload();

            if (!loadQuestState && _progress.Count == 0 && _completion.Count == 0)
            {
                IsLoaded = true;
                return;
            }

            try
            {
                ApplyToQuests(questManager);
                questManager.RefreshMiniGameBindings();
                await WaitForLevelServiceReadyAsync();
                questManager.ReevaluateQuestRequirements();
                questManager.RefreshLevelCompletionState();
            }
            catch (Exception ex)
            {
                AppLog.Warning($"[QuestProgressPersistence] Failed to apply in-memory progress: {ex.Message}");
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
            if (!autoSave || !_dirty || _questManager == null)
                return;

            if (Time.unscaledTime < _saveScheduledAt)
                return;

            _dirty = false;
            CaptureFromQuests(_questManager);
        }

        public void ScheduleSave()
        {
            _dirty = true;
            _saveScheduledAt = Time.unscaledTime + saveDebounceSeconds;
        }

        public void CaptureFromQuests(QuestManager questManager)
        {
            _progress.Clear();
            _completion.Clear();

            bool levelCompleted = questManager != null && questManager.IsLevelCompleted;
            foreach (QuestInfo quest in questManager.AllQuests)
            {
                if (quest == null || string.IsNullOrEmpty(quest.id))
                    continue;

                _progress[quest.id] = quest.CreateProgressEntry();
                if (quest.state == QuestState.FINISHED)
                    _completion[quest.id] = true;
            }

            WritePersistedPayload(levelCompleted);
        }

        private void ApplyToQuests(QuestManager questManager)
        {
            foreach (QuestInfo quest in questManager.AllQuests)
            {
                if (quest == null || string.IsNullOrEmpty(quest.id))
                    continue;

                if (_progress.TryGetValue(quest.id, out QuestProgressEntry entry))
                    quest.ApplyProgressEntry(entry);
                else if (_completion.TryGetValue(quest.id, out bool completed) && completed)
                    quest.SetState(QuestState.FINISHED);
            }
        }

        public void ClearSavedProgress()
        {
            _progress.Clear();
            _completion.Clear();
            QuestProgressLocalStore.Clear(saveKey);
            _dirty = false;
            _saveScheduledAt = -1f;
        }

        private void ReadPersistedPayload()
        {
            _progress.Clear();
            _completion.Clear();

            if (!QuestProgressLocalStore.TryLoad(out QuestProgressSavePayload payload, saveKey) ||
                payload?.Quests == null)
                return;

            foreach (QuestProgressSaveRecord record in payload.Quests)
            {
                if (record == null || string.IsNullOrEmpty(record.QuestId))
                    continue;

                if (record.Progress != null)
                    _progress[record.QuestId] = record.Progress;

                if (record.Finished)
                    _completion[record.QuestId] = true;
            }
        }

        private void WritePersistedPayload(bool levelCompleted)
        {
            var payload = new QuestProgressSavePayload
            {
                LevelCompleted = levelCompleted
            };

            foreach (KeyValuePair<string, QuestProgressEntry> pair in _progress)
            {
                payload.Quests.Add(new QuestProgressSaveRecord
                {
                    QuestId = pair.Key,
                    Progress = pair.Value,
                    Finished = _completion.TryGetValue(pair.Key, out bool completed) && completed
                });
            }

            QuestProgressLocalStore.Save(payload, saveKey);
        }
    }
}

