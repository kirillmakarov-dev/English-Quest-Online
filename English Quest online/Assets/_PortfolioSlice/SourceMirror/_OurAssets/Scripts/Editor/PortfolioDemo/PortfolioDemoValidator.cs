using System.Collections.Generic;
using System.IO;
using EnglishQuest.QuestSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EnglishQuest.Editor.PortfolioDemo
{
    public static class PortfolioDemoValidator
    {
        private const string ScenePath = "Assets/_PortfolioSlice/Demo/Scenes/PortfolioDemo.unity";
        private const string RegistryPath = "Assets/_PortfolioSlice/Demo/Data/QuestLineRegistry_MVP.asset";
        private const string SessionProfilePath =
            "Assets/_PortfolioSlice/Demo/Data/QuestLines/Shared_MVP_Catalogs/OpenWorldNetworkSessionProfile.asset";

        [MenuItem("Tools/English Quest/Validate Portfolio Demo")]
        public static void ValidatePortfolioDemo()
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            ValidateSceneFile(errors, warnings, infos);
            ValidateQuestRegistry(errors, warnings, infos);
            ValidateQuestLineBuildSpecs(errors, warnings, infos);
            ValidateSessionProfile(errors, warnings);
            ValidateShowcaseDocs(warnings, infos);

            EmitMessages(errors, warnings, infos);

            string summary = PortfolioDemoValidationSummaryFormatter.BuildSummary(
                errors.Count,
                warnings.Count,
                infos.Count,
                warnings);

            EditorUtility.DisplayDialog("Validate Portfolio Demo", summary, "OK");
        }

        [MenuItem("Tools/English Quest/Validate Loaded Scene Runtime Bindings")]
        public static void ValidateLoadedSceneRuntimeBindings()
        {
            Scene activeScene = SceneManager.GetActiveScene();

            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateLoadedSceneRuntimeBindings(activeScene, errors, warnings, infos);
            EmitMessages(errors, warnings, infos);

            string summary = PortfolioDemoValidationSummaryFormatter.BuildSummary(
                errors.Count,
                warningCount: warnings.Count,
                infoCount: infos.Count,
                warnings);

            EditorUtility.DisplayDialog("Validate Loaded Scene Runtime Bindings", summary, "OK");
        }

        private static void ValidateSceneFile(List<string> errors, List<string> warnings, List<string> infos)
        {
            PortfolioDemoValidationAnalyzer.ValidateSceneFile(ScenePath, errors, warnings, infos);
        }

        private static void ValidateQuestRegistry(List<string> errors, List<string> warnings, List<string> infos)
        {
            QuestLineRegistrySO registry = AssetDatabase.LoadAssetAtPath<QuestLineRegistrySO>(RegistryPath);
            if (registry == null)
            {
                errors.Add($"Missing quest registry asset: {RegistryPath}");
                return;
            }

            PortfolioDemoValidationAnalyzer.ValidateQuestRegistry(registry, errors, warnings, infos);
        }

        private static void ValidateSessionProfile(List<string> errors, List<string> warnings)
        {
            NetworkSessionProfile profile = AssetDatabase.LoadAssetAtPath<NetworkSessionProfile>(SessionProfilePath);
            if (profile == null)
            {
                errors.Add($"Missing network session profile: {SessionProfilePath}");
                return;
            }

            PortfolioDemoValidationAnalyzer.ValidateSessionProfile(profile, errors, warnings);
        }

        private static void ValidateQuestLineBuildSpecs(List<string> errors, List<string> warnings, List<string> infos)
        {
            string[] guids = AssetDatabase.FindAssets("t:QuestLineBuildSpecSO");
            if (guids == null || guids.Length == 0)
            {
                infos.Add("No QuestLineBuildSpecSO assets found. Registry-driven authoring remains the active path.");
                return;
            }

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                QuestLineBuildSpecSO spec = AssetDatabase.LoadAssetAtPath<QuestLineBuildSpecSO>(assetPath);
                PortfolioDemoValidationAnalyzer.ValidateQuestLineBuildSpec(spec, errors, warnings, infos);
            }
        }

        private static void ValidateShowcaseDocs(List<string> warnings, List<string> infos)
        {
            PortfolioDemoValidationAnalyzer.ValidateShowcaseDocs(warnings, infos);
        }

        private static void EmitMessages(
            List<string> errors,
            List<string> warnings,
            List<string> infos)
        {
            foreach (string info in infos)
                Debug.Log("[PortfolioDemoValidator] " + info);

            foreach (string warning in warnings)
                Debug.LogWarning("[PortfolioDemoValidator] " + warning);

            foreach (string error in errors)
                Debug.LogError("[PortfolioDemoValidator] " + error);
        }
    }

    public static class PortfolioDemoValidationSummaryFormatter
    {
        public static string BuildSummary(
            int errorCount,
            int warningCount,
            int infoCount,
            IReadOnlyList<string> warnings = null)
        {
            string summary =
                $"Validation finished.\n\nErrors: {errorCount}\nWarnings: {warningCount}\nInfo: {infoCount}";

            if (errorCount > 0)
            {
                return summary +
                       "\n\nFix the reported errors first, then run validation again before trusting the scene for demo use.";
            }

            if (warningCount > 0)
            {
                if (ContainsSoloRuntimeRecordGap(warnings))
                {
                    return summary +
                           "\n\nRecommended next step: the solo-first coding guardrails are in place, but final proof is still missing. Open the Solo-First Proof Pack from Tools > English Quest > Docs if you want the full sign-off path in one place, run the solo-first batchmode guardrail if needed, complete SoloRuntimeSignoff.md in a real one-player session, then create a dated SoloRuntimeSignoff_Record_YYYY-MM-DD.md file before the full shared-session checklist.";
                }

                return summary +
                       "\n\nRecommended next step: clear the warnings, then open the Solo-First Proof Pack from Tools > English Quest > Docs if helpful, run the solo-first batchmode guardrail if needed, then SoloFirstVerification.md, then SoloRuntimeSignoff.md, before the full shared-session checklist.";
            }

            return summary +
                   "\n\nRecommended next step: open the Solo-First Proof Pack from Tools > English Quest > Docs if helpful, run the solo-first batchmode guardrail if needed, then SoloFirstVerification.md, then SoloRuntimeSignoff.md, then use ManualVerificationChecklist.md for the broader shared-session and presentation pass.";
        }

        private static bool ContainsSoloRuntimeRecordGap(IReadOnlyList<string> warnings)
        {
            if (warnings == null)
                return false;

            for (int i = 0; i < warnings.Count; i++)
            {
                if (warnings[i] != null &&
                    warnings[i].Contains("No dated solo runtime sign-off record was found."))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
