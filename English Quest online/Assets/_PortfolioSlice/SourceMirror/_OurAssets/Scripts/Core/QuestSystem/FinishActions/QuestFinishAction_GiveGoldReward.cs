using EnglishQuest.RewardSystem;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// When the monitored quest finishes, awards the player with the quest's gold reward
/// via <see cref="IRewardService.GrantLegacyCoins"/>.
/// </summary>
[AddComponentMenu(QuestSystemComponentMenuPaths.FinishActions + "/Give Gold Reward")]
public class QuestFinishAction_GiveGoldReward : QuestFinishAction
{
    [SerializeField] private int goldReward = 10;

    protected override void OnQuestFinished()
    {
        if (questToMonitor == null) return;

        if (!ServiceLocator.For(this).TryGet<IRewardService>(out var rewardService))
        {
            AppLog.Warning("[QuestFinishAction_GiveGoldReward] IRewardService not found via ServiceLocator!");
            return;
        }

        rewardService.GrantLegacyCoins(goldReward);
        AppLog.Info($"[QuestFinishAction_GiveGoldReward] Rewarded {goldReward} coins for quest '{questToMonitor.id}'.");
    }
}

