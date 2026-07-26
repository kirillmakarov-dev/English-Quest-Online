using System.Collections.Generic;
using UnityEngine;
using UnityServiceLocator;

[AddComponentMenu(QuestSystemComponentMenuPaths.Authoring + "/Quest Availability Service")]
public class QuestAvailabilityService : MonoBehaviour, IQuestAvailabilityService
{
    private QuestCatalogSO _catalog;
    private Dictionary<QuestInfo, QuestDefinitionSO> _definitionByQuest = new();
    private IQuestService _questService;

    public void Initialize(
        QuestCatalogSO catalog,
        IReadOnlyDictionary<QuestInfo, QuestDefinitionSO> definitionByQuest)
    {
        _catalog = catalog;
        _definitionByQuest = new Dictionary<QuestInfo, QuestDefinitionSO>(definitionByQuest);
    }

    public IReadOnlyList<QuestInfo> GetAvailableToStart(string npcId)
    {
        return FilterByNpc(npcId, QuestState.CAN_START);
    }

    public IReadOnlyList<QuestInfo> GetInProgress(string npcId)
    {
        return FilterByNpc(npcId, QuestState.IN_PROGRESS);
    }

    public IReadOnlyList<QuestInfo> GetReadyToTurnIn(string npcId)
    {
        return FilterByNpc(npcId, QuestState.CAN_FINISH);
    }

    public QuestNpcIndicatorState GetBestIndicator(string npcId)
    {
        if (GetReadyToTurnIn(npcId).Count > 0)
            return QuestNpcIndicatorState.TurnIn;

        if (GetInProgress(npcId).Count > 0)
            return QuestNpcIndicatorState.InProgress;

        if (GetAvailableToStart(npcId).Count > 0)
            return QuestNpcIndicatorState.Available;

        return QuestNpcIndicatorState.None;
    }

    public bool TryGetDefinition(QuestInfo quest, out QuestDefinitionSO definition)
    {
        if (quest != null && _definitionByQuest.TryGetValue(quest, out definition))
            return true;

        if (quest != null && _catalog != null && _catalog.TryGetDefinition(quest.id, out definition))
            return true;

        if (quest != null && quest.TryGetComponent(out QuestDefinitionLink link) && link.Definition != null)
        {
            definition = link.Definition;
            return true;
        }

        definition = null;
        return false;
    }

    private IQuestService ResolveQuestService()
    {
        if (_questService == null)
            ServiceLocator.For(this)?.TryGet(out _questService);
        return _questService;
    }

    private List<QuestInfo> FilterByNpc(string npcId, QuestState state)
    {
        var results = new List<QuestInfo>();
        IQuestService questService = ResolveQuestService();
        if (string.IsNullOrEmpty(npcId) || questService == null)
            return results;

        foreach (QuestInfo quest in questService.AllQuests)
        {
            if (quest == null || quest.state != state)
                continue;

            if (!TryGetDefinition(quest, out QuestDefinitionSO definition))
                continue;

            if (definition.giverNpcId == npcId)
                results.Add(quest);
        }

        return results;
    }
}
