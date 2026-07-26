using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A generic QuestFinishAction that can run a specific GameAction or a UnityEvent.
/// This allows you to hook up any modular action (or sequence) to a quest completion,
/// or trigger arbitrary Unity logic via the inspector.
/// </summary>
[AddComponentMenu(QuestSystemComponentMenuPaths.FinishActions + "/Run Action")]
public class QuestFinishAction_RunAction : QuestFinishAction
{
    [Header("Modular Action")]
    [Tooltip("Assign a GameAction (e.g., Action_Sequence) to run when the quest finishes.")]
    [SerializeField] private GameAction _actionToRun;

    [Header("Unity Event")]
    [Tooltip("Invoke any public methods here when the quest finishes.")]
    [SerializeField] private UnityEvent _onQuestFinishedEvent;

    protected override void OnQuestFinished()
    {
        AppLog.Info($"[QuestFinishAction_RunAction] Quest finished. Executing assigned actions.");

        // 1. Run the GameAction (supports coroutines/waiting)
        if (_actionToRun != null)
        {
            StartCoroutine(_actionToRun.Execute());
        }

        // 2. Invoke the UnityEvent (instant)
        if (_onQuestFinishedEvent != null)
        {
            _onQuestFinishedEvent.Invoke();
        }
    }
}
