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

        private readonly Dictionary<string, QuestProgressEntry> _progress = new Dictionary<string, QuestProgressEntry>();
        private readonly Dictionary<string, bool> _completion = new Dictionary<string, bool>();
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
            foreach (QuestInfo quest in questManager.AllQuests)
            {
                if (quest == null || string.IsNullOrEmpty(quest.id))
                    continue;

                _progress[quest.id] = quest.CreateProgressEntry();
                if (quest.state == QuestState.FINISHED)
                    _completion[quest.id] = true;
            }
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
    }
}

