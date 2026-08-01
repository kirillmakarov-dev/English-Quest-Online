using System;
using System.Collections.Generic;
using EnglishQuest.QuestSystem;
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

    public void CollectValidationIssues(List<string> errors, List<string> warnings)
    {
        errors ??= new List<string>();
        warnings ??= new List<string>();

        if (string.IsNullOrWhiteSpace(lineId))
            errors.Add("QuestLineBuildSpecSO has an empty lineId.");

        if (string.IsNullOrWhiteSpace(npcId))
            errors.Add($"Quest line build spec '{name}' has an empty npcId.");

        if (quests == null || quests.Count == 0)
        {
            errors.Add($"Quest line build spec '{name}' has no quest entries.");
            return;
        }

        var seenQuestIds = new HashSet<string>();
        for (int i = 0; i < quests.Count; i++)
        {
            QuestBuildEntry entry = quests[i];
            if (entry == null)
            {
                errors.Add($"Quest line build spec '{name}' contains a null quest entry at index {i}.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.id))
            {
                errors.Add($"Quest line build spec '{name}' has a quest entry with an empty id at index {i}.");
            }
            else if (!seenQuestIds.Add(entry.id))
            {
                errors.Add($"Quest line build spec '{name}' contains duplicate quest id '{entry.id}'.");
            }

            if (entry.objectives == null || entry.objectives.Count == 0)
                errors.Add($"Quest build entry '{entry.id}' in '{name}' has no objectives.");

            if (entry.startDialogue == null)
                warnings.Add($"Quest build entry '{entry.id}' in '{name}' has no start dialogue.");
        }
    }
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

