using System;
using System.Collections.Generic;
using EnglishKingdom.QuestSystem;
using UnityEngine;
using UnityServiceLocator;

[AddComponentMenu(QuestSystemComponentMenuPaths.UI + "/Active Quest Journal Presenter")]
public class ActiveQuestJournalPresenter : MonoBehaviour
{
    readonly List<ActiveQuestJournalEntry> _entries = new();

    IQuestService _questService;
    IQuestWorldResolver _worldResolver;

    public event Action OnEntriesChanged;
    public IReadOnlyList<ActiveQuestJournalEntry> Entries => _entries;

    void Start()
    {
        if (!ServiceLocator.For(this).TryGet(out _questService))
        {
            AppLog.Warning("[ActiveQuestJournalPresenter] IQuestService not found.", this);
            return;
        }

        ServiceLocator.For(this).TryGet(out _worldResolver);

        _questService.OnQuestStarted += OnQuestEvent;
        _questService.OnQuestUpdated += OnQuestEvent;
        _questService.OnQuestCompleted += OnQuestEvent;
        _questService.OnQuestStateChanged += OnQuestEvent;
        _questService.OnObjectiveProgressChanged += OnObjectiveProgressChanged;

        RebuildEntries();
    }

    void OnDestroy()
    {
        if (_questService == null)
            return;

        _questService.OnQuestStarted -= OnQuestEvent;
        _questService.OnQuestUpdated -= OnQuestEvent;
        _questService.OnQuestCompleted -= OnQuestEvent;
        _questService.OnQuestStateChanged -= OnQuestEvent;
        _questService.OnObjectiveProgressChanged -= OnObjectiveProgressChanged;
    }

    void OnQuestEvent(QuestInfo _) => RebuildEntries();

    void OnObjectiveProgressChanged(QuestObjectiveProgressEvent _) => RebuildEntries();

    public void RebuildEntries()
    {
        _entries.Clear();

        if (_questService == null)
        {
            OnEntriesChanged?.Invoke();
            return;
        }

        foreach (QuestInfo quest in _questService.AllQuests)
        {
            if (quest == null || !ActiveQuestDisplayHelper.IsActiveJournalState(quest.state))
                continue;

            ActiveQuestJournalEntry entry = ActiveQuestDisplayHelper.BuildEntry(quest, _questService, _worldResolver);
            if (entry != null)
                _entries.Add(entry);
        }

        OnEntriesChanged?.Invoke();
    }
}
