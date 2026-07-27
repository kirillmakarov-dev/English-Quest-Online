using System.Collections.Generic;
using EnglishQuest.RewardSystem;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Grants a <see cref="RewardDefinition"/> each time any monitored step finishes.
/// Drop on the same GameObject as a QuestStep, or assign steps manually.
/// </summary>
[AddComponentMenu(QuestSystemComponentMenuPaths.StepFinishActions + "/Give Reward")]
public class QuestStepFinishAction_GiveReward : MonoBehaviour
{
    [Tooltip("Steps to monitor. If empty, uses the QuestStep on this GameObject.")]
    [SerializeField] private List<QuestStep> targetSteps = new();

    [SerializeField] private RewardDefinition rewardDefinition;
    [SerializeField] private bool showPopup = true;

    private void OnEnable()
    {
        foreach (QuestStep step in targetSteps)
        {
            if (step == null) continue;
            step.OnStepFinished += HandleStepFinished;
        }
    }

    private void OnDisable()
    {
        foreach (QuestStep step in targetSteps)
        {
            if (step == null) continue;
            step.OnStepFinished -= HandleStepFinished;
        }
    }

    private void HandleStepFinished(QuestStep step, string _)
    {
        if (rewardDefinition == null)
            return;

        if (!ServiceLocator.For(this).TryGet<IRewardService>(out var rewardService))
        {
            AppLog.Warning("[QuestStepFinishAction_GiveReward] IRewardService not found via ServiceLocator!");
            return;
        }

        rewardService.Grant(rewardDefinition.ToBundle(), showPopup);
        AppLog.Info($"[QuestStepFinishAction_GiveReward] Granted '{rewardDefinition.name}' for step '{step.gameObject.name}'.");
    }
}

