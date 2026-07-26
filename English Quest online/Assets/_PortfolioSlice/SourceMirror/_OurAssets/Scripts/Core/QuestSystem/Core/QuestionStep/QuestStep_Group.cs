using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A QuestStep that finishes only when ALL assigned sub-steps are finished.
/// This allows players to complete the sub-steps (like opening 4 chests) in any order.
/// </summary>
[AddComponentMenu(QuestSystemComponentMenuPaths.Steps + "/Group")]
public class QuestStep_Group : QuestStep
{
    [Header("Parallel Steps")]
    [Tooltip("List of steps that must be finished to complete this group.")]
    [SerializeField] private List<QuestStep> _subSteps = new List<QuestStep>();
    public List<QuestStep> SubSteps => _subSteps;

    public override void InitializeStep()
    {
        base.InitializeStep();
        // Setup sub-steps when this group is initialized
        foreach (var step in _subSteps)
        {
            if (step != null)
            {
                step.OnStepFinished += OnSubStepFinished;
            }
        }

        // If the group step is active, activate the sub-steps immediately
        if (stepIsActive)
        {
            ActivateSubSteps();
        }
    }

    private void OnDestroy()
    {
        foreach (var step in _subSteps)
        {
            if (step != null)
                step.OnStepFinished -= OnSubStepFinished;
        }
    }

    /// <summary>
    /// Called by the QuestManager when this generic step becomes active.
    /// </summary>
    public void ActivateGroup()
    {
        // We override or hook into standard Activate logic if your base class has it.
        // Assuming base logic sets 'stepIsActive = true'.
        
        ActivateSubSteps();
    }

    private void ActivateSubSteps()
    {
        foreach (var step in _subSteps)
        {
            // We manually activate the sub-steps so they become interactable.
            // (Assuming your QuestStep has a public generic Setup or Activate method, 
            // or we just rely on them usually being enabled).
            
            // If QuestStep has a tailored Initialize method, call it here.
            // Otherwise, we ensure their GameObjects are active and they are ready.
            if(step != null)
            {
                // Ensure sub-steps are initialized with context if needed
                step.gameObject.SetActive(true); 
                
                // Manually "activate" the step logic so it can be interacted with
                // We pass null for QuestInfo and -1 for index because these sub-steps 
                // aren't tracked individually by the QuestManager's main list.
                step.InitializeQuestStep(null, -1, "");
            }
        }
    }
    
    private void OnSubStepFinished(QuestStep step, string _)
    {
        CheckCompletion();
    }


    private void CheckCompletion()
    {
        // Only check completion if this group step is actually active
        if (!stepIsActive || isFinished) return;

        bool allFinished = true;

        foreach (var step in _subSteps)
        {
            if (step != null && !step.IsFinished) 
            {
                allFinished = false;
                break;
            }
        }

        if (allFinished)
        {
            FinishStep();
        }
    }
}