#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QuestLineSO))]
public class QuestLineSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var line = (QuestLineSO)target;
        EditorGUILayout.Space();

        if (GUILayout.Button("Apply Prerequisite Chain"))
            ApplyPrerequisiteChain(line);

        if (GUILayout.Button("Validate Line"))
            ValidateLine(line);
    }

    public static void ApplyPrerequisiteChain(QuestLineSO line)
    {
        if (line == null || line.quests == null)
            return;

        string previousId = null;
        for (int i = 0; i < line.quests.Count; i++)
        {
            QuestDefinitionSO definition = line.quests[i];
            if (definition == null)
                continue;

            definition.prerequisiteQuestId = previousId;
            definition.giverNpcId = line.npcId;
            EditorUtility.SetDirty(definition);
            previousId = definition.id;
        }

        EditorUtility.SetDirty(line);
        AssetDatabase.SaveAssets();
        Debug.Log($"[QuestLineSO] Applied prerequisite chain to '{line.name}'.");
    }

    public static void ValidateLine(QuestLineSO line)
    {
        if (line == null)
            return;

        int issues = 0;
        var seenIds = new System.Collections.Generic.HashSet<string>();

        foreach (QuestDefinitionSO definition in line.quests)
        {
            if (definition == null)
            {
                Debug.LogWarning($"[QuestLineSO] '{line.name}' has a null quest entry.");
                issues++;
                continue;
            }

            if (string.IsNullOrEmpty(definition.id))
            {
                Debug.LogWarning($"[QuestLineSO] '{line.name}' has a quest with empty id.", definition);
                issues++;
            }
            else if (!seenIds.Add(definition.id))
            {
                Debug.LogWarning($"[QuestLineSO] Duplicate quest id '{definition.id}' in line '{line.name}'.", definition);
                issues++;
            }
        }

        if (issues == 0)
            Debug.Log($"[QuestLineSO] '{line.name}' passed validation.");
        else
            Debug.LogWarning($"[QuestLineSO] '{line.name}' has {issues} issue(s).");
    }
}
#endif
