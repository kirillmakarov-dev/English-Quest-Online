using System;
using System.Collections.Generic;
using EnglishQuest.QuestSystem;
using UnityEngine;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

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
    [Tooltip("Optional runtime line asset synced from this build spec. Safe authoring path: edit this spec, then apply into the runtime line.")]
    public QuestLineSO runtimeLine;
    [Tooltip("Optional registries that should include the synced runtime line.")]
    public List<QuestLineRegistrySO> registries = new();

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

        if (runtimeLine == null)
            warnings.Add($"Quest line build spec '{name}' has no runtime QuestLineSO assigned.");

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

            if (entry.runtimeDefinition == null)
                warnings.Add($"Quest build entry '{entry.id}' in '{name}' has no runtime QuestDefinitionSO assigned.");
        }
    }
}

[Serializable]
public class QuestBuildEntry
{
    public string id;
    public string displayName;
    [TextArea] public string description;
    public int levelRequired = 1;
    public int xpReward = 50;
    public bool waitForNpcTurnIn = true;

    [Header("Runtime Sync")]
    [Tooltip("Optional runtime quest definition asset synced from this entry.")]
    public QuestDefinitionSO runtimeDefinition;

    public List<QuestObjectiveDefinition> objectives = new();

    [Header("Dialogue")]
    public DialogueNode startDialogue;
    public DialogueNode inProgressDialogue;
    public DialogueNode turnInDialogue;
    public DialogueNode cannotStartDialogue;
    public DialogueNode alreadyFinishedDialogue;
}

#if UNITY_EDITOR
public static class QuestLineBuildSpecAuthoringUtility
{
    public static void AutoWireRuntimeAssets(QuestLineBuildSpecSO spec)
    {
        if (spec == null)
            return;

        string folderPath = ResolveAssetFolder(spec);
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogError("[QuestLineBuildSpec] Could not resolve asset folder for auto-wiring.", spec);
            return;
        }

        if (spec.runtimeLine == null)
        {
            string linePath = AssetDatabase.GenerateUniqueAssetPath(
                $"{folderPath}/{BuildAssetFileName("QuestLine", spec.displayName, spec.lineId, spec.npcId)}.asset");

            spec.runtimeLine = ScriptableObject.CreateInstance<QuestLineSO>();
            AssetDatabase.CreateAsset(spec.runtimeLine, linePath);
        }

        if (spec.quests != null)
        {
            for (int i = 0; i < spec.quests.Count; i++)
            {
                QuestBuildEntry entry = spec.quests[i];
                if (entry == null || entry.runtimeDefinition != null)
                    continue;

                string definitionPath = AssetDatabase.GenerateUniqueAssetPath(
                    $"{folderPath}/{BuildAssetFileName("Quest", entry.displayName, entry.id, $"Step{i + 1}")}.asset");

                entry.runtimeDefinition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
                AssetDatabase.CreateAsset(entry.runtimeDefinition, definitionPath);
            }
        }

        EditorUtility.SetDirty(spec);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[QuestLineBuildSpec] Auto-wired runtime assets for '{spec.name}'.", spec);
    }

    public static void ApplyToRuntimeAssets(QuestLineBuildSpecSO spec)
    {
        if (spec == null)
            return;

        AutoWireRuntimeAssets(spec);
        if (spec.runtimeLine == null)
        {
            Debug.LogError("[QuestLineBuildSpec] Runtime QuestLineSO is still missing after auto-wire.", spec);
            return;
        }

        Undo.RecordObject(spec.runtimeLine, "Apply Quest Line Build Spec");

        spec.runtimeLine.lineId = spec.lineId;
        spec.runtimeLine.npcId = spec.npcId;
        spec.runtimeLine.prerequisiteLineId = spec.prerequisiteLineId;
        spec.runtimeLine.displayName = spec.displayName;
        spec.runtimeLine.theme = spec.theme;

        if (spec.profile != null && spec.profile.worldCatalogSet != null)
            spec.runtimeLine.worldCatalogSet = spec.profile.worldCatalogSet;

        var runtimeDefinitions = new List<QuestDefinitionSO>();
        QuestDefinitionSO previousDefinition = null;

        if (spec.quests != null)
        {
            for (int i = 0; i < spec.quests.Count; i++)
            {
                QuestBuildEntry entry = spec.quests[i];
                if (entry == null || entry.runtimeDefinition == null)
                    continue;

                QuestDefinitionSO definition = entry.runtimeDefinition;
                Undo.RecordObject(definition, "Apply Quest Build Entry");

                definition.id = entry.id;
                definition.displayName = entry.displayName;
                definition.description = entry.description;
                definition.levelRequired = entry.levelRequired;
                definition.giverNpcId = spec.npcId;
                definition.waitForNpcTurnIn = entry.waitForNpcTurnIn;
                definition.prerequisiteQuest = previousDefinition;
                definition.prerequisiteQuestId = previousDefinition != null ? previousDefinition.id : null;
                definition.startDialogue = entry.startDialogue;
                definition.inProgressDialogue = entry.inProgressDialogue;
                definition.turnInDialogue = entry.turnInDialogue;
                definition.cannotStartDialogue = entry.cannotStartDialogue;
                definition.alreadyFinishedDialogue = entry.alreadyFinishedDialogue;
                definition.objectives = CloneObjectives(entry.objectives);

                if (spec.profile != null && spec.profile.worldCatalogSet != null)
                    definition.authoringCatalogSet = spec.profile.worldCatalogSet;

                EditorUtility.SetDirty(definition);
                runtimeDefinitions.Add(definition);
                previousDefinition = definition;
            }
        }

        spec.runtimeLine.quests = runtimeDefinitions;
        EditorUtility.SetDirty(spec.runtimeLine);

        SyncRegistries(spec);

        EditorUtility.SetDirty(spec);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[QuestLineBuildSpec] Applied build spec '{spec.name}' to runtime assets.", spec);
    }

    private static void SyncRegistries(QuestLineBuildSpecSO spec)
    {
        if (spec == null || spec.runtimeLine == null || spec.registries == null)
            return;

        for (int i = 0; i < spec.registries.Count; i++)
        {
            QuestLineRegistrySO registry = spec.registries[i];
            if (registry == null)
                continue;

            Undo.RecordObject(registry, "Sync Quest Registry");

            if (registry.questLines == null)
                registry.questLines = new List<QuestLineSO>();

            if (!registry.questLines.Contains(spec.runtimeLine))
                registry.questLines.Add(spec.runtimeLine);

            if (registry.questCatalog == null && spec.profile != null && spec.profile.questCatalog != null)
                registry.questCatalog = spec.profile.questCatalog;

            if (registry.worldCatalogSet == null && spec.profile != null && spec.profile.worldCatalogSet != null)
                registry.worldCatalogSet = spec.profile.worldCatalogSet;

            EditorUtility.SetDirty(registry);
        }
    }

    private static List<QuestObjectiveDefinition> CloneObjectives(List<QuestObjectiveDefinition> source)
    {
        var result = new List<QuestObjectiveDefinition>();
        if (source == null)
            return result;

        for (int i = 0; i < source.Count; i++)
        {
            QuestObjectiveDefinition objective = source[i];
            if (objective == null)
                continue;

            var clone = new QuestObjectiveDefinition
            {
                type = objective.type,
                targetId = objective.targetId,
                count = objective.count,
                displayText = objective.displayText,
                miniGameConfig = objective.miniGameConfig,
                requiredItemId = objective.requiredItemId,
                dialogue = objective.dialogue,
                dialogueAfterFinished = objective.dialogueAfterFinished,
                stepReward = objective.stepReward,
                showStepRewardPopup = objective.showStepRewardPopup,
                parameters = CloneParameters(objective.parameters)
            };

            result.Add(clone);
        }

        return result;
    }

    private static List<QuestObjectiveParameter> CloneParameters(List<QuestObjectiveParameter> source)
    {
        var result = new List<QuestObjectiveParameter>();
        if (source == null)
            return result;

        for (int i = 0; i < source.Count; i++)
        {
            QuestObjectiveParameter parameter = source[i];
            if (parameter == null)
                continue;

            result.Add(new QuestObjectiveParameter
            {
                key = parameter.key,
                value = parameter.value
            });
        }

        return result;
    }

    private static string ResolveAssetFolder(QuestLineBuildSpecSO spec)
    {
        string assetPath = AssetDatabase.GetAssetPath(spec);
        if (string.IsNullOrEmpty(assetPath))
            return null;

        string folderPath = Path.GetDirectoryName(assetPath);
        return folderPath?.Replace("\\", "/");
    }

    private static string BuildAssetFileName(string prefix, string preferredName, string fallbackA, string fallbackB)
    {
        string baseName = preferredName;
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = fallbackA;
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = fallbackB;
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "Unnamed";

        return $"{prefix}_{Sanitize(baseName)}";
    }

    private static string Sanitize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "Unnamed";

        char[] invalidChars = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalidChars.Length; i++)
            raw = raw.Replace(invalidChars[i], '_');

        return raw.Replace(' ', '_');
    }
}
#endif

