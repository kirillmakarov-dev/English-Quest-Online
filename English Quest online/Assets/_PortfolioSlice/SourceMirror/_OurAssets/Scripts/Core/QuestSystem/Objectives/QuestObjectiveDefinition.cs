using System;
using System.Collections.Generic;
using EnglishQuest.RewardSystem;
using UnityEngine;

namespace EnglishQuest.QuestSystem
{
    [Serializable]
    public class QuestObjectiveDefinition
    {
        public QuestObjectiveType type = QuestObjectiveType.EnterArea;

        [Tooltip("NPC id, area id, item id, mini-game id, or custom handler id depending on type.")]
        public string targetId;

        [Tooltip("Progress target (e.g. collect count). Defaults to 1.")]
        public int count = 1;

        [TextArea] public string displayText;

        [Tooltip("Optional key/value parameters (e.g. customHandlerId, minScore).")]
        public List<QuestObjectiveParameter> parameters = new();

        [Tooltip("Quest-owned mini-game content. Required for CompleteMiniGame objectives.")]
        public QuestMiniGameConfigSO miniGameConfig;

        [Tooltip("Legacy item id used by DeliverItem objectives. The portfolio slice does not ship the old inventory package.")]
        public int requiredItemId;

        [Tooltip("Dialogue played when completing a TalkToNpc or DeliverItem step.")]
        public DialogueNode dialogue;

        [Tooltip("Dialogue played when talking again after this TalkToNpc/DeliverItem step already completed.")]
        public DialogueNode dialogueAfterFinished;

        [Tooltip("Optional reward granted when this objective step completes.")]
        public RewardDefinition stepReward;

        [Tooltip("Show loot popup when step reward is granted.")]
        public bool showStepRewardPopup = true;
    }

    [Serializable]
    public class QuestObjectiveParameter
    {
        public string key;
        public string value;
    }
}

