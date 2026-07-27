using System.Collections.Generic;
using EnglishQuest.RewardSystem;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Awards the player coins each time any monitored step finishes.
/// Drop on the same GameObject as a QuestStep, or assign steps manually.
/// </summary>
[AddComponentMenu(QuestSystemComponentMenuPaths.StepFinishActions + "/Give Gold Reward")]
public class QuestStepFinishAction_GiveGoldReward : MonoBehaviour
{
    [Tooltip("Steps to monitor. If empty, uses the QuestStep on this GameObject.")]
    [SerializeField] private List<QuestStep> _targetSteps = new();

    [SerializeField, Min(0)] private int _goldReward = 10;

    private void Awake()
    {
        if (_targetSteps.Count == 0)
        {
            var step = GetComponent<QuestStep>();
            if (step != null)
                _targetSteps.Add(step);
        }
    }

    private void OnEnable()
    {
        foreach (var step in _targetSteps)
            step.OnStepFinished += HandleStepFinished;
    }

    private void OnDisable()
    {
        foreach (var step in _targetSteps)
            step.OnStepFinished -= HandleStepFinished;
    }

    private void HandleStepFinished(QuestStep step, string _)
    {
        if (!ServiceLocator.For(this).TryGet<IRewardService>(out var rewardService))
        {
            AppLog.Warning("[QuestStepFinishAction_GiveGoldReward] IRewardService not found via ServiceLocator!");
            return;
        }

        rewardService.GrantLegacyCoins(_goldReward);
        AppLog.Info($"[QuestStepFinishAction_GiveGoldReward] Rewarded {_goldReward} coins for step '{step.gameObject.name}'.");
    }
}

