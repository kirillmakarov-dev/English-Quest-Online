using UnityEngine;

/// <summary>
/// Spawns the active quest journal canvas prefab when none exists in the loaded scenes.
/// </summary>
[AddComponentMenu(QuestSystemComponentMenuPaths.UI + "/Active Quest Journal Bootstrap")]
public class ActiveQuestJournalBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject activeQuestJournalCanvasPrefab;

    void Awake()
    {
        if (FindFirstObjectByType<ActiveQuestJournalUI>(FindObjectsInactive.Include) != null)
            return;

        if (activeQuestJournalCanvasPrefab == null)
        {
            AppLog.Warning("[ActiveQuestJournalBootstrap] Active quest journal canvas prefab is not assigned.", this);
            return;
        }

        GameObject instance = Instantiate(activeQuestJournalCanvasPrefab, transform);
        instance.name = activeQuestJournalCanvasPrefab.name;
    }
}
