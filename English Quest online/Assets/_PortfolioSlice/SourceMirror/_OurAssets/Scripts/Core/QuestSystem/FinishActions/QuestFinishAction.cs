using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Abstract base class for actions that should occur when a specific quest is finished.
/// </summary>
public abstract class QuestFinishAction : MonoBehaviour
{
    [Header("Quest Settings")]
    [Tooltip("The quest that triggers this action when finished.")]
    [SerializeField] protected QuestInfo questToMonitor;

    protected IQuestService _questService;

    [Tooltip("If true, the action will execute immediately on Start if the quest is already finished.")]
    [SerializeField] protected bool triggerOnStartIfFinished = true;

    protected virtual void Start()
    {
        if (!ServiceLocator.For(this).TryGet<IQuestService>(out _questService))
        {
            AppLog.Error("IQuestService not found via ServiceLocator!");
            return;
        }

        // Subscribe to the completion event
        _questService.OnQuestCompleted += HandleQuestCompleted;

        // Check if already finished
        if (triggerOnStartIfFinished && questToMonitor != null)
        {
            // We check the runtime state directly from the QuestInfo
            if (questToMonitor.state == QuestState.FINISHED)
            {
                OnQuestFinished();
            }
        }
    }

    protected virtual void OnDestroy()
    {
        if (_questService != null)
            _questService.OnQuestCompleted -= HandleQuestCompleted;
    }

    private void HandleQuestCompleted(QuestInfo quest)
    {
        // Only trigger if the completed quest matches our monitored quest
        // We compare IDs to be safe
        if (questToMonitor != null && quest != null && quest.id == questToMonitor.id)
        {
            OnQuestFinished();
        }
    }

    /// <summary>
    /// Logic to execute when the quest finishes.
    /// </summary>
    protected abstract void OnQuestFinished();
}
