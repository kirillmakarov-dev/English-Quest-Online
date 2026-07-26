using UnityEngine;

/// <summary>
/// Spawns the quest selection canvas prefab when none exists in the loaded scenes.
/// Place on a persistent manager (e.g. PreLoad) or next to <see cref="QuestLineRegistrar"/>.
/// </summary>
[AddComponentMenu(QuestSystemComponentMenuPaths.UI + "/Quest Selection Bootstrap")]
public class QuestSelectionBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject questSelectionCanvasPrefab;

    private void Awake()
    {
        if (FindFirstObjectByType<QuestSelectionUI>(FindObjectsInactive.Include) != null)
            return;

        if (questSelectionCanvasPrefab == null)
        {
            AppLog.Warning("[QuestSelectionBootstrap] Quest selection canvas prefab is not assigned.", this);
            return;
        }

        GameObject instance = Instantiate(questSelectionCanvasPrefab, transform);
        instance.name = questSelectionCanvasPrefab.name;
    }
}
