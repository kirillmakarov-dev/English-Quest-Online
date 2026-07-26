using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A concrete implementation of QuestFinishAction that toggles game objects.
/// </summary>
[AddComponentMenu(QuestSystemComponentMenuPaths.FinishActions + "/Toggle Objects")]
public class QuestFinishAction_ToggleObjects : QuestFinishAction
{
    [Header("Object Toggling")]
    [Tooltip("List of objects to set active (true) when the quest finishes.")]
    [SerializeField] private List<GameObject> objectsToActivate;

    [Tooltip("List of objects to set inactive (false) when the quest finishes.")]
    [SerializeField] private List<GameObject> objectsToDeactivate;

    protected override void OnQuestFinished()
    {
        string questId = questToMonitor != null ? questToMonitor.id : "Unknown";
        AppLog.Info($"[QuestFinishAction] Toggling objects for quest: {questId}");

        // Activate objects
        if (objectsToActivate != null)
        {
            foreach (GameObject obj in objectsToActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
        }

        // Deactivate objects
        if (objectsToDeactivate != null)
        {
            foreach (GameObject obj in objectsToDeactivate)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }
    }
}
