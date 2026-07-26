using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Listens to a specific QuestStep on this GameObject (or referenced) 
/// and runs actions when that step finishes.
/// This works for both main quest steps and sub-steps (like chests).
/// </summary>
[AddComponentMenu(QuestSystemComponentMenuPaths.ActionListeners + "/Step Action Listener")]
public class QuestStepActionListener : MonoBehaviour
{
    [Header("Target Step")]
    [Tooltip("The step to monitor. If empty, looks for one on this object.")]
    [SerializeField] private QuestStep _targetStep;

    [Header("Actions")]
    [Tooltip("Action to run when the step finishes.")]
    [SerializeField] private GameAction _actionToRun;

    [Tooltip("Unity Event to invoke when the step finishes.")]
    [SerializeField] private UnityEvent _onStepFinished;

    private void Awake()
    {
        if (_targetStep == null)
        {
            _targetStep = GetComponent<QuestStep>();
        }
    }

    private void OnEnable()
    {
        if (_targetStep != null)
        {
            _targetStep.OnStepFinished += OnStepFinishedHandler;
        }
        else
        {
            AppLog.Warning($"[QuestStepActionListener] No target step found on {gameObject.name}");
        }
    }

    private void OnDisable()
    {
        if (_targetStep != null)
        {
            _targetStep.OnStepFinished -= OnStepFinishedHandler;
        }
    }

    private void OnStepFinishedHandler(QuestStep step, string _)
    {
        // 1. Run the GameAction
        if (_actionToRun != null)
        {
            StartCoroutine(_actionToRun.Execute());
        }

        // 2. Invoke the UnityEvent
        _onStepFinished?.Invoke();
    }
}
