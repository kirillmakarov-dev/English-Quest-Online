using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu(QuestSystemComponentMenuPaths.ActionListeners + "/Multi Step Action Listener")]
public class QuestMultiStepActionListener : MonoBehaviour
{
    [Header("Steps to Monitor")]
    [Tooltip("List of QuestSteps to monitor. Actions will run when each one finishes.")]
    [SerializeField] private QuestStep[] stepsToMonitor;

    [Header("Actions")]
    [Tooltip("Action to run each time any monitored step finishes.")]
    [SerializeField] private GameAction actionToRun;

    [Header("Events")]
    [Tooltip("Unity Event to invoke each time any monitored step finishes.")]
    [SerializeField] private UnityEvent _OnstepFinished;

    private void OnEnable()
    {
        if (stepsToMonitor == null)
        {
            AppLog.Warning($"[QuestMultiStepActionListener] No steps assigned to monitor on {gameObject.name}");
            return;
        }

        foreach (var step in stepsToMonitor)
        {
            if (step != null)
                step.OnStepFinished += OnStepFinishedHandler;
            else
                AppLog.Warning($"[QuestMultiStepActionListener] Null step in stepsToMonitor on {gameObject.name}");
        }
    }

    private void OnDisable()
    {
        if (stepsToMonitor == null) return;

        foreach (var step in stepsToMonitor)
        {
            if (step != null)
                step.OnStepFinished -= OnStepFinishedHandler;
        }
    }

    private void OnStepFinishedHandler(QuestStep finishedStep, string _)
    {
        if (actionToRun != null)
            StartCoroutine(actionToRun.Execute());

        _OnstepFinished?.Invoke();
    }
}
