using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EnglishQuest.Editor.PortfolioDemo;
using EnglishQuest.QuestSystem;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EnglishQuest.Tests
{
    [Category(TestCategories.Fast)]
    public class PortfolioDemoValidatorAnalyzerTests
    {
        private const string ProjectRoot = @"C:\Portfolio Projects\English-Quest-Online";
        private const string ActualScenePath =
            @"C:\Portfolio Projects\English-Quest-Online\English Quest online\Assets\_PortfolioSlice\Demo\Scenes\PortfolioDemo.unity";
        private const string TeacherLineAssetPath =
            @"C:\Portfolio Projects\English-Quest-Online\English Quest online\Assets\_PortfolioSlice\Demo\Data\QuestLines\01_TeacherAda_FirstQuestline\QuestLine_TeacherAda.asset";
        private const string CoachLineAssetPath =
            @"C:\Portfolio Projects\English-Quest-Online\English Quest online\Assets\_PortfolioSlice\Demo\Data\QuestLines\02_CoachBen_SecondQuestline\QuestLine_CoachBen.asset";
        private const string GuideLineAssetPath =
            @"C:\Portfolio Projects\English-Quest-Online\English Quest online\Assets\_PortfolioSlice\Demo\Data\QuestLines\03_GuideNora_ThirdQuestline\QuestLine_GuideNora.asset";
        private const string TeacherQuestAssetPath =
            @"C:\Portfolio Projects\English-Quest-Online\English Quest online\Assets\_PortfolioSlice\Demo\Data\QuestLines\01_TeacherAda_FirstQuestline\Quest_TeacherAda_Letters.asset";
        private const string CoachQuestAssetPath =
            @"C:\Portfolio Projects\English-Quest-Online\English Quest online\Assets\_PortfolioSlice\Demo\Data\QuestLines\02_CoachBen_SecondQuestline\Quest_CoachBen_MissingLetter.asset";
        private const string GuideQuestAssetPath =
            @"C:\Portfolio Projects\English-Quest-Online\English Quest online\Assets\_PortfolioSlice\Demo\Data\QuestLines\03_GuideNora_ThirdQuestline\Quest_GuideNora_Sentence.asset";
        private const string ActualRegistryAssetPath =
            "Assets/_PortfolioSlice/Demo/Data/QuestLines/Shared_MVP_Catalogs/QuestLineRegistry_MVP.asset";
        private const string ActualSessionProfileAssetPath =
            "Assets/_PortfolioSlice/Demo/Data/QuestLines/Shared_MVP_Catalogs/OpenWorldNetworkSessionProfile.asset";

        [Test]
        public void ActualPortfolioDemoScene_PassesStaticSoloFirstValidation()
        {
            Assume.That(File.Exists(ActualScenePath), $"Missing scene file under test: {ActualScenePath}");

            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateSceneFile(ActualScenePath, errors, warnings, infos);

            Assert.That(errors, Is.Empty, "The committed PortfolioDemo scene should keep the solo-first MVP wiring valid.");
        }

        [Test]
        public void ActualPortfolioDemoScene_UsesOptionalMultiplayerControlsCopy()
        {
            AssertAssetContains(
                ActualScenePath,
                "ENGLISH QUEST MVP - OPEN WORLD + QUEST CHAINS + OPTIONAL MULTIPLAYER",
                "Optional 2 Players via Photon Fusion",
                "Solo play is fully supported");
        }

        [Test]
        public void ActualShowcaseDocs_PassSoloFirstDocumentationValidation()
        {
            Assume.That(Directory.Exists(ProjectRoot), $"Missing project root under test: {ProjectRoot}");

            string previousDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(ProjectRoot);
            try
            {
                var warnings = new List<string>();
                var infos = new List<string>();

                PortfolioDemoValidationAnalyzer.ValidateShowcaseDocs(warnings, infos);

                Assert.That(
                    warnings,
                    Is.Empty,
                    "The committed README and docs should be clean once the dated solo-first runtime sign-off record exists.");
                Assert.That(infos, Has.Some.Contains("Showcase doc found: README.md"));
                Assert.That(infos, Has.Some.Contains("Showcase doc found: Assets/_PortfolioSlice/Docs/SoloFirst_Status.md"));
                Assert.That(infos, Has.Some.Contains("Showcase doc found: Assets/_PortfolioSlice/Docs/SoloFirstVerification.md"));
                Assert.That(infos, Has.Some.Contains("Showcase doc found: Assets/_PortfolioSlice/Docs/SoloRuntimeSignoff_Record_Template.md"));
                Assert.That(infos, Has.Some.Contains("Support file found: scripts/Run-SoloFirstUnityEditMode.ps1"));
                Assert.That(infos, Has.Some.Contains("Solo runtime record found: SoloRuntimeSignoff_Record_2026-07-28.md"));
                Assert.That(infos, Has.Some.Contains("Solo runtime record content checked: SoloRuntimeSignoff_Record_2026-07-28.md"));
            }
            finally
            {
                Directory.SetCurrentDirectory(previousDirectory);
            }
        }

        [Test]
        public void ActualQuestLineAssets_PreserveAdaBenNoraSoloChain()
        {
            AssertAssetContains(
                TeacherLineAssetPath,
                "lineId: line_teacher_ada",
                "npcId: teacher_ada",
                "prerequisiteLineId: ");

            AssertAssetContains(
                CoachLineAssetPath,
                "lineId: line_coach_ben",
                "npcId: coach_ben",
                "prerequisiteLineId: line_teacher_ada");

            AssertAssetContains(
                GuideLineAssetPath,
                "lineId: line_guide_nora",
                "npcId: guide_nora",
                "prerequisiteLineId: line_coach_ben");
        }

        [Test]
        public void ActualQuestDefinitionAssets_PreserveSoloFirstLessonBindings()
        {
            AssertAssetContains(
                TeacherQuestAssetPath,
                "giverNpcId: teacher_ada",
                "prerequisiteQuestId: ",
                "targetId: line_match");

            AssertAssetContains(
                CoachQuestAssetPath,
                "giverNpcId: coach_ben",
                "prerequisiteQuestId: ",
                "targetId: letter_ordering");

            AssertAssetContains(
                GuideQuestAssetPath,
                "giverNpcId: guide_nora",
                "prerequisiteQuestId: ",
                "targetId: word_ordering");
        }

        [Test]
        public void ActualCommittedQuestRegistry_PassesValidatorForSoloFirstMvpFlow()
        {
            QuestLineRegistrySO registry = AssetDatabase.LoadAssetAtPath<QuestLineRegistrySO>(ActualRegistryAssetPath);
            Assert.That(registry, Is.Not.Null, $"Missing committed MVP quest registry asset: {ActualRegistryAssetPath}");

            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateQuestRegistry(registry, errors, warnings, infos);

            Assert.That(errors, Is.Empty, "The committed MVP quest registry should keep the solo-first Ada -> Ben -> Nora flow valid.");
        }

        [Test]
        public void ActualOpenWorldNetworkSessionProfile_PassesValidatorForTwoPlayerSharedSlice()
        {
            NetworkSessionProfile profile = AssetDatabase.LoadAssetAtPath<NetworkSessionProfile>(ActualSessionProfileAssetPath);
            Assert.That(profile, Is.Not.Null, $"Missing committed open-world network session profile: {ActualSessionProfileAssetPath}");

            var errors = new List<string>();
            var warnings = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateSessionProfile(profile, errors, warnings);

            Assert.That(errors, Is.Empty, "The committed open-world session profile should stay valid for the portfolio slice.");
            Assert.That(warnings, Is.Empty, "The committed open-world session profile should already match the intended two-player Shared-mode configuration.");
        }

        [Test]
        public void ValidateQuestRegistry_ReportsMultipleRootLines()
        {
            QuestLineRegistrySO registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
            registry.questCatalog = ScriptableObject.CreateInstance<QuestCatalogSO>();
            registry.worldCatalogSet = ScriptableObject.CreateInstance<QuestWorldCatalogSetSO>();

            registry.questLines.Add(CreateLine("line_teacher_ada", "teacher_ada", null, "quest_ada", "line_match"));
            registry.questLines.Add(CreateLine("line_coach_ben", "coach_ben", null, "quest_ben", "letter_ordering"));
            registry.questLines.Add(CreateLine("line_guide_nora", "guide_nora", "line_coach_ben", "quest_nora", "word_ordering"));

            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateQuestRegistry(registry, errors, warnings, infos);

            Assert.That(errors, Has.Some.Contains("multiple root quest lines"));
            Assert.That(errors, Has.Some.Contains("Quest line 'line_coach_ben' has prerequisite ''"));
        }

        [Test]
        public void ValidateQuestRegistry_ReportsNpcOwnershipMismatch()
        {
            QuestLineRegistrySO registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
            registry.questCatalog = ScriptableObject.CreateInstance<QuestCatalogSO>();
            registry.worldCatalogSet = ScriptableObject.CreateInstance<QuestWorldCatalogSetSO>();

            registry.questLines.Add(CreateLine("line_teacher_ada", "teacher_ada", null, "quest_ada", "line_match"));
            registry.questLines.Add(CreateLine("line_coach_ben", "coach_ben", "line_teacher_ada", "quest_ben", "letter_ordering", giverNpcId: "teacher_ada"));
            registry.questLines.Add(CreateLine("line_guide_nora", "guide_nora", "line_coach_ben", "quest_nora", "word_ordering"));

            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateQuestRegistry(registry, errors, warnings, infos);

            Assert.That(errors, Has.Some.Contains("does not match line npcId"));
        }

        [Test]
        public void ValidateQuestRegistry_ReportsDuplicateMiniGameTargetAcrossFlow()
        {
            QuestLineRegistrySO registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
            registry.questCatalog = ScriptableObject.CreateInstance<QuestCatalogSO>();
            registry.worldCatalogSet = ScriptableObject.CreateInstance<QuestWorldCatalogSetSO>();

            registry.questLines.Add(CreateLine("line_teacher_ada", "teacher_ada", null, "quest_ada", "line_match"));
            registry.questLines.Add(CreateLine("line_coach_ben", "coach_ben", "line_teacher_ada", "quest_ben", "line_match"));
            registry.questLines.Add(CreateLine("line_guide_nora", "guide_nora", "line_coach_ben", "quest_nora", "word_ordering"));

            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateQuestRegistry(registry, errors, warnings, infos);

            Assert.That(errors, Has.Some.Contains("Duplicate mini-game objective targetId 'line_match'"));
        }

        [Test]
        public void ValidateQuestRegistry_ReportsOptionalCoopObjectiveAsInvalidForMainFlow()
        {
            QuestLineRegistrySO registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
            registry.questCatalog = ScriptableObject.CreateInstance<QuestCatalogSO>();
            registry.worldCatalogSet = ScriptableObject.CreateInstance<QuestWorldCatalogSetSO>();

            registry.questLines.Add(CreateLine("line_teacher_ada", "teacher_ada", null, "quest_ada", "line_match"));
            registry.questLines.Add(CreateLine("line_coach_ben", "coach_ben", "line_teacher_ada", "quest_ben", "study_circle"));
            registry.questLines.Add(CreateLine("line_guide_nora", "guide_nora", "line_coach_ben", "quest_nora", "word_ordering"));

            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateQuestRegistry(registry, errors, warnings, infos);

            Assert.That(errors, Has.Some.Contains("points to optional co-op activity 'study_circle'"));
            Assert.That(errors, Has.Some.Contains("must remain solo-playable"));
        }

        [Test]
        public void ValidateQuestRegistry_ReportsUnexpectedMandatoryMiniGameObjective()
        {
            QuestLineRegistrySO registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
            registry.questCatalog = ScriptableObject.CreateInstance<QuestCatalogSO>();
            registry.worldCatalogSet = ScriptableObject.CreateInstance<QuestWorldCatalogSetSO>();

            registry.questLines.Add(CreateLine("line_teacher_ada", "teacher_ada", null, "quest_ada", "line_match"));
            registry.questLines.Add(CreateLine("line_coach_ben", "coach_ben", "line_teacher_ada", "quest_ben", "letter_ordering"));
            registry.questLines.Add(CreateLine("line_guide_nora", "guide_nora", "line_coach_ben", "quest_nora", "bonus_activity"));

            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateQuestRegistry(registry, errors, warnings, infos);

            Assert.That(errors, Has.Some.Contains("missing required MVP lesson mini-game objective 'word_ordering'"));
            Assert.That(errors, Has.Some.Contains("unexpected MVP mini-game objective 'bonus_activity'"));
        }

        [Test]
        public void ValidateQuestRegistry_ReportsWhenNpcLessonUsesWrongExpectedMiniGame()
        {
            QuestLineRegistrySO registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
            registry.questCatalog = ScriptableObject.CreateInstance<QuestCatalogSO>();
            registry.worldCatalogSet = ScriptableObject.CreateInstance<QuestWorldCatalogSetSO>();

            registry.questLines.Add(CreateLine("line_teacher_ada", "teacher_ada", null, "quest_ada", "letter_ordering"));
            registry.questLines.Add(CreateLine("line_coach_ben", "coach_ben", "line_teacher_ada", "quest_ben", "line_match"));
            registry.questLines.Add(CreateLine("line_guide_nora", "guide_nora", "line_coach_ben", "quest_nora", "word_ordering"));

            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateQuestRegistry(registry, errors, warnings, infos);

            Assert.That(errors, Has.Some.Contains("uses 'letter_ordering' for npc 'teacher_ada'"));
            Assert.That(errors, Has.Some.Contains("requires 'line_match'"));
            Assert.That(errors, Has.Some.Contains("uses 'line_match' for npc 'coach_ben'"));
            Assert.That(errors, Has.Some.Contains("requires 'letter_ordering'"));
        }

        [Test]
        public void ValidateQuestRegistry_ReportsWhenRequiredLineUsesWrongNpcBinding()
        {
            QuestLineRegistrySO registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
            registry.questCatalog = ScriptableObject.CreateInstance<QuestCatalogSO>();
            registry.worldCatalogSet = ScriptableObject.CreateInstance<QuestWorldCatalogSetSO>();

            registry.questLines.Add(CreateLine("line_teacher_ada", "coach_ben", null, "quest_ada", "line_match"));
            registry.questLines.Add(CreateLine("line_coach_ben", "coach_ben", "line_teacher_ada", "quest_ben", "letter_ordering"));
            registry.questLines.Add(CreateLine("line_guide_nora", "guide_nora", "line_coach_ben", "quest_nora", "word_ordering"));

            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateQuestRegistry(registry, errors, warnings, infos);

            Assert.That(errors, Has.Some.Contains("Quest line 'line_teacher_ada' is bound to npc 'coach_ben'"));
            Assert.That(errors, Has.Some.Contains("requires 'teacher_ada'"));
        }

        [Test]
        public void ValidateQuestRegistry_ReportsDirectQuestPrerequisiteInsideRequiredMvpLine()
        {
            QuestLineRegistrySO registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
            registry.questCatalog = ScriptableObject.CreateInstance<QuestCatalogSO>();
            registry.worldCatalogSet = ScriptableObject.CreateInstance<QuestWorldCatalogSetSO>();

            QuestDefinitionSO benQuest = CreateQuest("quest_ben", "coach_ben", "letter_ordering");
            benQuest.prerequisiteQuestId = "bonus_hidden_gate";

            QuestLineSO benLine = ScriptableObject.CreateInstance<QuestLineSO>();
            benLine.lineId = "line_coach_ben";
            benLine.npcId = "coach_ben";
            benLine.prerequisiteLineId = "line_teacher_ada";
            benLine.quests = new List<QuestDefinitionSO> { benQuest };

            registry.questLines.Add(CreateLine("line_teacher_ada", "teacher_ada", null, "quest_ada", "line_match"));
            registry.questLines.Add(benLine);
            registry.questLines.Add(CreateLine("line_guide_nora", "guide_nora", "line_coach_ben", "quest_nora", "word_ordering"));

            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateQuestRegistry(registry, errors, warnings, infos);

            Assert.That(errors, Has.Some.Contains("Quest 'quest_ben' in MVP line 'line_coach_ben' has direct prerequisite quest id 'bonus_hidden_gate'"));
            Assert.That(errors, Has.Some.Contains("solo-first slice expects line unlocking to be controlled only by the line chain"));
        }

        [Test]
        public void ValidateQuestRegistry_AllowsExpectedSoloFirstLineChainWithoutDirectQuestPrerequisites()
        {
            QuestLineRegistrySO registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
            registry.questCatalog = ScriptableObject.CreateInstance<QuestCatalogSO>();
            registry.worldCatalogSet = ScriptableObject.CreateInstance<QuestWorldCatalogSetSO>();

            registry.questLines.Add(CreateLine("line_teacher_ada", "teacher_ada", null, "quest_ada", "line_match"));
            registry.questLines.Add(CreateLine("line_coach_ben", "coach_ben", "line_teacher_ada", "quest_ben", "letter_ordering"));
            registry.questLines.Add(CreateLine("line_guide_nora", "guide_nora", "line_coach_ben", "quest_nora", "word_ordering"));

            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateQuestRegistry(registry, errors, warnings, infos);

            Assert.That(errors, Has.None.Contains("direct prerequisite quest id"));
        }

        [Test]
        public void ValidateQuestRegistry_ReportsWhenRequiredLineIsMissing()
        {
            QuestLineRegistrySO registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
            registry.questCatalog = ScriptableObject.CreateInstance<QuestCatalogSO>();
            registry.worldCatalogSet = ScriptableObject.CreateInstance<QuestWorldCatalogSetSO>();

            registry.questLines.Add(CreateLine("line_teacher_ada", "teacher_ada", null, "quest_ada", "line_match"));
            registry.questLines.Add(CreateLine("line_coach_ben", "coach_ben", "line_teacher_ada", "quest_ben", "letter_ordering"));

            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateQuestRegistry(registry, errors, warnings, infos);

            Assert.That(errors, Has.Some.Contains("must contain exactly 3 quest lines"));
            Assert.That(errors, Has.Some.Contains("missing required MVP line 'line_guide_nora'"));
        }

        [Test]
        public void ValidateQuestRegistry_AcceptsExpectedMandatoryMiniGameObjectiveSet()
        {
            QuestLineRegistrySO registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
            registry.questCatalog = ScriptableObject.CreateInstance<QuestCatalogSO>();
            registry.worldCatalogSet = ScriptableObject.CreateInstance<QuestWorldCatalogSetSO>();

            registry.questLines.Add(CreateLine("line_teacher_ada", "teacher_ada", null, "quest_ada", "line_match"));
            registry.questLines.Add(CreateLine("line_coach_ben", "coach_ben", "line_teacher_ada", "quest_ben", "letter_ordering"));
            registry.questLines.Add(CreateLine("line_guide_nora", "guide_nora", "line_coach_ben", "quest_nora", "word_ordering"));

            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateQuestRegistry(registry, errors, warnings, infos);

            Assert.That(errors, Has.None.Contains("required MVP lesson mini-game objective"));
            Assert.That(errors, Has.None.Contains("unexpected MVP mini-game objective"));
        }

        [Test]
        public void ValidateQuestRegistry_ReportsWrongMiniGameConfigTypeForLessonTarget()
        {
            QuestLineRegistrySO registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
            registry.questCatalog = ScriptableObject.CreateInstance<QuestCatalogSO>();
            registry.worldCatalogSet = ScriptableObject.CreateInstance<QuestWorldCatalogSetSO>();

            QuestLineSO adaLine = ScriptableObject.CreateInstance<QuestLineSO>();
            adaLine.lineId = "line_teacher_ada";
            adaLine.npcId = "teacher_ada";
            adaLine.quests = new List<QuestDefinitionSO>
            {
                CreateQuestWithConfig(
                    "quest_ada",
                    "teacher_ada",
                    "line_match",
                    CreateLetterOrderingConfig("line_match"))
            };

            registry.questLines.Add(adaLine);
            registry.questLines.Add(CreateLine("line_coach_ben", "coach_ben", "line_teacher_ada", "quest_ben", "letter_ordering"));
            registry.questLines.Add(CreateLine("line_guide_nora", "guide_nora", "line_coach_ben", "quest_nora", "word_ordering"));

            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateQuestRegistry(registry, errors, warnings, infos);

            Assert.That(errors, Has.Some.Contains("uses config type 'LetterOrderingQuestConfigSO' for targetId 'line_match'"));
        }

        [Test]
        public void ValidateQuestRegistry_ReportsMissingSharedCatalogReferences()
        {
            QuestLineRegistrySO registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
            registry.questLines.Add(CreateLine("line_teacher_ada", "teacher_ada", null, "quest_ada", "line_match"));
            registry.questLines.Add(CreateLine("line_coach_ben", "coach_ben", "line_teacher_ada", "quest_ben", "letter_ordering"));
            registry.questLines.Add(CreateLine("line_guide_nora", "guide_nora", "line_coach_ben", "quest_nora", "word_ordering"));

            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateQuestRegistry(registry, errors, warnings, infos);

            Assert.That(errors, Has.Some.Contains("missing QuestCatalogSO"));
            Assert.That(errors, Has.Some.Contains("missing QuestWorldCatalogSetSO"));
        }

        [Test]
        public void ValidateSessionProfile_ReportsWrongGameModeAndEmptySessionName()
        {
            NetworkSessionProfile profile = ScriptableObject.CreateInstance<NetworkSessionProfile>();
            SetPrivateField(profile, "_gameMode", Fusion.GameMode.Host);
            SetPrivateField(profile, "_sessionName", "");
            SetPrivateField(profile, "_initialSceneName", "");

            var errors = new List<string>();
            var warnings = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateSessionProfile(profile, errors, warnings);

            Assert.That(warnings, Has.Some.Contains("not using Fusion Shared mode"));
            Assert.That(errors, Has.Some.Contains("empty session name"));
            Assert.That(errors, Has.Some.Contains("does not resolve a valid initial scene"));
        }

        [Test]
        public void ValidateSceneText_ReportsMissingRequiredMarkers()
        {
            string sceneText = "m_Name: Portfolio Demo HUD";
            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateSceneText(sceneText, errors, warnings, infos);

            Assert.That(errors, Has.Some.Contains("missing the Quest Manager"));
            Assert.That(errors, Has.Some.Contains("missing one or more required NPC objects"));
        }

        [Test]
        public void ValidateSceneText_ReportsNpcBindingProblems()
        {
            string sceneText = @"
m_Name: Quest Manager
m_Name: Quest Line Registrar
loadQuestState: 1
m_Name: Portfolio Demo HUD
m_Name: NPC - Teacher Ada
NpcQuestGiver
npcId: wrong_id
questLine: {fileID: 0}
m_Name: NPC - Coach Ben
NpcQuestGiver
npcId: coach_ben
questLine: {fileID: 11400000, guid: abc, type: 2}
m_Name: NPC - Guide Nora
NpcQuestGiver
npcId: guide_nora
questLine: {fileID: 11400000, guid: def, type: 2}
gameId: line_match
gameId: letter_ordering
gameId: word_ordering
";
            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateSceneText(sceneText, errors, warnings, infos);

            Assert.That(errors, Has.Some.Contains("NPC - Teacher Ada has an incorrect or missing npcId binding"));
            Assert.That(errors, Has.Some.Contains("NPC - Teacher Ada is missing its QuestLineSO binding"));
        }

        [Test]
        public void ValidateSceneText_ReportsHudBindingProblems()
        {
            string sceneText = @"
m_Name: Quest Manager
m_Name: Quest Line Registrar
loadQuestState: 1
m_Name: Portfolio Demo HUD
PortfolioDemoHud
interactionPrompt: {fileID: 0}
statusText: {fileID: 0}
objectiveEventBus: {fileID: 0}
dialogueManager: {fileID: 0}
m_Name: NPC - Teacher Ada
NpcQuestGiver
npcId: teacher_ada
questLine: {fileID: 11400000, guid: aaa, type: 2}
m_Name: NPC - Coach Ben
NpcQuestGiver
npcId: coach_ben
questLine: {fileID: 11400000, guid: bbb, type: 2}
m_Name: NPC - Guide Nora
NpcQuestGiver
npcId: guide_nora
questLine: {fileID: 11400000, guid: ccc, type: 2}
gameId: line_match
gameId: letter_ordering
gameId: word_ordering
";
            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateSceneText(sceneText, errors, warnings, infos);

            Assert.That(errors, Has.Some.Contains("Portfolio Demo HUD is missing its interactionPrompt binding"));
            Assert.That(errors, Has.Some.Contains("Portfolio Demo HUD is missing its statusText binding"));
            Assert.That(errors, Has.Some.Contains("Portfolio Demo HUD is missing its QuestObjectiveEventBus binding"));
            Assert.That(errors, Has.Some.Contains("Portfolio Demo HUD is missing its DialogueManager binding"));
        }

        [Test]
        public void ValidateSceneText_ReportsNetworkBindingProblems()
        {
            string sceneText = @"
m_Name: Quest Manager
m_Name: Quest Line Registrar
loadQuestState: 1
m_Name: Portfolio Demo HUD
PortfolioDemoHud
interactionPrompt: {fileID: 111}
statusText: {fileID: 222}
objectiveEventBus: {fileID: 333}
dialogueManager: {fileID: 444}
m_Name: Network
GameNetworkManager
PlayerSpawnCoordinator
PortfolioNetworkAutoStart
_openWorldProfile: {fileID: 0}
sessionProfile: {fileID: 0}
_defaultPlayerPrefab:
  guid: 00000000000000000000000000000000
m_Name: Spawn Point - Player One
Transform
m_Name: NPC - Teacher Ada
NpcQuestGiver
npcId: teacher_ada
questLine: {fileID: 11400000, guid: aaa, type: 2}
m_Name: NPC - Coach Ben
NpcQuestGiver
npcId: coach_ben
questLine: {fileID: 11400000, guid: bbb, type: 2}
m_Name: NPC - Guide Nora
NpcQuestGiver
npcId: guide_nora
questLine: {fileID: 11400000, guid: ccc, type: 2}
gameId: line_match
gameId: letter_ordering
gameId: word_ordering
";
            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateSceneText(sceneText, errors, warnings, infos);

            Assert.That(errors, Has.Some.Contains("Network object is missing EnglishQuestNetworkSceneManager"));
            Assert.That(errors, Has.Some.Contains("GameNetworkManager is missing its open-world NetworkSessionProfile binding"));
            Assert.That(errors, Has.Some.Contains("PortfolioNetworkAutoStart is missing its NetworkSessionProfile binding"));
            Assert.That(errors, Has.Some.Contains("PlayerSpawnCoordinator is missing its default player prefab binding"));
            Assert.That(errors, Has.Some.Contains("Spawn Point - Player One is missing its PlayerSpawnPoint component"));
            Assert.That(errors, Has.Some.Contains("PortfolioDemo scene is missing Spawn Point - Player Two"));
        }

        [Test]
        public void ValidateSceneText_ReportsMiniGameStationBindingProblems()
        {
            string sceneText = @"
m_Name: Quest Manager
m_Name: Quest Line Registrar
loadQuestState: 1
m_Name: Portfolio Demo HUD
m_Name: NPC - Teacher Ada
NpcQuestGiver
npcId: teacher_ada
questLine: {fileID: 11400000, guid: aaa, type: 2}
m_Name: NPC - Coach Ben
NpcQuestGiver
npcId: coach_ben
questLine: {fileID: 11400000, guid: bbb, type: 2}
m_Name: NPC - Guide Nora
NpcQuestGiver
npcId: guide_nora
questLine: {fileID: 11400000, guid: ccc, type: 2}
m_Name: Line Match Station
MiniGameWorldInteractable
gameId: line_match
launchHost: {fileID: 123}
wordGameBootstrap: {fileID: 0}
lineMatchBootstrap: {fileID: 0}
fallbackConfig: {fileID: 11400000, guid: ddd, type: 2}
m_Name: Letter Ordering Station
MiniGameWorldInteractable
gameId: letter_ordering
launchHost: {fileID: 456}
wordGameBootstrap: {fileID: 0}
lineMatchBootstrap: {fileID: 0}
fallbackConfig: {fileID: 11400000, guid: eee, type: 2}
m_Name: Word Ordering Station
MiniGameWorldInteractable
gameId: wrong_word_ordering
launchHost: {fileID: 789}
wordGameBootstrap: {fileID: 0}
lineMatchBootstrap: {fileID: 0}
fallbackConfig: {fileID: 0}
";
            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateSceneText(sceneText, errors, warnings, infos);

            Assert.That(errors, Has.Some.Contains("Line Match Station is missing the required LineMatch bootstrap binding"));
            Assert.That(errors, Has.Some.Contains("Letter Ordering Station is missing the required WordGameBootstrap binding"));
            Assert.That(errors, Has.Some.Contains("Word Ordering Station has an incorrect or missing gameId binding"));
            Assert.That(errors, Has.Some.Contains("Word Ordering Station is missing its fallback quest mini-game config"));
        }

        [Test]
        public void ValidateSceneText_WarnsWhenOptionalCoopStudyCircleIsMissing()
        {
            string sceneText = @"
m_Name: Quest Manager
m_Name: Quest Line Registrar
loadQuestState: 1
m_Name: Portfolio Demo HUD
PortfolioDemoHud
interactionPrompt: {fileID: 111}
statusText: {fileID: 222}
objectiveEventBus: {fileID: 333}
dialogueManager: {fileID: 444}
m_Name: NPC - Teacher Ada
NpcQuestGiver
npcId: teacher_ada
questLine: {fileID: 11400000, guid: aaa, type: 2}
m_Name: NPC - Coach Ben
NpcQuestGiver
npcId: coach_ben
questLine: {fileID: 11400000, guid: bbb, type: 2}
m_Name: NPC - Guide Nora
NpcQuestGiver
npcId: guide_nora
questLine: {fileID: 11400000, guid: ccc, type: 2}
m_Name: Network
GameNetworkManager
EnglishQuestNetworkSceneManager
PlayerSpawnCoordinator
PortfolioNetworkAutoStart
_openWorldProfile: {fileID: 111}
sessionProfile: {fileID: 222}
_defaultPlayerPrefab:
  guid: abcdef1234567890abcdef1234567890
m_Name: Spawn Point - Player One
PlayerSpawnPoint
m_Name: Spawn Point - Player Two
PlayerSpawnPoint
gameId: line_match
gameId: letter_ordering
gameId: word_ordering
";
            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateSceneText(sceneText, errors, warnings, infos);

            Assert.That(warnings, Has.Some.Contains("missing the Optional Co-op Study Circle"));
            Assert.That(warnings, Has.None.Contains("(Not Required)"));
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void ValidateSceneText_WarnsWhenOptionalCoopStudyCircleHasNoComponent()
        {
            string sceneText = @"
m_Name: Quest Manager
m_Name: Quest Line Registrar
loadQuestState: 1
m_Name: Portfolio Demo HUD
PortfolioDemoHud
interactionPrompt: {fileID: 111}
statusText: {fileID: 222}
objectiveEventBus: {fileID: 333}
dialogueManager: {fileID: 444}
m_Name: NPC - Teacher Ada
NpcQuestGiver
npcId: teacher_ada
questLine: {fileID: 11400000, guid: aaa, type: 2}
m_Name: NPC - Coach Ben
NpcQuestGiver
npcId: coach_ben
questLine: {fileID: 11400000, guid: bbb, type: 2}
m_Name: NPC - Guide Nora
NpcQuestGiver
npcId: guide_nora
questLine: {fileID: 11400000, guid: ccc, type: 2}
m_Name: Network
GameNetworkManager
EnglishQuestNetworkSceneManager
PlayerSpawnCoordinator
PortfolioNetworkAutoStart
_openWorldProfile: {fileID: 111}
sessionProfile: {fileID: 222}
_defaultPlayerPrefab:
  guid: abcdef1234567890abcdef1234567890
m_Name: Spawn Point - Player One
PlayerSpawnPoint
m_Name: Spawn Point - Player Two
PlayerSpawnPoint
m_Name: Optional Co-op Study Circle
Transform
gameId: line_match
gameId: letter_ordering
gameId: word_ordering
";
            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateSceneText(sceneText, errors, warnings, infos);

            Assert.That(warnings, Has.Some.Contains("missing its PortfolioOptionalCoopStudyCircle component"));
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void ValidateSceneText_WarnsWhenSoloFirstHudMessageIsMissing()
        {
            string sceneText = @"
m_Name: Quest Manager
m_Name: Quest Line Registrar
loadQuestState: 1
m_Name: Portfolio Demo HUD
PortfolioDemoHud
interactionPrompt: {fileID: 111}
statusText: {fileID: 222}
objectiveEventBus: {fileID: 333}
dialogueManager: {fileID: 444}
m_Name: NPC - Teacher Ada
NpcQuestGiver
npcId: teacher_ada
questLine: {fileID: 11400000, guid: aaa, type: 2}
m_Name: NPC - Coach Ben
NpcQuestGiver
npcId: coach_ben
questLine: {fileID: 11400000, guid: bbb, type: 2}
m_Name: NPC - Guide Nora
NpcQuestGiver
npcId: guide_nora
questLine: {fileID: 11400000, guid: ccc, type: 2}
m_Name: Network
GameNetworkManager
EnglishQuestNetworkSceneManager
PlayerSpawnCoordinator
PortfolioNetworkAutoStart
_openWorldProfile: {fileID: 111}
sessionProfile: {fileID: 222}
_defaultPlayerPrefab:
  guid: abcdef1234567890abcdef1234567890
m_Name: Spawn Point - Player One
PlayerSpawnPoint
m_Name: Spawn Point - Player Two
PlayerSpawnPoint
m_Name: Optional Co-op Study Circle
PortfolioOptionalCoopStudyCircle
Optional Co-op
Study Circle
gameId: line_match
gameId: letter_ordering
gameId: word_ordering
";
            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateSceneText(sceneText, errors, warnings, infos);

            Assert.That(warnings, Has.Some.Contains("missing an explicit solo-first HUD message"));
            Assert.That(warnings, Has.Some.Contains("may predate the latest portfolio HUD copy"));
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void ValidateSceneText_WarnsWhenHudTitleDoesNotSayOptionalMultiplayer()
        {
            string sceneText = @"
m_Name: Quest Manager
m_Name: Quest Line Registrar
loadQuestState: 1
m_Name: Portfolio Demo HUD
PortfolioDemoHud
interactionPrompt: {fileID: 111}
statusText: {fileID: 222}
objectiveEventBus: {fileID: 333}
dialogueManager: {fileID: 444}
Solo play is fully supported
ENGLISH QUEST MVP - OPEN WORLD + QUEST CHAINS + MULTIPLAYER
Optional 2 Players via Photon Fusion
m_Name: NPC - Teacher Ada
NpcQuestGiver
npcId: teacher_ada
questLine: {fileID: 11400000, guid: aaa, type: 2}
m_Name: NPC - Coach Ben
NpcQuestGiver
npcId: coach_ben
questLine: {fileID: 11400000, guid: bbb, type: 2}
m_Name: NPC - Guide Nora
NpcQuestGiver
npcId: guide_nora
questLine: {fileID: 11400000, guid: ccc, type: 2}
m_Name: Network
GameNetworkManager
EnglishQuestNetworkSceneManager
PlayerSpawnCoordinator
PortfolioNetworkAutoStart
_openWorldProfile: {fileID: 111}
sessionProfile: {fileID: 222}
_defaultPlayerPrefab:
  guid: abcdef1234567890abcdef1234567890
m_Name: Spawn Point - Player One
PlayerSpawnPoint
m_Name: Spawn Point - Player Two
PlayerSpawnPoint
gameId: line_match
gameId: letter_ordering
gameId: word_ordering
";
            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateSceneText(sceneText, errors, warnings, infos);

            Assert.That(warnings, Has.Some.Contains("old HUD title"));
            Assert.That(warnings, Has.Some.Contains("optional showcase layer"));
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void ValidateSceneText_WarnsWhenMultiplayerControlsDoNotSayOptional()
        {
            string sceneText = @"
m_Name: Quest Manager
m_Name: Quest Line Registrar
loadQuestState: 1
m_Name: Portfolio Demo HUD
PortfolioDemoHud
interactionPrompt: {fileID: 111}
statusText: {fileID: 222}
objectiveEventBus: {fileID: 333}
dialogueManager: {fileID: 444}
Solo play is fully supported
WASD - Move    Mouse - Look    Space - Jump    E - Interact    2 Players via Photon Fusion
m_Name: NPC - Teacher Ada
NpcQuestGiver
npcId: teacher_ada
questLine: {fileID: 11400000, guid: aaa, type: 2}
m_Name: NPC - Coach Ben
NpcQuestGiver
npcId: coach_ben
questLine: {fileID: 11400000, guid: bbb, type: 2}
m_Name: NPC - Guide Nora
NpcQuestGiver
npcId: guide_nora
questLine: {fileID: 11400000, guid: ccc, type: 2}
m_Name: Network
GameNetworkManager
EnglishQuestNetworkSceneManager
PlayerSpawnCoordinator
PortfolioNetworkAutoStart
_openWorldProfile: {fileID: 111}
sessionProfile: {fileID: 222}
_defaultPlayerPrefab:
  guid: abcdef1234567890abcdef1234567890
m_Name: Spawn Point - Player One
PlayerSpawnPoint
m_Name: Spawn Point - Player Two
PlayerSpawnPoint
gameId: line_match
gameId: letter_ordering
gameId: word_ordering
";
            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateSceneText(sceneText, errors, warnings, infos);

            Assert.That(warnings, Has.Some.Contains("old multiplayer controls label"));
            Assert.That(warnings, Has.Some.Contains("presented as optional"));
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void ValidateSceneText_WarnsWhenStudyCircleMissingNotRequiredMarker()
        {
            string sceneText = @"
m_Name: Quest Manager
m_Name: Quest Line Registrar
loadQuestState: 1
m_Name: Portfolio Demo HUD
PortfolioDemoHud
interactionPrompt: {fileID: 111}
statusText: {fileID: 222}
objectiveEventBus: {fileID: 333}
dialogueManager: {fileID: 444}
Solo play is fully supported
m_Name: NPC - Teacher Ada
NpcQuestGiver
npcId: teacher_ada
questLine: {fileID: 11400000, guid: aaa, type: 2}
m_Name: NPC - Coach Ben
NpcQuestGiver
npcId: coach_ben
questLine: {fileID: 11400000, guid: bbb, type: 2}
m_Name: NPC - Guide Nora
NpcQuestGiver
npcId: guide_nora
questLine: {fileID: 11400000, guid: ccc, type: 2}
m_Name: Network
GameNetworkManager
EnglishQuestNetworkSceneManager
PlayerSpawnCoordinator
PortfolioNetworkAutoStart
_openWorldProfile: {fileID: 111}
sessionProfile: {fileID: 222}
_defaultPlayerPrefab:
  guid: abcdef1234567890abcdef1234567890
m_Name: Spawn Point - Player One
PlayerSpawnPoint
m_Name: Spawn Point - Player Two
PlayerSpawnPoint
m_Name: Optional Co-op Study Circle
PortfolioOptionalCoopStudyCircle
Optional Co-op
Study Circle
gameId: line_match
gameId: letter_ordering
gameId: word_ordering
";
            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            PortfolioDemoValidationAnalyzer.ValidateSceneText(sceneText, errors, warnings, infos);

            Assert.That(warnings, Has.Some.Contains("missing its '(Not Required)' world label marker"));
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void ValidateSceneFile_ReportsWhenPortfolioDemoUsesWrongCommittedQuestRegistryAsset()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "EnglishQuestSceneTest_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            string scenePath = Path.Combine(tempRoot, "PortfolioDemo.unity");

            try
            {
                File.WriteAllText(
                    scenePath,
                    @"
m_Name: Quest Manager
m_Name: Quest Line Registrar
QuestLineRegistrar
registry: {fileID: 11400000, guid: WRONGREGISTRYGUID0000000000000000, type: 2}
loadQuestState: 1
m_Name: Portfolio Demo HUD
PortfolioDemoHud
interactionPrompt: {fileID: 111}
statusText: {fileID: 222}
objectiveEventBus: {fileID: 333}
dialogueManager: {fileID: 444}
Solo play is fully supported
m_Name: NPC - Teacher Ada
NpcQuestGiver
npcId: teacher_ada
questLine: {fileID: 11400000, guid: aaa, type: 2}
m_Name: NPC - Coach Ben
NpcQuestGiver
npcId: coach_ben
questLine: {fileID: 11400000, guid: bbb, type: 2}
m_Name: NPC - Guide Nora
NpcQuestGiver
npcId: guide_nora
questLine: {fileID: 11400000, guid: ccc, type: 2}
m_Name: Network
GameNetworkManager
EnglishQuestNetworkSceneManager
PlayerSpawnCoordinator
PortfolioNetworkAutoStart
_openWorldProfile: {fileID: 11400000, guid: bb4357919d7142eeb5b5c3391fc53c54, type: 2}
sessionProfile: {fileID: 11400000, guid: bb4357919d7142eeb5b5c3391fc53c54, type: 2}
_defaultPlayerPrefab:
  guid: abcdef1234567890abcdef1234567890
m_Name: Spawn Point - Player One
PlayerSpawnPoint
m_Name: Spawn Point - Player Two
PlayerSpawnPoint
m_Name: Optional Co-op Study Circle
PortfolioOptionalCoopStudyCircle
(Not Required)
gameId: line_match
gameId: letter_ordering
gameId: word_ordering
");

                var errors = new List<string>();
                var warnings = new List<string>();
                var infos = new List<string>();

                PortfolioDemoValidationAnalyzer.ValidateSceneFile(scenePath, errors, warnings, infos);

                Assert.That(errors, Has.Some.Contains("Quest Line Registrar is not bound to the committed QuestLineRegistry_MVP asset."));
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public void ValidateSceneFile_ReportsWhenPortfolioDemoUsesWrongCommittedNetworkProfileAsset()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "EnglishQuestSceneTest_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            string scenePath = Path.Combine(tempRoot, "PortfolioDemo.unity");

            try
            {
                File.WriteAllText(
                    scenePath,
                    @"
m_Name: Quest Manager
m_Name: Quest Line Registrar
QuestLineRegistrar
registry: {fileID: 11400000, guid: 3abff86e8fba4271bdb6ecee76bec7ba, type: 2}
loadQuestState: 1
m_Name: Portfolio Demo HUD
PortfolioDemoHud
interactionPrompt: {fileID: 111}
statusText: {fileID: 222}
objectiveEventBus: {fileID: 333}
dialogueManager: {fileID: 444}
Solo play is fully supported
m_Name: NPC - Teacher Ada
NpcQuestGiver
npcId: teacher_ada
questLine: {fileID: 11400000, guid: aaa, type: 2}
m_Name: NPC - Coach Ben
NpcQuestGiver
npcId: coach_ben
questLine: {fileID: 11400000, guid: bbb, type: 2}
m_Name: NPC - Guide Nora
NpcQuestGiver
npcId: guide_nora
questLine: {fileID: 11400000, guid: ccc, type: 2}
m_Name: Network
GameNetworkManager
EnglishQuestNetworkSceneManager
PlayerSpawnCoordinator
PortfolioNetworkAutoStart
_openWorldProfile: {fileID: 11400000, guid: WRONGPROFILEGUID00000000000000000, type: 2}
sessionProfile: {fileID: 11400000, guid: WRONGPROFILEGUID00000000000000000, type: 2}
_defaultPlayerPrefab:
  guid: abcdef1234567890abcdef1234567890
m_Name: Spawn Point - Player One
PlayerSpawnPoint
m_Name: Spawn Point - Player Two
PlayerSpawnPoint
m_Name: Optional Co-op Study Circle
PortfolioOptionalCoopStudyCircle
(Not Required)
gameId: line_match
gameId: letter_ordering
gameId: word_ordering
");

                var errors = new List<string>();
                var warnings = new List<string>();
                var infos = new List<string>();

                PortfolioDemoValidationAnalyzer.ValidateSceneFile(scenePath, errors, warnings, infos);

                Assert.That(errors, Has.Some.Contains("GameNetworkManager is not bound to the committed OpenWorldNetworkSessionProfile MVP asset."));
                Assert.That(errors, Has.Some.Contains("PortfolioNetworkAutoStart is not bound to the committed OpenWorldNetworkSessionProfile MVP asset."));
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public void ValidateShowcaseDocs_WarnsWhenRequiredDocIsMissing()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "EnglishQuestDocsTest_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            string docsRoot = Path.Combine(tempRoot, "Assets", "_PortfolioSlice", "Docs");
            Directory.CreateDirectory(docsRoot);

            try
            {
                File.WriteAllText(Path.Combine(tempRoot, "README.md"), "portfolio readme");
                WriteValidShowcaseDocs(docsRoot, includeChecklist: false);

                string previousDirectory = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(tempRoot);
                try
                {
                    var warnings = new List<string>();
                    var infos = new List<string>();

                    PortfolioDemoValidationAnalyzer.ValidateShowcaseDocs(warnings, infos);

                    Assert.That(warnings, Has.Some.Contains("Missing showcase doc: Assets/_PortfolioSlice/Docs/ManualVerificationChecklist.md"));
                    Assert.That(warnings, Has.Some.Contains("Missing showcase doc: Assets/_PortfolioSlice/Docs/SoloFirst_Status.md"));
                    Assert.That(warnings, Has.Some.Contains("Missing showcase doc: Assets/_PortfolioSlice/Docs/SoloFirstVerification.md"));
                    Assert.That(warnings, Has.Some.Contains("Missing showcase doc: Assets/_PortfolioSlice/Docs/SoloRuntimeSignoff.md"));
                    Assert.That(warnings, Has.Some.Contains("Missing showcase doc: Assets/_PortfolioSlice/Docs/SoloRuntimeSignoff_Record_Template.md"));
                    Assert.That(warnings, Has.Some.Contains("Missing support file: scripts/Run-SoloFirstUnityEditMode.ps1"));
                    Assert.That(warnings, Has.Some.Contains("No dated solo runtime sign-off record was found."));
                    Assert.That(infos, Has.Some.Contains("Showcase doc found: Assets/_PortfolioSlice/Docs/Architecture.md"));
                }
                finally
                {
                    Directory.SetCurrentDirectory(previousDirectory);
                }
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public void ValidateShowcaseDocs_AddsInfoWhenAllRequiredDocsExist()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "EnglishQuestDocsTest_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            string docsRoot = Path.Combine(tempRoot, "Assets", "_PortfolioSlice", "Docs");
            Directory.CreateDirectory(docsRoot);

            try
            {
                WriteValidRootReadme(tempRoot);
                WriteValidShowcaseDocs(docsRoot, includeChecklist: true);
                Directory.CreateDirectory(Path.Combine(tempRoot, "scripts"));
                File.WriteAllText(
                    Path.Combine(tempRoot, "scripts", "Run-SoloFirstUnityEditMode.ps1"),
                    "Write-Host 'solo-first guardrail'");
                File.WriteAllText(
                    Path.Combine(docsRoot, "SoloRuntimeSignoff_Record_2026-07-28.md"),
                    "# Record\n" +
                    "- [x] PASS\n" +
                    "- [x] One player finished `Ada -> Ben -> Nora` in one session\n" +
                    "- [x] No second player was required to activate any mission\n" +
                    "## Final sign-off statement\n" +
                    "Solo-first MVP rule verified in live runtime.\n");

                string previousDirectory = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(tempRoot);
                try
                {
                    var warnings = new List<string>();
                    var infos = new List<string>();

                    PortfolioDemoValidationAnalyzer.ValidateShowcaseDocs(warnings, infos);

                    Assert.That(warnings, Is.Empty);
                    Assert.That(infos, Has.Some.Contains("Showcase doc found: README.md"));
                    Assert.That(infos, Has.Some.Contains("Showcase doc found: Assets/_PortfolioSlice/Docs/ShowcaseFlow.md"));
                    Assert.That(infos, Has.Some.Contains("Showcase doc found: Assets/_PortfolioSlice/Docs/ManualVerificationChecklist.md"));
                    Assert.That(infos, Has.Some.Contains("Showcase doc found: Assets/_PortfolioSlice/Docs/SoloFirst_Status.md"));
                    Assert.That(infos, Has.Some.Contains("Showcase doc found: Assets/_PortfolioSlice/Docs/SoloFirstVerification.md"));
                    Assert.That(infos, Has.Some.Contains("Showcase doc found: Assets/_PortfolioSlice/Docs/SoloRuntimeSignoff.md"));
                    Assert.That(infos, Has.Some.Contains("Showcase doc found: Assets/_PortfolioSlice/Docs/SoloRuntimeSignoff_Record_Template.md"));
                    Assert.That(infos, Has.Some.Contains("Showcase doc content checked: README.md"));
                    Assert.That(infos, Has.Some.Contains("Showcase doc content checked: Assets/_PortfolioSlice/Docs/QuestFlow.md"));
                    Assert.That(infos, Has.Some.Contains("Showcase doc content checked: Assets/_PortfolioSlice/Docs/SoloFirst_Status.md"));
                    Assert.That(infos, Has.Some.Contains("Showcase doc content checked: Assets/_PortfolioSlice/Docs/SoloFirstVerification.md"));
                    Assert.That(infos, Has.Some.Contains("Showcase doc content checked: Assets/_PortfolioSlice/Docs/SoloRuntimeSignoff.md"));
                    Assert.That(infos, Has.Some.Contains("Showcase doc content checked: Assets/_PortfolioSlice/Docs/SoloRuntimeSignoff_Record_Template.md"));
                    Assert.That(infos, Has.Some.Contains("Support file found: scripts/Run-SoloFirstUnityEditMode.ps1"));
                    Assert.That(infos, Has.Some.Contains("Solo runtime record found: SoloRuntimeSignoff_Record_2026-07-28.md"));
                    Assert.That(infos, Has.Some.Contains("Solo runtime record content checked: SoloRuntimeSignoff_Record_2026-07-28.md"));
                    Assert.That(
                        infos,
                        Has.Some.Contains("Recommended solo-first proof order: run the batchmode guardrail first if needed, then SoloFirstVerification.md, then SoloRuntimeSignoff.md"));
                }
                finally
                {
                    Directory.SetCurrentDirectory(previousDirectory);
                }
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public void ValidateShowcaseDocs_WarnsWhenSoloFirstMarkersAreMissing()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "EnglishQuestDocsTest_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            string docsRoot = Path.Combine(tempRoot, "Assets", "_PortfolioSlice", "Docs");
            Directory.CreateDirectory(docsRoot);

            try
            {
                File.WriteAllText(Path.Combine(tempRoot, "README.md"), "English Quest Online");
                File.WriteAllText(Path.Combine(docsRoot, "Architecture.md"), "ok");
                File.WriteAllText(Path.Combine(docsRoot, "QuestFlow.md"), "Quest progression is per-player, not global.");
                File.WriteAllText(Path.Combine(docsRoot, "Multiplayer.md"), "Current networking goal:\n- two players can join the same open-world scene;");
                File.WriteAllText(Path.Combine(docsRoot, "ContentAuthoring.md"), "ok");
                File.WriteAllText(Path.Combine(docsRoot, "ShowcaseFlow.md"), "The project should read as a readable Photon Fusion Shared multiplayer slice.");
                File.WriteAllText(Path.Combine(docsRoot, "ManualVerificationChecklist.md"), "## Exit rule\n- the full solo path passes");
                File.WriteAllText(Path.Combine(docsRoot, "SoloFirst_Status.md"), "Solo-first status");
                File.WriteAllText(Path.Combine(docsRoot, "SoloFirstVerification.md"), "Sprint 3/4 verification");
                File.WriteAllText(Path.Combine(docsRoot, "SoloRuntimeSignoff.md"), "Solo runtime sign-off");
                File.WriteAllText(Path.Combine(docsRoot, "SoloRuntimeSignoff_Record_Template.md"), "Solo runtime record");

                string previousDirectory = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(tempRoot);
                try
                {
                    var warnings = new List<string>();
                    var infos = new List<string>();

                    PortfolioDemoValidationAnalyzer.ValidateShowcaseDocs(warnings, infos);

                    Assert.That(warnings, Has.Some.Contains("README.md' is missing solo-first marker 'one player must be able to enter the scene and complete the full lesson chain alone'"));
                    Assert.That(warnings, Has.Some.Contains("README.md' is missing solo-first marker 'Run-SoloFirstUnityEditMode.ps1'"));
                    Assert.That(warnings, Has.Some.Contains("README.md' is missing solo-first marker 'SoloFirst_Status.md'"));
                    Assert.That(warnings, Has.Some.Contains("README.md' is missing solo-first marker 'SoloFirstVerification.md'"));
                    Assert.That(warnings, Has.Some.Contains("README.md' is missing solo-first marker 'SoloRuntimeSignoff.md'"));
                    Assert.That(warnings, Has.Some.Contains("README.md' is missing solo-first marker 'SoloRuntimeSignoff_Record_Template.md'"));
                    Assert.That(warnings, Has.Some.Contains("QuestFlow.md' is missing solo-first marker 'solo play remains fully valid'"));
                    Assert.That(warnings, Has.Some.Contains("Multiplayer.md' is missing solo-first marker 'does not unlock the main lesson flow'"));
                    Assert.That(warnings, Has.Some.Contains("SoloFirst_Status.md' is missing solo-first marker 'one player must be able to enter the scene and complete the full lesson chain alone'"));
                    Assert.That(warnings, Has.Some.Contains("SoloFirst_Status.md' is missing solo-first marker 'Live Unity one-player pass: completed'"));
                    Assert.That(warnings, Has.Some.Contains("SoloFirst_Status.md' is missing solo-first marker 'Run-SoloFirstUnityEditMode.ps1'"));
                    Assert.That(warnings, Has.Some.Contains("ShowcaseFlow.md' is missing solo-first marker 'SoloFirstVerification.md'"));
                    Assert.That(warnings, Has.Some.Contains("ShowcaseFlow.md' is missing solo-first marker 'SoloRuntimeSignoff.md'"));
                    Assert.That(warnings, Has.Some.Contains("ShowcaseFlow.md' is missing solo-first marker 'never waits for a partner'"));
                    Assert.That(warnings, Has.Some.Contains("ManualVerificationChecklist.md' is missing solo-first marker 'complete the single-player pass first'"));
                    Assert.That(warnings, Has.Some.Contains("ManualVerificationChecklist.md' is missing solo-first marker 'only then verify the shared-session pass'"));
                    Assert.That(warnings, Has.Some.Contains("ManualVerificationChecklist.md' is missing solo-first marker 'SoloRuntimeSignoff.md'"));
                    Assert.That(warnings, Has.Some.Contains("ManualVerificationChecklist.md' is missing solo-first marker 'SoloFirstVerification.md'"));
                    Assert.That(warnings, Has.Some.Contains("ManualVerificationChecklist.md' is missing solo-first marker 'SoloRuntimeSignoff_Record_Template.md'"));
                    Assert.That(warnings, Has.Some.Contains("ManualVerificationChecklist.md' is missing solo-first marker 'The player can complete the full slice alone'"));
                    Assert.That(warnings, Has.Some.Contains("SoloFirstVerification.md' is missing solo-first marker 'one player must be able to complete the full lesson chain alone'"));
                    Assert.That(warnings, Has.Some.Contains("SoloFirstVerification.md' is missing solo-first marker 'SoloRuntimeSignoff.md'"));
                    Assert.That(warnings, Has.Some.Contains("SoloFirstVerification.md' is missing solo-first marker 'SoloRuntimeSignoff_Record_Template.md'"));
                    Assert.That(warnings, Has.Some.Contains("SoloRuntimeSignoff.md' is missing solo-first marker 'one player can enter the scene and finish the full MVP lesson chain alone'"));
                    Assert.That(warnings, Has.Some.Contains("SoloRuntimeSignoff.md' is missing solo-first marker 'Ada -> Ben -> Nora'"));
                    Assert.That(warnings, Has.Some.Contains("SoloRuntimeSignoff.md' is missing solo-first marker 'Run-SoloFirstUnityEditMode.ps1'"));
                    Assert.That(warnings, Has.Some.Contains("SoloRuntimeSignoff.md' is missing solo-first marker 'SoloRuntimeSignoff_Record_Template.md'"));
                    Assert.That(warnings, Has.Some.Contains("SoloRuntimeSignoff_Record_Template.md' is missing solo-first marker 'Use this file to record the result of the live one-player Unity pass'"));
                    Assert.That(warnings, Has.Some.Contains("SoloRuntimeSignoff_Record_Template.md' is missing solo-first marker 'No second player was required to activate any mission'"));
                    Assert.That(warnings, Has.Some.Contains("Missing support file: scripts/Run-SoloFirstUnityEditMode.ps1"));
                    Assert.That(warnings, Has.Some.Contains("No dated solo runtime sign-off record was found."));
                }
                finally
                {
                    Directory.SetCurrentDirectory(previousDirectory);
                }
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public void ValidateShowcaseDocs_WarnsWhenDatedSoloRuntimeRecordLacksProofMarkers()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "EnglishQuestDocsTest_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            string docsRoot = Path.Combine(tempRoot, "Assets", "_PortfolioSlice", "Docs");
            Directory.CreateDirectory(docsRoot);

            try
            {
                WriteValidRootReadme(tempRoot);
                WriteValidShowcaseDocs(docsRoot, includeChecklist: true);
                Directory.CreateDirectory(Path.Combine(tempRoot, "scripts"));
                File.WriteAllText(
                    Path.Combine(tempRoot, "scripts", "Run-SoloFirstUnityEditMode.ps1"),
                    "Write-Host 'solo-first guardrail'");
                File.WriteAllText(
                    Path.Combine(docsRoot, "SoloRuntimeSignoff_Record_2026-07-28.md"),
                    "placeholder");

                string previousDirectory = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(tempRoot);
                try
                {
                    var warnings = new List<string>();
                    var infos = new List<string>();

                    PortfolioDemoValidationAnalyzer.ValidateShowcaseDocs(warnings, infos);

                    Assert.That(infos, Has.Some.Contains("Solo runtime record found: SoloRuntimeSignoff_Record_2026-07-28.md"));
                    Assert.That(infos, Has.Some.Contains("Solo runtime record content checked: SoloRuntimeSignoff_Record_2026-07-28.md"));
                    Assert.That(warnings, Has.Some.Contains("Solo runtime record 'SoloRuntimeSignoff_Record_2026-07-28.md' is missing required marker '- [x] PASS'"));
                    Assert.That(warnings, Has.Some.Contains("Solo runtime record 'SoloRuntimeSignoff_Record_2026-07-28.md' is missing required marker 'One player finished `Ada -> Ben -> Nora` in one session'"));
                    Assert.That(warnings, Has.Some.Contains("Solo runtime record 'SoloRuntimeSignoff_Record_2026-07-28.md' is missing required marker 'No second player was required to activate any mission'"));
                    Assert.That(warnings, Has.Some.Contains("Solo runtime record 'SoloRuntimeSignoff_Record_2026-07-28.md' is missing required marker 'Final sign-off statement'"));
                }
                finally
                {
                    Directory.SetCurrentDirectory(previousDirectory);
                }
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public void ValidationSummary_WithErrors_PrioritizesFixingErrorsFirst()
        {
            string summary = PortfolioDemoValidationSummaryFormatter.BuildSummary(
                errorCount: 2,
                warningCount: 1,
                infoCount: 5,
                warnings: new List<string> { "Any warning" });

            StringAssert.Contains("Errors: 2", summary);
            StringAssert.Contains("Warnings: 1", summary);
            StringAssert.Contains("Fix the reported errors first", summary);
            StringAssert.DoesNotContain("run SoloFirstVerification.md first", summary);
        }

        [Test]
        public void ValidationSummary_WithWarningsAndNoErrors_RecommendsSoloFirstVerificationAfterCleanup()
        {
            string summary = PortfolioDemoValidationSummaryFormatter.BuildSummary(
                errorCount: 0,
                warningCount: 3,
                infoCount: 9,
                warnings: new List<string> { "Regular setup warning" });

            StringAssert.Contains("Warnings: 3", summary);
            StringAssert.Contains("clear the warnings", summary);
            StringAssert.Contains("Solo-First Proof Pack", summary);
            StringAssert.Contains("solo-first batchmode guardrail", summary);
            StringAssert.Contains("SoloFirstVerification.md", summary);
            StringAssert.Contains("SoloRuntimeSignoff.md", summary);
        }

        [Test]
        public void ValidationSummary_WhenClean_RecommendsSoloFirstThenFullChecklist()
        {
            string summary = PortfolioDemoValidationSummaryFormatter.BuildSummary(
                errorCount: 0,
                warningCount: 0,
                infoCount: 12,
                warnings: new List<string>());

            StringAssert.Contains("Errors: 0", summary);
            StringAssert.Contains("Warnings: 0", summary);
            StringAssert.Contains("Solo-First Proof Pack", summary);
            StringAssert.Contains("solo-first batchmode guardrail", summary);
            StringAssert.Contains("SoloRuntimeSignoff.md", summary);
            StringAssert.Contains("ManualVerificationChecklist.md", summary);
        }

        [Test]
        public void ValidationSummary_WithMissingSoloRuntimeRecordWarning_PrioritizesLiveOnePlayerProof()
        {
            string summary = PortfolioDemoValidationSummaryFormatter.BuildSummary(
                errorCount: 0,
                warningCount: 1,
                infoCount: 11,
                warnings: new List<string>
                {
                    "No dated solo runtime sign-off record was found. Run SoloRuntimeSignoff.md, then create and save a SoloRuntimeSignoff_Record_YYYY-MM-DD.md file after the live one-player Unity pass."
                });

            StringAssert.Contains("solo-first coding guardrails are in place", summary);
            StringAssert.Contains("Solo-First Proof Pack", summary);
            StringAssert.Contains("SoloRuntimeSignoff.md", summary);
            StringAssert.Contains("SoloRuntimeSignoff_Record_YYYY-MM-DD.md", summary);
            StringAssert.DoesNotContain("clear the warnings", summary);
        }

        private static QuestLineSO CreateLine(
            string lineId,
            string npcId,
            string prerequisiteLineId,
            string questId,
            string miniGameId,
            string giverNpcId = null)
        {
            QuestLineSO line = ScriptableObject.CreateInstance<QuestLineSO>();
            line.lineId = lineId;
            line.npcId = npcId;
            line.prerequisiteLineId = prerequisiteLineId;
            line.quests = new List<QuestDefinitionSO>
            {
                CreateQuest(questId, giverNpcId ?? npcId, miniGameId)
            };
            return line;
        }

        private static QuestDefinitionSO CreateQuest(string questId, string giverNpcId, string miniGameId)
        {
            QuestDefinitionSO definition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            definition.id = questId;
            definition.giverNpcId = giverNpcId;
            definition.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition
                {
                    type = QuestObjectiveType.CompleteMiniGame,
                    targetId = miniGameId,
                    miniGameConfig = CreateLineMatchConfig(miniGameId)
                }
            };
            return definition;
        }

        private static QuestDefinitionSO CreateQuestWithConfig(
            string questId,
            string giverNpcId,
            string miniGameId,
            QuestMiniGameConfigSO config)
        {
            QuestDefinitionSO definition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            definition.id = questId;
            definition.giverNpcId = giverNpcId;
            definition.objectives = new List<QuestObjectiveDefinition>
            {
                new QuestObjectiveDefinition
                {
                    type = QuestObjectiveType.CompleteMiniGame,
                    targetId = miniGameId,
                    miniGameConfig = config
                }
            };
            return definition;
        }

        private static LineMatchQuestConfigSO CreateLineMatchConfig(string gameId)
        {
            LineMatchQuestConfigSO config = ScriptableObject.CreateInstance<LineMatchQuestConfigSO>();
            SetPrivateField(config, "gameId", gameId);
            SetPrivateField(config, "levelConfig", ScriptableObject.CreateInstance<Puzzle.Gameplay.MiniGames.LetterConnection.LetterConnectionLevelConfigSO>());
            return config;
        }

        private static LetterOrderingQuestConfigSO CreateLetterOrderingConfig(string gameId)
        {
            LetterOrderingQuestConfigSO config = ScriptableObject.CreateInstance<LetterOrderingQuestConfigSO>();
            SetPrivateField(config, "gameId", gameId);
            SetPrivateField(config, "data", ScriptableObject.CreateInstance<Puzzle.Gameplay.MiniGames.DuolingoWordGame.LetterOrderingDataSO>());
            return config;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }

        private static void WriteValidShowcaseDocs(string docsRoot, bool includeChecklist)
        {
            File.WriteAllText(Path.Combine(docsRoot, "Architecture.md"), "ok");
            File.WriteAllText(
                Path.Combine(docsRoot, "QuestFlow.md"),
                "solo play remains fully valid\nmust still play exactly the same way");
            File.WriteAllText(
                Path.Combine(docsRoot, "Multiplayer.md"),
                "solo player can complete the entire slice without a partner\ndoes not unlock the main lesson flow");
            File.WriteAllText(Path.Combine(docsRoot, "ContentAuthoring.md"), "ok");
            File.WriteAllText(
                Path.Combine(docsRoot, "ShowcaseFlow.md"),
                "solo-playable\nnever waits for a partner\nSoloFirstVerification.md\nSoloRuntimeSignoff.md");

            if (includeChecklist)
            {
                File.WriteAllText(
                    Path.Combine(docsRoot, "ManualVerificationChecklist.md"),
                    "complete the single-player pass first\n" +
                    "only then verify the shared-session pass\n" +
                    "SoloRuntimeSignoff.md\n" +
                    "SoloRuntimeSignoff_Record_Template.md\n" +
                    "The player can complete the full slice alone\n" +
                    "nothing in the UI suggests that a second player is required for mission activation\n" +
                    "SoloFirstVerification.md");

                File.WriteAllText(
                    Path.Combine(docsRoot, "SoloFirst_Status.md"),
                    "one player must be able to enter the scene and complete the full lesson chain alone\n" +
                    "Live Unity one-player pass: completed\n" +
                    "Open Solo-First Proof Pack\n" +
                    "SoloRuntimeSignoff.md\n" +
                    "SoloRuntimeSignoff_Record_Template.md");

                File.WriteAllText(
                    Path.Combine(docsRoot, "SoloFirstVerification.md"),
                    "one player must be able to complete the full lesson chain alone\n" +
                    "must never be required to activate, start, or complete a mission\n" +
                    "Open Solo-First Proof Pack\n" +
                    "A complete Ada -> Ben -> Nora run works in a single-player session\n" +
                    "SoloRuntimeSignoff.md\n" +
                    "SoloRuntimeSignoff_Record_Template.md");

                File.WriteAllText(
                    Path.Combine(docsRoot, "SoloRuntimeSignoff.md"),
                    "one player can enter the scene and finish the full MVP lesson chain alone\n" +
                    "no second player is required to activate, start, unlock, or complete any mission\n" +
                    "Open Solo-First Proof Pack\n" +
                    "Ada -> Ben -> Nora\n" +
                    "Hard fail conditions\n" +
                    "SoloRuntimeSignoff_Record_Template.md");

                File.WriteAllText(
                    Path.Combine(docsRoot, "SoloRuntimeSignoff_Record_Template.md"),
                    "Use this file to record the result of the live one-player Unity pass\n" +
                    "SoloRuntimeSignoff.md\n" +
                    "One player finished `Ada -> Ben -> Nora` in one session\n" +
                    "No second player was required to activate any mission");
            }
        }

        private static void WriteValidRootReadme(string tempRoot)
        {
            File.WriteAllText(
                Path.Combine(tempRoot, "README.md"),
                "one player must be able to enter the scene and complete the full lesson chain alone\n" +
                "not to gate mission activation\n" +
                "does not require a second player to activate any mission\n" +
                "SoloFirst_Status.md\n" +
                "SoloFirstVerification.md\n" +
                "SoloRuntimeSignoff.md\n" +
                "SoloRuntimeSignoff_Record_Template.md");
        }

        private static void AssertAssetContains(string assetPath, params string[] markers)
        {
            Assume.That(File.Exists(assetPath), $"Missing asset file under test: {assetPath}");
            string content = File.ReadAllText(assetPath);

            for (int i = 0; i < markers.Length; i++)
                StringAssert.Contains(markers[i], content, $"Asset '{assetPath}' is missing marker '{markers[i]}'.");
        }
    }
}
