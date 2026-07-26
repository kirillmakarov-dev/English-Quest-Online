using System.Collections;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Waits for the dialogue system to report that dialogue has ended.
/// </summary>
public class Action_WaitForDialogueEnd : GameAction
{
    private bool _dialogueEnded;
    private IDialogueService _dialogue;
    private bool _waitingForEnd;

    public override IEnumerator Execute()
    {
        if (!ServiceLocator.For(this).TryGet(out _dialogue) || !UnityLifetime.IsAlive(_dialogue))
        {
            AppLog.Warning("[Action_WaitForDialogueEnd] IDialogueService not found. Continuing immediately.");
            yield break;
        }

        if (!_dialogue.IsDialogueActive)
        {
            AppLog.Info("[Action_WaitForDialogueEnd] Dialogue not active. Continuing immediately.");
            yield break;
        }

        AppLog.Info("[Action_WaitForDialogueEnd] Waiting for Dialogue End event...");
        _dialogueEnded = false;
        _waitingForEnd = true;
        _dialogue.OnDialogueEnd += OnDialogueEnd;

        while (!_dialogueEnded)
            yield return null;

        UnsubscribeFromDialogueEnd();
        AppLog.Info("[Action_WaitForDialogueEnd] Dialogue Ended.");
    }

    private void OnDialogueEnd() => _dialogueEnded = true;

    private void OnDisable() => UnsubscribeFromDialogueEnd();

    private void UnsubscribeFromDialogueEnd()
    {
        if (!_waitingForEnd) return;

        if (UnityLifetime.IsAlive(_dialogue))
            _dialogue.OnDialogueEnd -= OnDialogueEnd;

        _waitingForEnd = false;
        _dialogue = null;
    }
}
