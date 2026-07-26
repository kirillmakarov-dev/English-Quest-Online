using System;
using System.Collections.Generic;
using EnglishKingdom.QuestSystem;
using UnityEngine;

[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.CoreQuestLineBuildSpec, fileName = "QuestLineBuildSpec_")]
public class QuestLineBuildSpecSO : ScriptableObject
{
    [Header("Identity")]
    public string lineId;
    public string npcId;
    [Tooltip("Optional quest line that must be completed before this NPC becomes available.")]
    public string prerequisiteLineId;
    public string displayName;
    [TextArea] public string theme;

    [Header("Authoring")]
    [Tooltip("Optional profile for folder and catalog paths. When unset, paths derive from this asset's folder.")]
    public QuestLineAuthoringProfileSO profile;

    [Header("Quests")]
    public List<QuestBuildEntry> quests = new();
}

[Serializable]
public class QuestBuildEntry
{
    public string id;
    public string displayName;
    public int levelRequired = 1;
    public int xpReward = 50;

    public List<QuestObjectiveDefinition> objectives = new();

    [Header("Dialogue")]
    public DialogueNode startDialogue;
    public DialogueNode inProgressDialogue;
    public DialogueNode turnInDialogue;
    public DialogueNode cannotStartDialogue;
    public DialogueNode alreadyFinishedDialogue;
}
