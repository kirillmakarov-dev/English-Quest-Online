using System.Collections.Generic;
using EnglishQuest.QuestSystem;
using UnityEngine;
using UnityServiceLocator;

[DefaultExecutionOrder(10)]
[AddComponentMenu(QuestSystemComponentMenuPaths.Authoring + "/Quest Line Registrar")]
public class QuestLineRegistrar : MonoBehaviour
{
    [SerializeField] private QuestLineRegistrySO registry;

    [Header("Legacy (fallback when registry is unset)")]
    [HideInInspector] [SerializeField] private QuestLineSO questLine;
    [HideInInspector] [SerializeField] private QuestCatalogSO questCatalog;
    [HideInInspector] [SerializeField] private QuestWorldCatalogSetSO worldCatalogSet;
    [SerializeField] private Transform questParent;
    [HideInInspector] [SerializeField] private GameObject activeQuestJournalCanvasPrefab;
    [HideInInspector] [SerializeField] private GameObject questRuntimeShellPrefab;

    private readonly Dictionary<QuestInfo, QuestDefinitionSO> _definitionByQuest = new();
    private QuestAvailabilityService _availabilityService;

    public QuestLineRegistrySO Registry => registry;

    /// <summary>First registered line, or the legacy single-line field when no registry is assigned.</summary>
    public QuestLineSO QuestLine
    {
        get
        {
            if (registry != null && registry.questLines != null)
            {
                foreach (QuestLineSO line in registry.questLines)
                {
                    if (line != null)
                        return line;
                }
            }

            return questLine;
        }
    }

    public IReadOnlyDictionary<QuestInfo, QuestDefinitionSO> DefinitionByQuest => _definitionByQuest;

    private void Awake()
    {
        if (!TryResolveLines(out List<QuestLineSO> lines))
        {
            AppLog.Error(
                "[QuestLineRegistrar] QuestLineRegistrySO (or legacy QuestLineSO) is not assigned.",
                this);
            return;
        }

        if (!ValidateResolvedLines(lines))
            return;

        if (!ServiceLocator.For(this).TryGet<IQuestService>(out IQuestService questService))
        {
            AppLog.Error("[QuestLineRegistrar] IQuestService not found.", this);
            return;
        }

        RegisterQuestLines(questService, lines);
        RegisterAvailabilityService();
        BindNpcQuestGivers(lines);
        RegisterObjectiveRuntime(questService, lines);
        EnsureWorldTargetRegistry();
        EnsureObjectiveIndicatorDirector();
        EnsureActiveQuestJournal();
    }

    private void EnsureWorldTargetRegistry()
    {
        if (GetComponent<QuestWorldTargetRegistrar>() == null)
            gameObject.AddComponent<QuestWorldTargetRegistrar>();
    }

    private void EnsureObjectiveIndicatorDirector()
    {
        if (GetComponent<QuestObjectiveIndicatorDirector>() == null)
            gameObject.AddComponent<QuestObjectiveIndicatorDirector>();
    }

    private void EnsureActiveQuestJournal()
    {
        if (FindFirstObjectByType<ActiveQuestJournalUI>(FindObjectsInactive.Include) != null)
            return;

        GameObject prefab = ResolveActiveQuestJournalPrefab();
        if (prefab == null)
        {
            AppLog.Warning("[QuestLineRegistrar] Active quest journal canvas prefab is not assigned.", this);
            return;
        }

        GameObject instance = Instantiate(prefab, transform);
        instance.name = prefab.name;
    }

    private GameObject ResolveActiveQuestJournalPrefab()
    {
        if (registry != null && registry.activeQuestJournalCanvasPrefab != null)
            return registry.activeQuestJournalCanvasPrefab;

        if (activeQuestJournalCanvasPrefab != null)
            return activeQuestJournalCanvasPrefab;

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_OurAssets/Art/Prefabs/UI/Quests/ActiveQuestJournalCanvas.prefab");
#else
        return null;
#endif
    }

    private void OnDestroy()
    {
        if (_availabilityService != null)
            ServiceLocator.DeregisterFor<IQuestAvailabilityService>(this);

        ServiceLocator.DeregisterFor<IQuestWorldResolver>(this);
    }

    private GameObject ResolveQuestRuntimeShellPrefab()
    {
        if (registry != null && registry.questRuntimeShellPrefab != null)
            return registry.questRuntimeShellPrefab;

        if (questRuntimeShellPrefab != null)
            return questRuntimeShellPrefab;

#if UNITY_EDITOR
        GameObject asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_OurAssets/Data/Quests/Prefabs/QuestRuntimeShell.prefab");
        if (asset != null)
            return asset;
#else
#endif
        GameObject fallback = new GameObject("QuestRuntimeShell");
        fallback.hideFlags = HideFlags.HideAndDontSave;
        return fallback;
    }

    private bool TryResolveLines(out List<QuestLineSO> lines)
    {
        lines = new List<QuestLineSO>();

        if (registry != null && registry.questLines != null)
        {
            foreach (QuestLineSO line in registry.questLines)
            {
                if (line != null)
                    lines.Add(line);
            }
        }

        if (lines.Count == 0 && questLine != null)
            lines.Add(questLine);

        return lines.Count > 0;
    }

    private bool ValidateResolvedLines(IReadOnlyList<QuestLineSO> lines)
    {
        if (lines == null || lines.Count == 0)
            return false;

        var seenLineIds = new HashSet<string>();
        var seenQuestIds = new HashSet<string>();
        var seenNpcIds = new HashSet<string>();

        for (int i = 0; i < lines.Count; i++)
        {
            QuestLineSO line = lines[i];
            if (line == null)
            {
                AppLog.Error("[QuestLineRegistrar] Resolved quest lines contain a null entry.", this);
                return false;
            }

            if (string.IsNullOrWhiteSpace(line.lineId))
            {
                AppLog.Error($"[QuestLineRegistrar] Quest line '{line.name}' has an empty lineId.", this);
                return false;
            }

            if (string.IsNullOrWhiteSpace(line.npcId))
            {
                AppLog.Error($"[QuestLineRegistrar] Quest line '{line.lineId}' has an empty npcId.", this);
                return false;
            }

            if (!seenLineIds.Add(line.lineId))
            {
                AppLog.Error($"[QuestLineRegistrar] Duplicate line id '{line.lineId}' detected.", this);
                return false;
            }

            if (!seenNpcIds.Add(line.npcId))
            {
                AppLog.Error($"[QuestLineRegistrar] Duplicate npc id '{line.npcId}' detected across quest lines.", this);
                return false;
            }

            if (line.quests == null || line.quests.Count == 0)
            {
                AppLog.Error($"[QuestLineRegistrar] Quest line '{line.lineId}' has no quest definitions.", this);
                return false;
            }

            foreach (QuestDefinitionSO definition in line.quests)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.id))
                {
                    AppLog.Error($"[QuestLineRegistrar] Quest line '{line.lineId}' contains an invalid quest definition.", this);
                    return false;
                }

                if (!seenQuestIds.Add(definition.id))
                {
                    AppLog.Error($"[QuestLineRegistrar] Duplicate quest id '{definition.id}' detected across quest lines.", this);
                    return false;
                }
            }
        }

        for (int i = 0; i < lines.Count; i++)
        {
            QuestLineSO line = lines[i];
            if (string.IsNullOrWhiteSpace(line.prerequisiteLineId))
                continue;

            if (!seenLineIds.Contains(line.prerequisiteLineId))
            {
                AppLog.Error(
                    $"[QuestLineRegistrar] Quest line '{line.lineId}' references missing prerequisite line '{line.prerequisiteLineId}'.",
                    this);
                return false;
            }
        }

        return true;
    }

    private void BindNpcQuestGivers(IReadOnlyList<QuestLineSO> lines)
    {
        if (lines == null || lines.Count == 0)
            return;

        var lineByNpcId = new Dictionary<string, QuestLineSO>();
        for (int i = 0; i < lines.Count; i++)
        {
            QuestLineSO line = lines[i];
            if (line == null || string.IsNullOrWhiteSpace(line.npcId))
                continue;

            lineByNpcId[line.npcId] = line;
        }

        NpcQuestGiver[] givers = FindObjectsByType<NpcQuestGiver>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < givers.Length; i++)
        {
            NpcQuestGiver giver = givers[i];
            if (giver == null)
                continue;

            string npcId = giver.NpcId;
            if (string.IsNullOrWhiteSpace(npcId))
                continue;

            if (!lineByNpcId.TryGetValue(npcId, out QuestLineSO line) || line == null)
                continue;

            giver.AssignQuestLine(line);
        }
    }

    private QuestCatalogSO ResolveQuestCatalog()
    {
        if (registry != null && registry.questCatalog != null)
            return registry.questCatalog;
        return questCatalog;
    }

    private QuestWorldCatalogSetSO ResolveWorldCatalogSet(IReadOnlyList<QuestLineSO> lines)
    {
        if (registry != null && registry.worldCatalogSet != null)
            return registry.worldCatalogSet;

        if (worldCatalogSet != null)
            return worldCatalogSet;

        if (lines != null)
        {
            foreach (QuestLineSO line in lines)
            {
                if (line != null && line.worldCatalogSet != null)
                    return line.worldCatalogSet;
            }
        }

        return null;
    }

    private void RegisterQuestLines(IQuestService questService, IReadOnlyList<QuestLineSO> lines)
    {
        GameObject shellPrefab = ResolveQuestRuntimeShellPrefab();
        if (shellPrefab == null)
        {
            AppLog.Error("[QuestLineRegistrar] Quest runtime shell prefab is not assigned.", this);
            return;
        }

        IReadOnlyDictionary<string, string> lineCompletionQuestIds = BuildLineCompletionQuestIds(lines);

        foreach (QuestLineSO line in lines)
            RegisterQuestLine(questService, line, shellPrefab, lineCompletionQuestIds);
    }

    private static IReadOnlyDictionary<string, string> BuildLineCompletionQuestIds(IReadOnlyList<QuestLineSO> lines)
    {
        var result = new Dictionary<string, string>();
        if (lines == null)
            return result;

        foreach (QuestLineSO line in lines)
        {
            if (line == null || string.IsNullOrEmpty(line.lineId) || line.quests == null)
                continue;

            for (int i = line.quests.Count - 1; i >= 0; i--)
            {
                QuestDefinitionSO definition = line.quests[i];
                if (definition == null || string.IsNullOrEmpty(definition.id))
                    continue;

                result[line.lineId] = definition.id;
                break;
            }
        }

        return result;
    }

    private void RegisterQuestLine(
        IQuestService questService,
        QuestLineSO line,
        GameObject shellPrefab,
        IReadOnlyDictionary<string, string> lineCompletionQuestIds)
    {
        if (line.quests == null)
            return;

        string prerequisiteQuestId = null;
        if (!string.IsNullOrEmpty(line.prerequisiteLineId))
        {
            if (lineCompletionQuestIds != null &&
                lineCompletionQuestIds.TryGetValue(line.prerequisiteLineId, out prerequisiteQuestId))
            {
                // Found the quest that completes the previous NPC line.
            }
            else
            {
                AppLog.Warning(
                    $"[QuestLineRegistrar] Quest line '{line.lineId}' references prerequisite line '{line.prerequisiteLineId}', but no completion quest was found.",
                    this);
            }
        }

        bool isFirstQuestInLine = true;
        foreach (QuestDefinitionSO definition in line.quests)
        {
            if (definition == null || string.IsNullOrEmpty(definition.id))
            {
                AppLog.Warning(
                    $"[QuestLineRegistrar] Skipping invalid quest definition in line '{line.lineId}'.",
                    this);
                continue;
            }

            Transform parent = questParent != null ? questParent : transform;
            GameObject instance = Instantiate(shellPrefab, parent);
            instance.name = $"Quest_{definition.id}";

            QuestInfo questInfo = instance.GetComponent<QuestInfo>();
            if (questInfo == null)
                questInfo = instance.AddComponent<QuestInfo>();

            questInfo.ApplyAuthoringMetadata(
                definition.id,
                definition.displayName,
                definition.description,
                definition.levelRequired,
                definition.waitForNpcTurnIn,
                definition.prerequisiteQuestId);

            if (isFirstQuestInLine && !string.IsNullOrEmpty(prerequisiteQuestId))
                questInfo.AddRequiredQuestId(prerequisiteQuestId);

            QuestDefinitionLink link = instance.GetComponent<QuestDefinitionLink>();
            if (link == null)
                link = instance.AddComponent<QuestDefinitionLink>();
            link.SetDefinition(definition);

            questService.RegisterQuest(questInfo);
            _definitionByQuest[questInfo] = definition;
            isFirstQuestInLine = false;
        }
    }

    private void RegisterAvailabilityService()
    {
        _availabilityService = GetComponent<QuestAvailabilityService>();
        if (_availabilityService == null)
            _availabilityService = gameObject.AddComponent<QuestAvailabilityService>();

        _availabilityService.Initialize(ResolveQuestCatalog(), _definitionByQuest);
        ServiceLocator.For(this).Register<IQuestAvailabilityService>(_availabilityService);
    }

    private void RegisterObjectiveRuntime(IQuestService questService, IReadOnlyList<QuestLineSO> lines)
    {
        QuestWorldCatalogSetSO resolvedCatalogSet = ResolveWorldCatalogSet(lines);
        if (resolvedCatalogSet == null)
        {
            AppLog.Warning(
                "[QuestLineRegistrar] World catalog set is not assigned. Objective id validation disabled.",
                this);
            return;
        }

        QuestCatalogSO catalog = ResolveQuestCatalog();
        if (resolvedCatalogSet.questCatalog == null && catalog != null)
            resolvedCatalogSet.questCatalog = catalog;

        var resolver = new QuestWorldResolver(resolvedCatalogSet);
        ServiceLocator.For(this).Register<IQuestWorldResolver>(resolver);

        if (questService is QuestManager questManager)
            questManager.ConfigureObjectiveRuntime(resolver);

        if (FindFirstObjectByType<QuestObjectiveEventBus>(FindObjectsInactive.Include) == null &&
            GetComponent<QuestObjectiveEventBus>() == null)
            gameObject.AddComponent<QuestObjectiveEventBus>();
    }
}

