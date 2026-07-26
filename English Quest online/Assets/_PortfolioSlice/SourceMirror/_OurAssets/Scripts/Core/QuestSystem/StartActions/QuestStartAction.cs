using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Abstract base class for actions that should occur when a specific quest is started.
/// Mirrors the QuestFinishAction pattern but hooks into IQuestService.OnQuestStarted.
/// </summary>
public abstract class QuestStartAction : MonoBehaviour
{
    [Header("Quest Settings")]
    [Tooltip("The quest that triggers this action when started.")]
    [SerializeField] protected QuestInfo questToMonitor;

    protected IQuestService _questService;

    [Tooltip("If true, the action will execute immediately on Start if the quest is already in progress.")]
    [SerializeField] protected bool triggerOnStartIfInProgress = true;

    protected virtual void Start()
    {
        if (!ServiceLocator.For(this).TryGet<IQuestService>(out _questService))
        {
            AppLog.Error("[QuestStartAction] IQuestService not found via ServiceLocator!");
            return;
        }

        _questService.OnQuestStarted += HandleQuestStarted;

        // Fire immediately if the quest is already running (e.g. after a scene reload)
        if (triggerOnStartIfInProgress && questToMonitor != null)
        {
            if (questToMonitor.state == QuestState.IN_PROGRESS)
            {
                OnQuestStarted();
            }
        }
    }

    protected virtual void OnDestroy()
    {
        if (_questService != null)
            _questService.OnQuestStarted -= HandleQuestStarted;
    }

    private void HandleQuestStarted(QuestInfo quest)
    {
        if (questToMonitor != null && quest != null && quest.id == questToMonitor.id)
        {
            OnQuestStarted();
        }
    }

    /// <summary>
    /// Logic to execute when the quest starts.
    /// </summary>
    protected abstract void OnQuestStarted();
}
