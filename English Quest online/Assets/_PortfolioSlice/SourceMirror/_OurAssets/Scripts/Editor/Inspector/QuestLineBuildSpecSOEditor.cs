#if UNITY_EDITOR
using System.Collections.Generic;
using EnglishQuest.Editor.PortfolioDemo;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QuestLineBuildSpecSO))]
public class QuestLineBuildSpecSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var spec = (QuestLineBuildSpecSO)target;
        EditorGUILayout.Space();

        if (GUILayout.Button("Auto-Wire Runtime Assets"))
            QuestLineBuildSpecAuthoringUtility.AutoWireRuntimeAssets(spec);

        if (GUILayout.Button("Apply Build Spec To Runtime Assets"))
            QuestLineBuildSpecAuthoringUtility.ApplyToRuntimeAssets(spec);

        if (GUILayout.Button("Validate Build Spec"))
            Validate(spec);
    }

    private static void Validate(QuestLineBuildSpecSO spec)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var infos = new List<string>();

        PortfolioDemoValidationAnalyzer.ValidateQuestLineBuildSpec(spec, errors, warnings, infos);

        foreach (string info in infos)
            Debug.Log("[QuestLineBuildSpec] " + info, spec);

        foreach (string warning in warnings)
            Debug.LogWarning("[QuestLineBuildSpec] " + warning, spec);

        foreach (string error in errors)
            Debug.LogError("[QuestLineBuildSpec] " + error, spec);

        string summary =
            $"Build spec validation finished.\n\nErrors: {errors.Count}\nWarnings: {warnings.Count}\nInfo: {infos.Count}";
        EditorUtility.DisplayDialog("Quest Line Build Spec", summary, "OK");
    }
}
#endif
