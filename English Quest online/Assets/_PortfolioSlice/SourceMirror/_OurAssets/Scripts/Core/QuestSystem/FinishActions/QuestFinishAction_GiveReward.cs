using EnglishQuest.RewardSystem;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// When the monitored quest finishes, grants every reward in a <see cref="RewardDefinition"/>
/// via <see cref="IRewardService"/>.
/// </summary>
[AddComponentMenu(QuestSystemComponentMenuPaths.FinishActions + "/Give Reward")]
public class QuestFinishAction_GiveReward : QuestFinishAction
{
    [SerializeField] private RewardDefinition rewardDefinition;
    [SerializeField] private bool showPopup = true;

    protected override void OnQuestFinished()
    {
        if (questToMonitor == null || rewardDefinition == null)
            return;

        if (!ServiceLocator.For(this).TryGet<IRewardService>(out var rewardService))
        {
            AppLog.Warning("[QuestFinishAction_GiveReward] IRewardService not found via ServiceLocator!");
            return;
        }

        rewardService.Grant(rewardDefinition.ToBundle(), showPopup);
        AppLog.Info($"[QuestFinishAction_GiveReward] Granted '{rewardDefinition.name}' for quest '{questToMonitor.id}'.");
    }
}

