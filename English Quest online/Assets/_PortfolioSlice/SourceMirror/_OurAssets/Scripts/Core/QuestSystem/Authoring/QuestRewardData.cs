using System;
using System.Collections.Generic;
using EnglishQuest.RewardSystem;
using UnityEngine;

[Serializable]
public class QuestRewardItemData
{
    public string itemId;
    public int amount = 1;
}

[Serializable]
public class QuestRewardData
{
    public int xp;
    public List<QuestRewardItemData> items = new();

    public RewardBundle ToRewardBundle()
    {
        var bundle = new RewardBundle();
        if (xp > 0)
            bundle.AddXp(xp);
        return bundle;
    }
}

