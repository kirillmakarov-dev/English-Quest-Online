using System;
using UnityEngine;
using UnityServiceLocator;

public abstract class QuestStep : MonoBehaviour
{
    [TextArea] public string stepDescription;

    protected bool isFinished = false;
    public bool IsFinished => isFinished;

    protected bool stepIsActive = false;
    public bool StepIsActive => stepIsActive;

    // Carries the completing step and its final state string to listeners (manager, group, etc.)
    public event Action<QuestStep, string> OnStepFinished;

    // Called by the parent quest (or group) when this step becomes active
    public void InitializeQuestStep(QuestInfo questInfo, int stepIndex, string questStepState, QuestStepStatus stepStatus = QuestStepStatus.NOT_STARTED)
    {
        if (stepStatus == QuestStepStatus.COMPLETED)
        {
            isFinished = true;
            stepIsActive = false;
        }
        else
        {
            stepIsActive = true;
        }

        if (!string.IsNullOrEmpty(questStepState))
            SetQuestStepState(questStepState);

        // Give the manager context so it can react when this step fires OnStepFinished.
        // Sub-steps pass null questInfo and are intentionally skipped here.
        if (questInfo != null && ServiceLocator.For(this).TryGet<IQuestService>(out var qs))
            qs.RegisterQuestStep(this, questInfo, stepIndex);

        InitializeStep();
    }

    public virtual void InitializeStep()
    {
        // Override to set up listeners, enable triggers, show UI, etc.
    }

    // Allows external systems (cutscenes, cheat tools, network sync) to finish
    // this step without knowing its internal implementation.
    public void CompleteExternally() => FinishStep("");

    protected void FinishStep()
    {
        stepIsActive = false;
        FinishStep("");
    }

    protected void FinishStep(string finalState)
    {
        if (isFinished) return;
        isFinished = true;
        OnStepFinished?.Invoke(this, finalState);
    }

    protected virtual void SetQuestStepState(string state)
    {
        // Base implementation does nothing
    }

    public void ResetStep()
    {
        isFinished = false;
        stepIsActive = false;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void Debug_TriggerFinish()
    {
        FinishStep("DEBUG_COMPLETE");
    }
#endif
}
