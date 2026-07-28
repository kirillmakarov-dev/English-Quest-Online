using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EnglishQuest.Editor.PortfolioDemo
{
    public static class PortfolioDemoDocsMenu
    {
        private const string DocsRoot = "Assets/_PortfolioSlice/Docs/";
        private const string SoloRuntimeRecordTemplatePath = DocsRoot + "SoloRuntimeSignoff_Record_Template.md";
        private const string SoloFirstBatchmodeScriptPath = "scripts/Run-SoloFirstUnityEditMode.ps1";

        [MenuItem("Tools/English Quest/Docs/Open README")]
        public static void OpenReadme()
        {
            OpenAssetAtPath("README.md");
        }

        [MenuItem("Tools/English Quest/Docs/Open Roadmap")]
        public static void OpenRoadmap()
        {
            OpenAssetAtPath(DocsRoot + "MVP_SENIOR_ROADMAP.md");
        }

        [MenuItem("Tools/English Quest/Docs/Open Architecture")]
        public static void OpenArchitecture()
        {
            OpenAssetAtPath(DocsRoot + "Architecture.md");
        }

        [MenuItem("Tools/English Quest/Docs/Open Multiplayer")]
        public static void OpenMultiplayer()
        {
            OpenAssetAtPath(DocsRoot + "Multiplayer.md");
        }

        [MenuItem("Tools/English Quest/Docs/Open Quest Flow")]
        public static void OpenQuestFlow()
        {
            OpenAssetAtPath(DocsRoot + "QuestFlow.md");
        }

        [MenuItem("Tools/English Quest/Docs/Open Content Authoring")]
        public static void OpenContentAuthoring()
        {
            OpenAssetAtPath(DocsRoot + "ContentAuthoring.md");
        }

        [MenuItem("Tools/English Quest/Docs/Open Showcase Flow")]
        public static void OpenShowcaseFlow()
        {
            OpenAssetAtPath(DocsRoot + "ShowcaseFlow.md");
        }

        [MenuItem("Tools/English Quest/Docs/Open Manual Verification Checklist")]
        public static void OpenManualVerificationChecklist()
        {
            OpenAssetAtPath(DocsRoot + "ManualVerificationChecklist.md");
        }

        [MenuItem("Tools/English Quest/Docs/Open Solo-First Verification")]
        public static void OpenSoloFirstVerification()
        {
            OpenAssetAtPath(DocsRoot + "SoloFirstVerification.md");
        }

        [MenuItem("Tools/English Quest/Docs/Open Solo-First Status")]
        public static void OpenSoloFirstStatus()
        {
            OpenAssetAtPath(DocsRoot + "SoloFirst_Status.md");
        }

        [MenuItem("Tools/English Quest/Docs/Open Solo-First Proof Pack")]
        public static void OpenSoloFirstProofPack()
        {
            OpenAssetAtPath(DocsRoot + "SoloFirst_Status.md");
            OpenAssetAtPath(DocsRoot + "SoloFirstVerification.md");
            OpenAssetAtPath(DocsRoot + "SoloRuntimeSignoff.md");
            OpenAssetAtPath(SoloRuntimeRecordTemplatePath);
            OpenExternalFileAtProjectPath(SoloFirstBatchmodeScriptPath);
        }

        [MenuItem("Tools/English Quest/Docs/Open Solo Runtime Sign-off")]
        public static void OpenSoloRuntimeSignoff()
        {
            OpenAssetAtPath(DocsRoot + "SoloRuntimeSignoff.md");
        }

        [MenuItem("Tools/English Quest/Docs/Open Solo-First Batchmode Script")]
        public static void OpenSoloFirstBatchmodeScript()
        {
            OpenExternalFileAtProjectPath(SoloFirstBatchmodeScriptPath);
        }

        [MenuItem("Tools/English Quest/Docs/Open Solo Runtime Record Template")]
        public static void OpenSoloRuntimeRecordTemplate()
        {
            OpenAssetAtPath(SoloRuntimeRecordTemplatePath);
        }

        [MenuItem("Tools/English Quest/Docs/Create Solo Runtime Record From Template")]
        public static void CreateSoloRuntimeRecordFromTemplate()
        {
            string absoluteTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), SoloRuntimeRecordTemplatePath);
            if (!File.Exists(absoluteTemplatePath))
            {
                Debug.LogWarning($"[PortfolioDemoDocsMenu] Missing document template: {SoloRuntimeRecordTemplatePath}");
                return;
            }

            string dateStamp = DateTime.Now.ToString("yyyy-MM-dd");
            string newAssetPath = BuildUniqueSoloRuntimeRecordPath(dateStamp);
            string absoluteNewPath = Path.Combine(Directory.GetCurrentDirectory(), newAssetPath);

            Directory.CreateDirectory(Path.GetDirectoryName(absoluteNewPath) ?? string.Empty);
            File.Copy(absoluteTemplatePath, absoluteNewPath);

            AssetDatabase.Refresh();
            Debug.Log($"[PortfolioDemoDocsMenu] Created solo runtime record: {newAssetPath}");
            OpenAssetAtPath(newAssetPath);
        }

        private static string BuildUniqueSoloRuntimeRecordPath(string dateStamp)
        {
            string baseFileName = $"SoloRuntimeSignoff_Record_{dateStamp}";
            string candidatePath = DocsRoot + baseFileName + ".md";
            string absoluteCandidatePath = Path.Combine(Directory.GetCurrentDirectory(), candidatePath);
            if (!File.Exists(absoluteCandidatePath))
                return candidatePath;

            int suffix = 2;
            while (true)
            {
                candidatePath = DocsRoot + $"{baseFileName}_{suffix}.md";
                absoluteCandidatePath = Path.Combine(Directory.GetCurrentDirectory(), candidatePath);
                if (!File.Exists(absoluteCandidatePath))
                    return candidatePath;

                suffix++;
            }
        }

        private static void OpenAssetAtPath(string assetPath)
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
            {
                Debug.LogWarning($"[PortfolioDemoDocsMenu] Missing document: {assetPath}");
                return;
            }

            AssetDatabase.OpenAsset(asset);
            EditorGUIUtility.PingObject(asset);
        }

        private static void OpenExternalFileAtProjectPath(string projectRelativePath)
        {
            string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), projectRelativePath);
            if (!File.Exists(absolutePath))
            {
                Debug.LogWarning($"[PortfolioDemoDocsMenu] Missing file: {projectRelativePath}");
                return;
            }

            EditorUtility.OpenWithDefaultApp(absolutePath);
        }
    }
}
