using System.Collections.Generic;
using System.IO;
using EnglishQuest.QuestSystem;
using UnityEngine;

namespace EnglishQuest.Editor.PortfolioDemo
{
    public static class PortfolioDemoValidationAnalyzer
    {
        private static readonly HashSet<string> ForbiddenOptionalCoopObjectiveIds = new()
        {
            "study_circle",
            "optional_coop",
            "optional_study_circle",
            "study_circle_optional"
        };

        private static readonly string[] RequiredLessonMiniGameObjectiveIds =
        {
            "line_match",
            "letter_ordering",
            "word_ordering"
        };

        private static readonly Dictionary<string, string> ExpectedNpcMiniGameObjectiveIds = new()
        {
            { "teacher_ada", "line_match" },
            { "coach_ben", "letter_ordering" },
            { "guide_nora", "word_ordering" }
        };

        private static readonly Dictionary<string, string> ExpectedPrerequisiteByLineId = new()
        {
            { "line_teacher_ada", string.Empty },
            { "line_coach_ben", "line_teacher_ada" },
            { "line_guide_nora", "line_coach_ben" }
        };

        private static readonly Dictionary<string, string> ExpectedNpcByLineId = new()
        {
            { "line_teacher_ada", "teacher_ada" },
            { "line_coach_ben", "coach_ben" },
            { "line_guide_nora", "guide_nora" }
        };

        private static readonly Dictionary<string, string> ForbiddenDirectQuestPrerequisiteByLineId = new()
        {
            { "line_teacher_ada", string.Empty },
            { "line_coach_ben", string.Empty },
            { "line_guide_nora", string.Empty }
        };

        private const string ExpectedMvpRegistryGuid = "3abff86e8fba4271bdb6ecee76bec7ba";
        private const string ExpectedOpenWorldNetworkSessionProfileGuid = "bb4357919d7142eeb5b5c3391fc53c54";

        private static readonly string[] RequiredShowcaseDocs =
        {
            "README.md",
            "Assets/_PortfolioSlice/Docs/Architecture.md",
            "Assets/_PortfolioSlice/Docs/QuestFlow.md",
            "Assets/_PortfolioSlice/Docs/Multiplayer.md",
            "Assets/_PortfolioSlice/Docs/ContentAuthoring.md",
            "Assets/_PortfolioSlice/Docs/ShowcaseFlow.md",
            "Assets/_PortfolioSlice/Docs/ManualVerificationChecklist.md",
            "Assets/_PortfolioSlice/Docs/SoloFirst_Status.md",
            "Assets/_PortfolioSlice/Docs/SoloFirstVerification.md",
            "Assets/_PortfolioSlice/Docs/SoloRuntimeSignoff.md",
            "Assets/_PortfolioSlice/Docs/SoloRuntimeSignoff_Record_Template.md"
        };

        private static readonly string[] RequiredSupportFiles =
        {
            "scripts/Run-SoloFirstUnityEditMode.ps1"
        };

        private const string SoloRuntimeRecordDocsRoot = "Assets/_PortfolioSlice/Docs";
        private const string SoloRuntimeRecordTemplateFileName = "SoloRuntimeSignoff_Record_Template.md";
        private static readonly string[] RequiredSoloRuntimeRecordMarkers =
        {
            "- [x] PASS",
            "One player finished `Ada -> Ben -> Nora` in one session",
            "No second player was required to activate any mission",
            "Final sign-off statement"
        };

        private static readonly Dictionary<string, string[]> RequiredShowcaseDocMarkers = new()
        {
            {
                "README.md",
                new[]
                {
                    "one player must be able to enter the scene and complete the full lesson chain alone",
                    "not to gate mission activation",
                    "does not require a second player to activate any mission",
                    "Run-SoloFirstUnityEditMode.ps1",
                    "SoloFirst_Status.md",
                    "SoloFirstVerification.md",
                    "SoloRuntimeSignoff.md",
                    "SoloRuntimeSignoff_Record_Template.md"
                }
            },
            {
                "Assets/_PortfolioSlice/Docs/QuestFlow.md",
                new[]
                {
                    "solo play remains fully valid",
                    "must still play exactly the same way"
                }
            },
            {
                "Assets/_PortfolioSlice/Docs/Multiplayer.md",
                new[]
                {
                    "solo player can complete the entire slice without a partner",
                    "does not unlock the main lesson flow"
                }
            },
            {
                "Assets/_PortfolioSlice/Docs/ShowcaseFlow.md",
                new[]
                {
                    "solo-playable",
                    "never waits for a partner",
                    "SoloFirstVerification.md",
                    "SoloRuntimeSignoff.md"
                }
            },
            {
                "Assets/_PortfolioSlice/Docs/ManualVerificationChecklist.md",
                new[]
                {
                    "complete the single-player pass first",
                    "only then verify the shared-session pass",
                    "The player can complete the full slice alone",
                    "second player is required for mission activation",
                    "SoloFirstVerification.md",
                    "SoloRuntimeSignoff_Record_Template.md"
                }
            },
            {
                "Assets/_PortfolioSlice/Docs/SoloFirst_Status.md",
                new[]
                {
                    "one player must be able to enter the scene and complete the full lesson chain alone",
                    "Live Unity one-player pass: completed",
                    "Open Solo-First Proof Pack",
                    "Run-SoloFirstUnityEditMode.ps1",
                    "SoloRuntimeSignoff.md",
                    "SoloRuntimeSignoff_Record_Template.md"
                }
            },
            {
                "Assets/_PortfolioSlice/Docs/SoloFirstVerification.md",
                new[]
                {
                    "one player must be able to complete the full lesson chain alone",
                    "must never be required to activate, start, or complete a mission",
                    "Open Solo-First Proof Pack",
                    "A complete Ada -> Ben -> Nora run works in a single-player session",
                    "SoloRuntimeSignoff.md",
                    "SoloRuntimeSignoff_Record_Template.md"
                }
            },
            {
                "Assets/_PortfolioSlice/Docs/SoloRuntimeSignoff.md",
                new[]
                {
                    "one player can enter the scene and finish the full MVP lesson chain alone",
                    "no second player is required to activate, start, unlock, or complete any mission",
                    "Open Solo-First Proof Pack",
                    "Ada -> Ben -> Nora",
                    "Run-SoloFirstUnityEditMode.ps1",
                    "Hard fail conditions",
                    "SoloRuntimeSignoff_Record_Template.md"
                }
            },
            {
                "Assets/_PortfolioSlice/Docs/SoloRuntimeSignoff_Record_Template.md",
                new[]
                {
                    "Use this file to record the result of the live one-player Unity pass",
                    "SoloRuntimeSignoff.md",
                    "One player finished `Ada -> Ben -> Nora` in one session",
                    "No second player was required to activate any mission"
                }
            }
        };

        public static void ValidateSceneText(string sceneText, List<string> errors, List<string> warnings, List<string> infos)
        {
            if (string.IsNullOrWhiteSpace(sceneText))
            {
                errors.Add("PortfolioDemo scene text is empty or missing.");
                return;
            }

            if (!sceneText.Contains("m_Name: Quest Manager"))
                errors.Add("PortfolioDemo scene is missing the Quest Manager object.");

            if (!sceneText.Contains("m_Name: Quest Line Registrar"))
                errors.Add("PortfolioDemo scene is missing the Quest Line Registrar object.");

            if (!sceneText.Contains("loadQuestState: 1"))
                errors.Add("PortfolioDemo scene is still serialized with loadQuestState disabled.");

            if (!sceneText.Contains("m_Name: NPC - Teacher Ada") ||
                !sceneText.Contains("m_Name: NPC - Coach Ben") ||
                !sceneText.Contains("m_Name: NPC - Guide Nora"))
            {
                errors.Add("PortfolioDemo scene is missing one or more required NPC objects.");
            }

            if (!sceneText.Contains("gameId: line_match") ||
                !sceneText.Contains("gameId: letter_ordering") ||
                !sceneText.Contains("gameId: word_ordering"))
            {
                errors.Add("PortfolioDemo scene is missing one or more required mini-game stations.");
            }

            if (!sceneText.Contains("m_Name: Portfolio Demo HUD"))
                warnings.Add("PortfolioDemo scene has no Portfolio Demo HUD object.");

            ValidateHudBindings(sceneText, errors);
            ValidateNetworkBindings(sceneText, errors);
            ValidateNpcBindings(sceneText, errors);
            ValidateMiniGameStationBindings(sceneText, errors);
            ValidateOptionalCoopBindings(sceneText, warnings);
            ValidateSoloFirstPresentation(sceneText, warnings);

            infos.Add("PortfolioDemo scene text passed static validation.");
        }

        public static void ValidateQuestRegistry(QuestLineRegistrySO registry, List<string> errors, List<string> warnings, List<string> infos)
        {
            if (registry == null)
            {
                errors.Add("Quest registry asset is missing.");
                return;
            }

            infos.Add($"Quest registry loaded: {registry.name}");

            if (registry.questCatalog == null)
                errors.Add("Quest registry is missing QuestCatalogSO.");

            if (registry.worldCatalogSet == null)
                errors.Add("Quest registry is missing QuestWorldCatalogSetSO.");

            if (registry.questRuntimeShellPrefab == null)
                warnings.Add("Quest registry is missing questRuntimeShellPrefab.");

            if (registry.activeQuestJournalCanvasPrefab == null)
                warnings.Add("Quest registry is missing activeQuestJournalCanvasPrefab.");

            if (registry.questLines == null || registry.questLines.Count != 3)
                errors.Add("Quest registry must contain exactly 3 quest lines for the MVP slice.");

            var lineIds = new HashSet<string>();
            var questIds = new HashSet<string>();
            var miniGameIds = new HashSet<string>();
            int rootLineCount = 0;

            if (registry.questLines == null)
                return;

            foreach (QuestLineSO line in registry.questLines)
            {
                if (line == null)
                {
                    errors.Add("Quest registry contains a null quest line reference.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line.lineId))
                {
                    errors.Add($"Quest line '{line.name}' has an empty lineId.");
                }
                else
                {
                    if (!lineIds.Add(line.lineId))
                        errors.Add($"Duplicate quest line id: {line.lineId}");

                    if (line.prerequisiteLineId == line.lineId)
                        errors.Add($"Quest line '{line.lineId}' cannot reference itself as prerequisite.");
                }

                if (string.IsNullOrWhiteSpace(line.npcId))
                    errors.Add($"Quest line '{line.name}' has an empty npcId.");

                if (string.IsNullOrWhiteSpace(line.prerequisiteLineId))
                    rootLineCount++;

                if (line.quests == null || line.quests.Count == 0)
                    errors.Add($"Quest line '{line.name}' has no quests assigned.");

                if (line.worldCatalogSet == null && registry.worldCatalogSet == null)
                    warnings.Add($"Quest line '{line.name}' has no world catalog fallback.");

                if (line.quests == null)
                    continue;

                foreach (QuestDefinitionSO definition in line.quests)
                {
                    if (definition == null)
                    {
                        errors.Add($"Quest line '{line.name}' contains a null quest definition.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(definition.id))
                        errors.Add($"Quest definition '{definition.name}' has an empty id.");
                    else if (!questIds.Add(definition.id))
                        errors.Add($"Duplicate quest id: {definition.id}");

                    if (string.IsNullOrWhiteSpace(definition.giverNpcId))
                    {
                        warnings.Add($"Quest '{definition.id}' has no giverNpcId.");
                    }
                    else if (!string.IsNullOrWhiteSpace(line.npcId) && definition.giverNpcId != line.npcId)
                    {
                        errors.Add(
                            $"Quest '{definition.id}' giverNpcId '{definition.giverNpcId}' does not match line npcId '{line.npcId}'.");
                    }

                    if (definition.startDialogue == null)
                        warnings.Add($"Quest '{definition.id}' has no start dialogue.");

                    ValidateForbiddenDirectQuestPrerequisite(line, definition, errors);

                    if (definition.objectives == null || definition.objectives.Count == 0)
                    {
                        errors.Add($"Quest '{definition.id}' has no objectives.");
                        continue;
                    }

                    for (int i = 0; i < definition.objectives.Count; i++)
                    {
                        QuestObjectiveDefinition objective = definition.objectives[i];
                        if (objective == null)
                        {
                            errors.Add($"Quest '{definition.id}' has a null objective at index {i}.");
                            continue;
                        }

                        if (objective.type != QuestObjectiveType.CompleteMiniGame)
                            continue;

                        if (string.IsNullOrWhiteSpace(objective.targetId))
                        {
                            errors.Add($"Quest '{definition.id}' objective {i} has an empty mini-game targetId.");
                            continue;
                        }

                        if (ForbiddenOptionalCoopObjectiveIds.Contains(objective.targetId))
                        {
                            errors.Add(
                                $"Quest '{definition.id}' objective {i} points to optional co-op activity '{objective.targetId}'. " +
                                "The MVP learning path must remain solo-playable and cannot require a second player.");
                            continue;
                        }

                        if (!miniGameIds.Add(objective.targetId))
                        {
                            errors.Add(
                                $"Duplicate mini-game objective targetId '{objective.targetId}' across the MVP quest flow.");
                        }

                        if (objective.miniGameConfig == null)
                        {
                            errors.Add($"Quest '{definition.id}' objective {i} is missing QuestMiniGameConfigSO.");
                            continue;
                        }

                        if (objective.miniGameConfig.GameId != objective.targetId)
                        {
                            errors.Add(
                                $"Quest '{definition.id}' objective {i} targetId '{objective.targetId}' " +
                                $"does not match config GameId '{objective.miniGameConfig.GameId}'.");
                        }

                        ValidateExpectedLessonMapping(line.npcId, definition.id, i, objective.targetId, errors);
                        ValidateExpectedMiniGameConfigType(definition.id, i, objective.targetId, objective.miniGameConfig, errors);
                        ValidateMiniGameConfig(definition.id, i, objective.miniGameConfig, errors);
                    }
                }
            }

            ValidateRequiredLessonMiniGames(miniGameIds, errors);
            ValidateRequiredLineStructure(registry.questLines, errors);

            if (rootLineCount == 0)
                errors.Add("Quest registry has no root quest line. Solo flow would have no starting lesson.");
            else if (rootLineCount > 1)
                errors.Add("Quest registry has multiple root quest lines. MVP solo flow must start from exactly one lesson.");

            foreach (QuestLineSO line in registry.questLines)
            {
                if (line == null || string.IsNullOrWhiteSpace(line.prerequisiteLineId))
                    continue;

                if (!lineIds.Contains(line.prerequisiteLineId))
                {
                    errors.Add(
                        $"Quest line '{line.lineId}' references missing prerequisite line '{line.prerequisiteLineId}'.");
                }
            }
        }

        public static void ValidateSessionProfile(NetworkSessionProfile profile, List<string> errors, List<string> warnings)
        {
            if (profile == null)
            {
                errors.Add("Network session profile is missing.");
                return;
            }

            if (profile.MaxPlayers != 2)
                warnings.Add("Open-world network session profile is not capped to 2 players.");

            if (profile.GameMode != Fusion.GameMode.Shared)
                warnings.Add("Open-world network session profile is not using Fusion Shared mode.");

            if (string.IsNullOrWhiteSpace(profile.SessionName))
                errors.Add("Network session profile has an empty session name.");

            if (profile.ResolveInitialSceneBuildIndex() < 0)
                errors.Add("Network session profile does not resolve a valid initial scene from Build Settings.");
        }

        public static void ValidateSceneFile(string scenePath, List<string> errors, List<string> warnings, List<string> infos)
        {
            if (!File.Exists(scenePath))
            {
                errors.Add($"Missing scene: {scenePath}");
                return;
            }

            infos.Add($"Scene found: {scenePath}");
            string sceneText = File.ReadAllText(scenePath);
            ValidateSceneText(sceneText, errors, warnings, infos);
            ValidateCommittedPortfolioDemoAssetBindings(scenePath, sceneText, errors);
        }

        public static void ValidateShowcaseDocs(List<string> warnings, List<string> infos)
        {
            bool hasAllRequiredDocs = true;

            for (int i = 0; i < RequiredShowcaseDocs.Length; i++)
            {
                string path = RequiredShowcaseDocs[i];
                if (File.Exists(path))
                {
                    infos.Add($"Showcase doc found: {path}");
                    ValidateShowcaseDocContent(path, warnings, infos);
                    continue;
                }

                warnings.Add($"Missing showcase doc: {path}");
                hasAllRequiredDocs = false;
            }

            for (int i = 0; i < RequiredSupportFiles.Length; i++)
            {
                string path = RequiredSupportFiles[i];
                if (File.Exists(path))
                {
                    infos.Add($"Support file found: {path}");
                    continue;
                }

                warnings.Add($"Missing support file: {path}");
                hasAllRequiredDocs = false;
            }

            ValidateSoloRuntimeRecordPresence(warnings, infos);

            if (hasAllRequiredDocs)
            {
                infos.Add(
                    "Recommended solo-first proof order: run the batchmode guardrail first if needed, then SoloFirstVerification.md, then SoloRuntimeSignoff.md, then use ManualVerificationChecklist.md for the broader shared-session and presentation pass.");
            }
        }

        private static void ValidateSoloRuntimeRecordPresence(List<string> warnings, List<string> infos)
        {
            if (!Directory.Exists(SoloRuntimeRecordDocsRoot))
            {
                warnings.Add($"Missing docs directory for solo runtime records: {SoloRuntimeRecordDocsRoot}");
                return;
            }

            string[] recordPaths = Directory.GetFiles(
                SoloRuntimeRecordDocsRoot,
                "SoloRuntimeSignoff_Record*.md",
                SearchOption.TopDirectoryOnly);

            bool hasDatedRecord = false;
            for (int i = 0; i < recordPaths.Length; i++)
            {
                string fileName = Path.GetFileName(recordPaths[i]);
                if (fileName == SoloRuntimeRecordTemplateFileName)
                    continue;

                hasDatedRecord = true;
                infos.Add($"Solo runtime record found: {Path.GetFileName(recordPaths[i])}");
                ValidateSoloRuntimeRecordContent(recordPaths[i], warnings, infos);
            }

            if (!hasDatedRecord)
            {
                warnings.Add(
                    "No dated solo runtime sign-off record was found. Run SoloRuntimeSignoff.md, then create and save a SoloRuntimeSignoff_Record_YYYY-MM-DD.md file after the live one-player Unity pass.");
            }
        }

        private static void ValidateSoloRuntimeRecordContent(string recordPath, List<string> warnings, List<string> infos)
        {
            string fileName = Path.GetFileName(recordPath);
            string content = File.ReadAllText(recordPath);
            if (string.IsNullOrWhiteSpace(content))
            {
                warnings.Add($"Solo runtime record is empty: {fileName}");
                return;
            }

            for (int i = 0; i < RequiredSoloRuntimeRecordMarkers.Length; i++)
            {
                string marker = RequiredSoloRuntimeRecordMarkers[i];
                if (!content.Contains(marker))
                {
                    warnings.Add(
                        $"Solo runtime record '{fileName}' is missing required marker '{marker}'. " +
                        "The dated sign-off file should capture real one-player runtime evidence, not only its filename.");
                }
            }

            infos.Add($"Solo runtime record content checked: {fileName}");
        }

        private static void ValidateMiniGameConfig(
            string questId,
            int objectiveIndex,
            QuestMiniGameConfigSO config,
            List<string> errors)
        {
            switch (config)
            {
                case LineMatchQuestConfigSO lineMatchConfig when lineMatchConfig.LevelConfig == null:
                    errors.Add($"Quest '{questId}' objective {objectiveIndex} line-match config is missing LevelConfig.");
                    break;
                case LetterOrderingQuestConfigSO letterOrderingConfig when letterOrderingConfig.Data == null:
                    errors.Add($"Quest '{questId}' objective {objectiveIndex} letter-ordering config is missing Data.");
                    break;
                case WordOrderingQuestConfigSO wordOrderingConfig when wordOrderingConfig.Data == null:
                    errors.Add($"Quest '{questId}' objective {objectiveIndex} word-ordering config is missing Data.");
                    break;
            }
        }

        private static void ValidateRequiredLessonMiniGames(HashSet<string> miniGameIds, List<string> errors)
        {
            foreach (string requiredId in RequiredLessonMiniGameObjectiveIds)
            {
                if (!miniGameIds.Contains(requiredId))
                    errors.Add($"Quest registry is missing required MVP lesson mini-game objective '{requiredId}'.");
            }

            foreach (string miniGameId in miniGameIds)
            {
                bool isKnownRequired = false;
                for (int i = 0; i < RequiredLessonMiniGameObjectiveIds.Length; i++)
                {
                    if (miniGameId == RequiredLessonMiniGameObjectiveIds[i])
                    {
                        isKnownRequired = true;
                        break;
                    }
                }

                if (!isKnownRequired)
                {
                    errors.Add(
                        $"Quest registry contains unexpected MVP mini-game objective '{miniGameId}'. " +
                        "Only line_match, letter_ordering, and word_ordering are allowed in the mandatory lesson path.");
                }
            }
        }

        private static void ValidateExpectedLessonMapping(
            string npcId,
            string questId,
            int objectiveIndex,
            string targetId,
            List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return;

            if (!ExpectedNpcMiniGameObjectiveIds.TryGetValue(npcId, out string expectedTargetId))
                return;

            if (targetId != expectedTargetId)
            {
                errors.Add(
                    $"Quest '{questId}' objective {objectiveIndex} uses '{targetId}' for npc '{npcId}', " +
                    $"but the MVP lesson mapping requires '{expectedTargetId}'.");
            }
        }

        private static void ValidateExpectedMiniGameConfigType(
            string questId,
            int objectiveIndex,
            string targetId,
            QuestMiniGameConfigSO config,
            List<string> errors)
        {
            if (config == null)
                return;

            bool isExpectedType = targetId switch
            {
                "line_match" => config is LineMatchQuestConfigSO,
                "letter_ordering" => config is LetterOrderingQuestConfigSO,
                "word_ordering" => config is WordOrderingQuestConfigSO,
                _ => true
            };

            if (!isExpectedType)
            {
                errors.Add(
                    $"Quest '{questId}' objective {objectiveIndex} uses config type '{config.GetType().Name}' for targetId '{targetId}', " +
                    "but the MVP lesson path requires the matching mini-game config type.");
            }
        }

        private static void ValidateRequiredLineStructure(IReadOnlyList<QuestLineSO> lines, List<string> errors)
        {
            var linesById = new Dictionary<string, QuestLineSO>();

            for (int i = 0; i < lines.Count; i++)
            {
                QuestLineSO line = lines[i];
                if (line == null || string.IsNullOrWhiteSpace(line.lineId))
                    continue;

                linesById[line.lineId] = line;
            }

            foreach (KeyValuePair<string, string> expected in ExpectedPrerequisiteByLineId)
            {
                if (!linesById.TryGetValue(expected.Key, out QuestLineSO line))
                {
                    errors.Add($"Quest registry is missing required MVP line '{expected.Key}'.");
                    continue;
                }

                string actualPrerequisite = line.prerequisiteLineId ?? string.Empty;
                string expectedPrerequisite = expected.Value ?? string.Empty;
                if (actualPrerequisite != expectedPrerequisite)
                {
                    errors.Add(
                        $"Quest line '{expected.Key}' has prerequisite '{actualPrerequisite}', " +
                        $"but the MVP chain requires '{expectedPrerequisite}'.");
                }

                if (ExpectedNpcByLineId.TryGetValue(expected.Key, out string expectedNpcId))
                {
                    if (line.npcId != expectedNpcId)
                    {
                        errors.Add(
                            $"Quest line '{expected.Key}' is bound to npc '{line.npcId}', " +
                            $"but the MVP chain requires '{expectedNpcId}'.");
                    }
                }
            }
        }

        private static void ValidateForbiddenDirectQuestPrerequisite(
            QuestLineSO line,
            QuestDefinitionSO definition,
            List<string> errors)
        {
            if (line == null || definition == null || string.IsNullOrWhiteSpace(line.lineId))
                return;

            if (!ForbiddenDirectQuestPrerequisiteByLineId.TryGetValue(line.lineId, out string forbiddenPrerequisite))
                return;

            string actualPrerequisite = definition.prerequisiteQuestId ?? string.Empty;
            string expectedForbiddenValue = forbiddenPrerequisite ?? string.Empty;
            if (actualPrerequisite == expectedForbiddenValue)
                return;

            errors.Add(
                $"Quest '{definition.id}' in MVP line '{line.lineId}' has direct prerequisite quest id '{actualPrerequisite}', " +
                "but the solo-first slice expects line unlocking to be controlled only by the line chain and current quest completion.");
        }

        private static void ValidateNpcBindings(string sceneText, List<string> errors)
        {
            ValidateNpcBinding(sceneText, "NPC - Teacher Ada", "npcId: teacher_ada", "questLine: {fileID: 11400000", errors);
            ValidateNpcBinding(sceneText, "NPC - Coach Ben", "npcId: coach_ben", "questLine: {fileID: 11400000", errors);
            ValidateNpcBinding(sceneText, "NPC - Guide Nora", "npcId: guide_nora", "questLine: {fileID: 11400000", errors);
        }

        private static void ValidateHudBindings(string sceneText, List<string> errors)
        {
            string section = ExtractObjectSection(sceneText, "Portfolio Demo HUD");
            if (string.IsNullOrEmpty(section) || !section.Contains("PortfolioDemoHud"))
                return;

            if (!section.Contains("interactionPrompt: {fileID:") || section.Contains("interactionPrompt: {fileID: 0}"))
                errors.Add("Portfolio Demo HUD is missing its interactionPrompt binding.");

            if (!section.Contains("statusText: {fileID:") || section.Contains("statusText: {fileID: 0}"))
                errors.Add("Portfolio Demo HUD is missing its statusText binding.");

            if (!section.Contains("objectiveEventBus: {fileID:") || section.Contains("objectiveEventBus: {fileID: 0}"))
                errors.Add("Portfolio Demo HUD is missing its QuestObjectiveEventBus binding.");

            if (!section.Contains("dialogueManager: {fileID:") || section.Contains("dialogueManager: {fileID: 0}"))
                errors.Add("Portfolio Demo HUD is missing its DialogueManager binding.");
        }

        private static void ValidateNetworkBindings(string sceneText, List<string> errors)
        {
            string section = ExtractObjectSection(sceneText, "Network");
            if (string.IsNullOrEmpty(section))
            {
                errors.Add("PortfolioDemo scene is missing the Network root object.");
                return;
            }

            if (!section.Contains("GameNetworkManager"))
                errors.Add("Network object is missing GameNetworkManager.");

            if (!section.Contains("EnglishQuestNetworkSceneManager"))
                errors.Add("Network object is missing EnglishQuestNetworkSceneManager.");

            if (!section.Contains("PlayerSpawnCoordinator"))
                errors.Add("Network object is missing PlayerSpawnCoordinator.");

            if (!section.Contains("PortfolioNetworkAutoStart"))
                errors.Add("Network object is missing PortfolioNetworkAutoStart.");

            if (!section.Contains("_openWorldProfile: {fileID:") || section.Contains("_openWorldProfile: {fileID: 0}"))
                errors.Add("GameNetworkManager is missing its open-world NetworkSessionProfile binding.");

            if (!section.Contains("sessionProfile: {fileID:") || section.Contains("sessionProfile: {fileID: 0}"))
                errors.Add("PortfolioNetworkAutoStart is missing its NetworkSessionProfile binding.");

            if (!section.Contains("_defaultPlayerPrefab:") || section.Contains("guid: 00000000000000000000000000000000"))
                errors.Add("PlayerSpawnCoordinator is missing its default player prefab binding.");

            ValidateSpawnPoint(sceneText, "Spawn Point - Player One", errors);
            ValidateSpawnPoint(sceneText, "Spawn Point - Player Two", errors);
        }

        private static void ValidateSpawnPoint(string sceneText, string objectName, List<string> errors)
        {
            string section = ExtractObjectSection(sceneText, objectName);
            if (string.IsNullOrEmpty(section))
            {
                errors.Add($"PortfolioDemo scene is missing {objectName}.");
                return;
            }

            if (!section.Contains("PlayerSpawnPoint"))
                errors.Add($"{objectName} is missing its PlayerSpawnPoint component.");
        }

        private static void ValidateNpcBinding(
            string sceneText,
            string objectName,
            string npcIdMarker,
            string questLineMarker,
            List<string> errors)
        {
            string section = ExtractObjectSection(sceneText, objectName);
            if (string.IsNullOrEmpty(section))
                return;

            if (!section.Contains("NpcQuestGiver"))
                return;

            if (!section.Contains(npcIdMarker))
                errors.Add($"{objectName} has an incorrect or missing npcId binding.");

            if (!section.Contains(questLineMarker))
                errors.Add($"{objectName} is missing its QuestLineSO binding.");
        }

        private static void ValidateMiniGameStationBindings(string sceneText, List<string> errors)
        {
            ValidateMiniGameStation(
                sceneText,
                "Line Match Station",
                "gameId: line_match",
                requiresWordBootstrap: false,
                requiresLineMatchBootstrap: true,
                errors);

            ValidateMiniGameStation(
                sceneText,
                "Letter Ordering Station",
                "gameId: letter_ordering",
                requiresWordBootstrap: true,
                requiresLineMatchBootstrap: false,
                errors);

            ValidateMiniGameStation(
                sceneText,
                "Word Ordering Station",
                "gameId: word_ordering",
                requiresWordBootstrap: true,
                requiresLineMatchBootstrap: false,
                errors);
        }

        private static void ValidateMiniGameStation(
            string sceneText,
            string objectName,
            string expectedGameId,
            bool requiresWordBootstrap,
            bool requiresLineMatchBootstrap,
            List<string> errors)
        {
            string section = ExtractObjectSection(sceneText, objectName);
            if (string.IsNullOrEmpty(section))
                return;

            if (!section.Contains("MiniGameWorldInteractable"))
                return;

            if (!section.Contains(expectedGameId))
                errors.Add($"{objectName} has an incorrect or missing gameId binding.");

            if (!section.Contains("launchHost: {fileID:"))
                errors.Add($"{objectName} is missing its MiniGameWorldLaunchHost reference.");

            if (!section.Contains("fallbackConfig: {fileID: 11400000"))
                errors.Add($"{objectName} is missing its fallback quest mini-game config.");

            if (requiresWordBootstrap && !section.Contains("wordGameBootstrap: {fileID:") || requiresWordBootstrap && section.Contains("wordGameBootstrap: {fileID: 0}"))
                errors.Add($"{objectName} is missing the required WordGameBootstrap binding.");

            if (requiresLineMatchBootstrap && !section.Contains("lineMatchBootstrap: {fileID:") || requiresLineMatchBootstrap && section.Contains("lineMatchBootstrap: {fileID: 0}"))
                errors.Add($"{objectName} is missing the required LineMatch bootstrap binding.");
        }

        private static void ValidateOptionalCoopBindings(string sceneText, List<string> warnings)
        {
            string section = ExtractObjectSection(sceneText, "Optional Co-op Study Circle");
            if (string.IsNullOrEmpty(section))
            {
                warnings.Add(
                    "PortfolioDemo scene is missing the Optional Co-op Study Circle. Solo flow still works, but the multiplayer showcase loses its optional shared world moment.");
                return;
            }

            if (!section.Contains("PortfolioOptionalCoopStudyCircle"))
            {
                warnings.Add(
                    "Optional Co-op Study Circle is missing its PortfolioOptionalCoopStudyCircle component.");
            }
        }

        private static void ValidateCommittedPortfolioDemoAssetBindings(string scenePath, string sceneText, List<string> errors)
        {
            if (!string.Equals(Path.GetFileName(scenePath), "PortfolioDemo.unity", System.StringComparison.OrdinalIgnoreCase))
                return;

            string registrarSection = ExtractObjectSection(sceneText, "Quest Line Registrar");
            if (string.IsNullOrEmpty(registrarSection) || !registrarSection.Contains("QuestLineRegistrar"))
            {
                errors.Add("Quest Line Registrar object is missing its QuestLineRegistrar component.");
            }
            else if (!registrarSection.Contains($"registry: {{fileID: 11400000, guid: {ExpectedMvpRegistryGuid}, type: 2}}"))
            {
                errors.Add("Quest Line Registrar is not bound to the committed QuestLineRegistry_MVP asset.");
            }

            string networkSection = ExtractObjectSection(sceneText, "Network");
            if (string.IsNullOrEmpty(networkSection))
                return;

            if (!networkSection.Contains($"_openWorldProfile: {{fileID: 11400000, guid: {ExpectedOpenWorldNetworkSessionProfileGuid}, type: 2}}"))
                errors.Add("GameNetworkManager is not bound to the committed OpenWorldNetworkSessionProfile MVP asset.");

            if (!networkSection.Contains($"sessionProfile: {{fileID: 11400000, guid: {ExpectedOpenWorldNetworkSessionProfileGuid}, type: 2}}"))
                errors.Add("PortfolioNetworkAutoStart is not bound to the committed OpenWorldNetworkSessionProfile MVP asset.");
        }

        private static void ValidateSoloFirstPresentation(string sceneText, List<string> warnings)
        {
            if (sceneText.Contains("m_Name: Portfolio Demo HUD", System.StringComparison.Ordinal) &&
                !sceneText.Contains("Solo play is fully supported", System.StringComparison.Ordinal) &&
                !sceneText.Contains("Solo completion remains the primary flow", System.StringComparison.Ordinal))
            {
                warnings.Add(
                    "PortfolioDemo scene is missing an explicit solo-first HUD message. The scene may predate the latest portfolio HUD copy, and the demo should still state clearly that one player can complete the slice alone.");
            }

            if (sceneText.Contains("ENGLISH QUEST MVP - OPEN WORLD + QUEST CHAINS + MULTIPLAYER", System.StringComparison.Ordinal) &&
                !sceneText.Contains("ENGLISH QUEST MVP - OPEN WORLD + QUEST CHAINS + OPTIONAL MULTIPLAYER", System.StringComparison.Ordinal))
            {
                warnings.Add(
                    "PortfolioDemo scene still uses the old HUD title. Update the title so multiplayer is presented as an optional showcase layer instead of looking like a required base mode.");
            }

            if (sceneText.Contains("2 Players via Photon Fusion", System.StringComparison.Ordinal) &&
                !sceneText.Contains("Optional 2 Players via Photon Fusion", System.StringComparison.Ordinal))
            {
                warnings.Add(
                    "PortfolioDemo scene still uses the old multiplayer controls label. Update the HUD copy so Photon Fusion presence is presented as optional and does not read like a mission requirement.");
            }

            if (sceneText.Contains("m_Name: Optional Co-op Study Circle", System.StringComparison.Ordinal) &&
                !sceneText.Contains("(Not Required)", System.StringComparison.Ordinal))
            {
                warnings.Add(
                    "Optional Co-op Study Circle is missing its '(Not Required)' world label marker. The scene should communicate visually that this shared beat does not gate progression.");
            }
        }

        private static void ValidateShowcaseDocContent(string path, List<string> warnings, List<string> infos)
        {
            if (!RequiredShowcaseDocMarkers.TryGetValue(path, out string[] requiredMarkers) ||
                requiredMarkers == null ||
                requiredMarkers.Length == 0)
            {
                return;
            }

            string content = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(content))
            {
                warnings.Add($"Showcase doc is empty: {path}");
                return;
            }

            for (int i = 0; i < requiredMarkers.Length; i++)
            {
                string marker = requiredMarkers[i];
                if (content.Contains(marker, System.StringComparison.Ordinal))
                    continue;

                warnings.Add(
                    $"Showcase doc '{path}' is missing solo-first marker '{marker}'. " +
                    "Portfolio docs should keep the main lesson path readable as complete for one player.");
            }

            infos.Add($"Showcase doc content checked: {path}");
        }

        private static string ExtractObjectSection(string sceneText, string objectName)
        {
            string marker = $"m_Name: {objectName}";
            int start = sceneText.IndexOf(marker, System.StringComparison.Ordinal);
            if (start < 0)
                return null;

            int nextObject = sceneText.IndexOf("--- !u!1 &", start + marker.Length, System.StringComparison.Ordinal);
            if (nextObject < 0)
                nextObject = sceneText.Length;

            return sceneText.Substring(start, nextObject - start);
        }
    }
}
