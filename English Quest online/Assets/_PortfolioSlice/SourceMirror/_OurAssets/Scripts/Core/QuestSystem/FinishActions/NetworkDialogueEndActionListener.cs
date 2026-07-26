using Fusion;
using UnityEngine;
using UnityEngine.Events;
using UnityServiceLocator;

/// <summary>
/// Basic script flow: 
/// Quest step finished -> talking client waits for dialogue end -> sends RPC -> all clients run ActionSequences once
/// </summary>
[AddComponentMenu(QuestSystemComponentMenuPaths.ActionListeners + "/Network Dialogue End Action Listener")]
public class NetworkDialogueEndActionListener : NetworkBehaviour
{
    [Header("Target Step")]
    [Tooltip("The step to monitor. If empty, looks for one on this object.")]
    [SerializeField] private QuestStep _targetStep;

    [Header("Actions")]
    [Tooltip("Action to run for everyone after the local dialogue ends.")]
    [SerializeField] private GameAction _actionToRun;

    [Tooltip("UnityEvent to invoke for everyone after the local dialogue ends.")]
    [SerializeField] private UnityEvent _onDialogueEnded;

    private bool _waitingForDialogueEnd;
    private bool _rpcAlreadySent;
    private bool _actionAlreadyRun;
    private IDialogueService _dialogue;
    private bool _dialogueSubscribed;

    private void Awake()
    {
        if (_targetStep == null)
            _targetStep = GetComponent<QuestStep>();
    }

    private void OnEnable()
    {
        if (_targetStep != null)
            _targetStep.OnStepFinished += HandleStepFinished;
        else
            AppLog.Warning($"[NetworkDialogueEndActionListener] No target step found on {gameObject.name}");
    }

    private void OnDisable()
    {
        if (_targetStep != null)
            _targetStep.OnStepFinished -= HandleStepFinished;

        UnsubscribeFromDialogueEnd();
        _waitingForDialogueEnd = false;
    }

    private void HandleStepFinished(QuestStep step, string finalState)
    {
        if (_rpcAlreadySent || _actionAlreadyRun)
            return;

        if (!ServiceLocator.For(this).TryGet(out _dialogue) || !UnityLifetime.IsAlive(_dialogue))
        {
            TriggerForEveryone();
            return;
        }

        if (!_dialogue.IsDialogueActive)
            return;

        _waitingForDialogueEnd = true;
        SubscribeToDialogueEnd();
    }

    private void HandleDialogueEnd()
    {
        if (!_waitingForDialogueEnd)
            return;

        UnsubscribeFromDialogueEnd();
        _waitingForDialogueEnd = false;
        TriggerForEveryone();
    }

    private void SubscribeToDialogueEnd()
    {
        if (_dialogueSubscribed || !UnityLifetime.IsAlive(_dialogue))
            return;

        _dialogue.OnDialogueEnd += HandleDialogueEnd;
        _dialogueSubscribed = true;
    }

    private void UnsubscribeFromDialogueEnd()
    {
        if (!_dialogueSubscribed) return;

        if (UnityLifetime.IsAlive(_dialogue))
            _dialogue.OnDialogueEnd -= HandleDialogueEnd;

        _dialogue = null;
        _dialogueSubscribed = false;
    }

    private void TriggerForEveryone()
    {
        if (_rpcAlreadySent)
            return;

        _rpcAlreadySent = true;

        if (Object != null && Object.IsValid)
        {
            RPC_RunAction();
            RunActionOnce();
        }
        else
        {
            RunActionOnce();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_RunAction() => RunActionOnce();

    private void RunAction()
    {
        if (_actionToRun != null)
            StartCoroutine(_actionToRun.Execute());

        _onDialogueEnded?.Invoke();
    }

    private void RunActionOnce()
    {
        if (_actionAlreadyRun)
            return;

        _actionAlreadyRun = true;
        RunAction();
    }
}
