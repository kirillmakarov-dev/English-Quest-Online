using Fusion;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Listens to a specific QuestStep and uses Photon Fusion RPCs to ensure 
/// the actions and UnityEvents are invoked for all players in Shared Mode.
/// </summary>
[AddComponentMenu(QuestSystemComponentMenuPaths.ActionListeners + "/Network Step Action Listener")]
public class NetworkQuestStepActionListener : NetworkBehaviour
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
            AppLog.Warning($"[NetworkQuestStepActionListener] No target step found on {gameObject.name}");
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
        // Check if we are connected to Fusion and have state authority 
        // OR if you want any client to trigger it, we let the RPC handle the broadcast.
        // It's usually best to ensure the object is valid on the network before sending RPCs.
        if (Object != null && Object.IsValid)
        {
            RPC_NotifyStepFinished();
        }
        else
        {
            // Fallback for offline mode testing
            ExecuteActions();
        }
    }

    /// <summary>
    /// RpcSources.All: Any client (in Shared Mode) can call this.
    /// RpcTargets.All: Sends the execution to all connected clients.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_NotifyStepFinished()
    {
        ExecuteActions();
    }

    private void ExecuteActions()
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