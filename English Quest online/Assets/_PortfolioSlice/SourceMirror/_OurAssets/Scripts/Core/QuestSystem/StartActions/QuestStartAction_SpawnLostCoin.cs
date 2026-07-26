using UnityEngine;

/// <summary>
/// When the monitored quest starts, instantiates the lost coin prefab at a randomly
/// chosen spawn point from the provided list.
/// The spawned coin is automatically destroyed when the quest is completed,
/// so it never lingers in the world after the quest ends.
/// 
/// Setup:
///  1. Assign the lost coin prefab (with its light beam) to 'lostCoinPrefab'.
///  2. Add as many empty GameObjects in the scene as desired spawn locations
///     and assign them to 'spawnPoints'.
///  3. Assign the quest this component should react to in 'questToMonitor'.
/// </summary>
[AddComponentMenu(QuestSystemComponentMenuPaths.StartActions + "/Spawn Lost Coin")]
public class QuestStartAction_SpawnLostCoin : QuestStartAction
{
    [Header("Coin Spawn Settings")]
    [Tooltip("The lost coin prefab to instantiate (should include its light beam child object and a QuestStep_CollectItem component).")]
    [SerializeField] private GameObject lostCoinPrefab;

    [Tooltip("Empty GameObjects that mark valid spawn locations in the world.")]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip("Index of the QuestStep_CollectItem step inside this quest's questSteps list (zero-based).")]
    [SerializeField] private int collectStepIndex = 0;

    private GameObject _spawnedCoin;

    protected override void Start()
    {
        base.Start();

        // Also listen for quest completion so we can clean up the coin
        if (_questService != null)
            _questService.OnQuestCompleted += HandleQuestCompleted;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_questService != null)
            _questService.OnQuestCompleted -= HandleQuestCompleted;
    }

    protected override void OnQuestStarted()
    {
        if (lostCoinPrefab == null)
        {
            AppLog.Warning("[QuestStartAction_SpawnLostCoin] No lostCoinPrefab assigned!");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            AppLog.Warning("[QuestStartAction_SpawnLostCoin] No spawn points assigned!");
            return;
        }

        // Guard against being triggered more than once
        if (_spawnedCoin != null) return;

        Transform chosen = spawnPoints[Random.Range(0, spawnPoints.Length)];
        _spawnedCoin = Instantiate(lostCoinPrefab, chosen.position, chosen.rotation);

        // Wire the collect-item step to this quest before its Start() fires
        QuestStep_CollectItem collectStep = _spawnedCoin.GetComponentInChildren<QuestStep_CollectItem>(true);
        if (collectStep != null)
        {
            collectStep.InitializeQuestStep(questToMonitor, collectStepIndex, string.Empty);
        }
        else
        {
            AppLog.Warning("[QuestStartAction_SpawnLostCoin] No QuestStep_CollectItem found on the spawned coin prefab!");
        }

        AppLog.Info($"[QuestStartAction_SpawnLostCoin] Spawned lost coin at '{chosen.name}' for quest '{questToMonitor?.id}'.");
    }

    private void HandleQuestCompleted(QuestInfo quest)
    {
        if (questToMonitor == null || quest == null || quest.id != questToMonitor.id) return;

        if (_spawnedCoin != null)
        {
            Destroy(_spawnedCoin);
            _spawnedCoin = null;
        }
    }
}
