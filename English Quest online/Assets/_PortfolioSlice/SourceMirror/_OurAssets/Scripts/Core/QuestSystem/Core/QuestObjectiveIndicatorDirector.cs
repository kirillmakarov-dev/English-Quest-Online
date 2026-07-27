using System.Collections.Generic;
using EnglishQuest.QuestSystem;
using TargetIndicators;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Scene-wide director that shows world-space indicator arrows for the current step of each in-progress quest.
/// Replaces manual <see cref="QuestStepIndicator"/> wiring for SO-authored objective quests.
/// </summary>
[AddComponentMenu(QuestSystemComponentMenuPaths.UI + "/Quest Objective Indicator Director")]
public class QuestObjectiveIndicatorDirector : MonoBehaviour
{
    IQuestService _questService;
    TargetIndicatorManager _targetIndicatorManager;
    IQuestWorldTargetRegistry _targetRegistry;

    readonly Dictionary<string, TargetIndicatorId> _indicatorByQuestId = new();

    void Start()
    {
        if (!ServiceLocator.For(this).TryGet(out _questService))
        {
            AppLog.Warning("[QuestObjectiveIndicatorDirector] IQuestService not found.", this);
            return;
        }

        if (!ServiceLocator.For(this).TryGet(out _targetIndicatorManager))
        {
            AppLog.Warning("[QuestObjectiveIndicatorDirector] TargetIndicatorManager not found. Add TargetIndicatorService to the scene.", this);
            return;
        }

        ServiceLocator.For(this).TryGet(out _targetRegistry);

        _questService.OnQuestStarted += OnQuestChanged;
        _questService.OnQuestUpdated += OnQuestChanged;
        _questService.OnQuestCompleted += OnQuestChanged;
        _questService.OnQuestStateChanged += OnQuestChanged;
        _questService.OnObjectiveProgressChanged += OnObjectiveProgressChanged;

        RefreshAllIndicators();
    }

    void OnDestroy()
    {
        if (_questService != null)
        {
            _questService.OnQuestStarted -= OnQuestChanged;
            _questService.OnQuestUpdated -= OnQuestChanged;
            _questService.OnQuestCompleted -= OnQuestChanged;
            _questService.OnQuestStateChanged -= OnQuestChanged;
            _questService.OnObjectiveProgressChanged -= OnObjectiveProgressChanged;
        }

        ClearAllIndicators();
    }

    void OnQuestChanged(QuestInfo _) => RefreshAllIndicators();

    void OnObjectiveProgressChanged(QuestObjectiveProgressEvent _) => RefreshAllIndicators();

    void RefreshAllIndicators()
    {
        if (_questService == null || _targetIndicatorManager == null)
            return;

        var activeQuestIds = new HashSet<string>();

        foreach (QuestInfo quest in _questService.AllQuests)
        {
            if (quest == null || quest.state != QuestState.IN_PROGRESS)
                continue;

            activeQuestIds.Add(quest.id);
            RefreshIndicatorForQuest(quest);
        }

        var staleQuestIds = new List<string>();
        foreach (string questId in _indicatorByQuestId.Keys)
        {
            if (!activeQuestIds.Contains(questId))
                staleQuestIds.Add(questId);
        }

        foreach (string questId in staleQuestIds)
            RemoveIndicator(questId);
    }

    void RefreshIndicatorForQuest(QuestInfo quest)
    {
        RemoveIndicator(quest.id);

        if (!TryResolveTargetTransform(quest, out Transform target))
            return;

        if (_targetIndicatorManager.TryAddTarget(target, out TargetIndicator indicator))
            _indicatorByQuestId[quest.id] = indicator.Id;
    }

    bool TryResolveTargetTransform(QuestInfo quest, out Transform target)
    {
        target = null;
        if (quest == null || !quest.CurrentStepExists())
            return false;

        int stepIndex = quest.currentStepIndex;

        if (quest.UsesObjectives() && quest.TryGetObjectiveDefinition(stepIndex, out QuestObjectiveDefinition definition))
        {
            if (definition.type == QuestObjectiveType.Custom)
                return false;

            if (_targetRegistry != null &&
                _targetRegistry.TryGetTransform(definition.type, definition.targetId, out target))
            {
                return true;
            }

            AppLog.Warning($"[QuestObjectiveIndicatorDirector] No world target registered for quest '{quest.id}' step {stepIndex} ({definition.type}, '{definition.targetId}').");
            return false;
        }

        if (quest.UsesLegacySteps() &&
            quest.questSteps != null &&
            stepIndex >= 0 &&
            stepIndex < quest.questSteps.Count &&
            quest.questSteps[stepIndex] != null)
        {
            target = quest.questSteps[stepIndex].transform;
            return true;
        }

        return false;
    }

    void RemoveIndicator(string questId)
    {
        if (string.IsNullOrEmpty(questId) || _targetIndicatorManager == null)
            return;

        if (!_indicatorByQuestId.TryGetValue(questId, out TargetIndicatorId indicatorId))
            return;

        _targetIndicatorManager.TryRemoveTarget(indicatorId);
        _indicatorByQuestId.Remove(questId);
    }

    void ClearAllIndicators()
    {
        if (_targetIndicatorManager == null)
        {
            _indicatorByQuestId.Clear();
            return;
        }

        foreach (TargetIndicatorId indicatorId in _indicatorByQuestId.Values)
            _targetIndicatorManager.TryRemoveTarget(indicatorId);

        _indicatorByQuestId.Clear();
    }
}

