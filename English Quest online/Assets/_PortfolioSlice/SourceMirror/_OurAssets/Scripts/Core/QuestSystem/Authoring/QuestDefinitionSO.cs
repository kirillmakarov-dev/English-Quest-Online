using System.Collections.Generic;
using EnglishKingdom.QuestSystem;
using UnityEngine;

[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.CoreQuestDefinition, fileName = "QuestDefinition_")]
public class QuestDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    [TextArea] public string description;

    [Header("Requirements")]
    public int levelRequired;
    public string giverNpcId;
    [Tooltip("Optional SO reference for authoring. Syncs prerequisiteQuestId in the editor.")]
    public QuestDefinitionSO prerequisiteQuest;
    public string prerequisiteQuestId;

    [Header("Runtime")]
    public bool waitForNpcTurnIn = true;

    [Header("Rewards")]
    public QuestRewardData rewards = new();
    public EnglishKingdom.RewardSystem.RewardDefinition rewardDefinition;

    [Header("Dialogue")]
    public DialogueNode startDialogue;
    public DialogueNode inProgressDialogue;
    public DialogueNode turnInDialogue;
    public DialogueNode cannotStartDialogue;
    public DialogueNode alreadyFinishedDialogue;

    [Header("Objectives")]
    public List<QuestObjectiveDefinition> objectives = new();

    [Header("Editor")]
    [Tooltip("Optional catalog set used by quest authoring inspectors for ID dropdowns. Not used at runtime.")]
    public QuestWorldCatalogSetSO authoringCatalogSet;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (prerequisiteQuest != null && !string.IsNullOrEmpty(prerequisiteQuest.id))
            prerequisiteQuestId = prerequisiteQuest.id;
    }
#endif
}
