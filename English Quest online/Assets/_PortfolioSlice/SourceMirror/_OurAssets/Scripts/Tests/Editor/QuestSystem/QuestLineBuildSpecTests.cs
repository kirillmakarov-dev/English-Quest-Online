using System.Collections.Generic;
using EnglishQuest.QuestSystem;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EnglishQuest.Tests.QuestSystem
{
    [TestFixture]
    public class QuestLineBuildSpecTests
    {
        [Test]
        public void BuildSpecEntry_StoresObjectivesAndDialogueRefs()
        {
            var spec = ScriptableObject.CreateInstance<QuestLineBuildSpecSO>();
            spec.lineId = "test_line";
            spec.npcId = "npc_a";
            spec.prerequisiteLineId = "intro_line";

            var startDialogue = ScriptableObject.CreateInstance<DialogueNode>();
            var entry = new QuestBuildEntry
            {
                id = "q01",
                displayName = "Quest One",
                levelRequired = 2,
                xpReward = 100,
                startDialogue = startDialogue,
                objectives = new List<QuestObjectiveDefinition>
                {
                    new QuestObjectiveDefinition
                    {
                        type = QuestObjectiveType.CompleteMiniGame,
                        targetId = "game_a",
                        displayText = "Play"
                    }
                }
            };

            spec.quests = new List<QuestBuildEntry> { entry };

            Assert.AreEqual("test_line", spec.lineId);
            Assert.AreEqual("intro_line", spec.prerequisiteLineId);
            Assert.AreEqual(1, spec.quests.Count);
            Assert.AreSame(startDialogue, spec.quests[0].startDialogue);
            Assert.AreEqual("game_a", spec.quests[0].objectives[0].targetId);
        }

        [Test]
        public void CollectValidationIssues_ReportsDuplicateQuestIdsAndMissingObjectives()
        {
            var spec = ScriptableObject.CreateInstance<QuestLineBuildSpecSO>();
            spec.lineId = "test_line";
            spec.npcId = "npc_a";
            spec.quests = new List<QuestBuildEntry>
            {
                new()
                {
                    id = "q01",
                    objectives = new List<QuestObjectiveDefinition>()
                },
                new()
                {
                    id = "q01",
                    objectives = null
                }
            };

            var errors = new List<string>();
            var warnings = new List<string>();

            spec.CollectValidationIssues(errors, warnings);

            Assert.That(errors, Has.Some.Contains("duplicate quest id 'q01'").IgnoreCase);
            Assert.That(errors, Has.Some.Contains("has no objectives").IgnoreCase);
        }

        [Test]
        public void CollectValidationIssues_WarnsAboutMissingRuntimeBindings()
        {
            var spec = ScriptableObject.CreateInstance<QuestLineBuildSpecSO>();
            spec.lineId = "test_line";
            spec.npcId = "npc_a";
            spec.quests = new List<QuestBuildEntry>
            {
                new()
                {
                    id = "q01",
                    objectives = new List<QuestObjectiveDefinition>
                    {
                        new QuestObjectiveDefinition
                        {
                            type = QuestObjectiveType.EnterArea,
                            targetId = "area_a"
                        }
                    }
                }
            };

            var errors = new List<string>();
            var warnings = new List<string>();

            spec.CollectValidationIssues(errors, warnings);

            Assert.That(warnings, Has.Some.Contains("has no runtime QuestLineSO assigned").IgnoreCase);
            Assert.That(warnings, Has.Some.Contains("has no runtime QuestDefinitionSO assigned").IgnoreCase);
        }

        [Test]
        public void ApplyToRuntimeAssets_SyncsLineDefinitionsAndRegistry()
        {
            string folderName = "__QuestLineBuildSpecTests_" + System.Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folderName);
            string folderPath = "Assets/" + folderName;

            try
            {
                var spec = ScriptableObject.CreateInstance<QuestLineBuildSpecSO>();
                AssetDatabase.CreateAsset(spec, $"{folderPath}/QuestLineBuildSpec_Test.asset");

                spec.lineId = "line_teacher_ada";
                spec.npcId = "teacher_ada";
                spec.displayName = "Teacher Ada";

                spec.profile = ScriptableObject.CreateInstance<QuestLineAuthoringProfileSO>();
                spec.profile.worldCatalogSet = ScriptableObject.CreateInstance<QuestWorldCatalogSetSO>();
                spec.profile.questCatalog = ScriptableObject.CreateInstance<QuestCatalogSO>();

                var registry = ScriptableObject.CreateInstance<QuestLineRegistrySO>();
                AssetDatabase.CreateAsset(registry, $"{folderPath}/QuestLineRegistry_Test.asset");
                spec.registries = new List<QuestLineRegistrySO> { registry };

                spec.quests = new List<QuestBuildEntry>
                {
                    new()
                    {
                        id = "quest_teacher_ada_letters",
                        displayName = "Teacher Ada Letters",
                        description = "Learn the first letters.",
                        levelRequired = 2,
                        waitForNpcTurnIn = true,
                        objectives = new List<QuestObjectiveDefinition>
                        {
                            new QuestObjectiveDefinition
                            {
                                type = QuestObjectiveType.CompleteMiniGame,
                                targetId = "line_match",
                                miniGameConfig = CreateLineMatchConfig("line_match")
                            }
                        }
                    }
                };

                QuestLineBuildSpecAuthoringUtility.ApplyToRuntimeAssets(spec);

                Assert.That(spec.runtimeLine, Is.Not.Null);
                Assert.That(spec.runtimeLine.lineId, Is.EqualTo("line_teacher_ada"));
                Assert.That(spec.runtimeLine.npcId, Is.EqualTo("teacher_ada"));
                Assert.That(spec.runtimeLine.quests, Has.Count.EqualTo(1));
                Assert.That(spec.quests[0].runtimeDefinition, Is.Not.Null);
                Assert.That(spec.quests[0].runtimeDefinition.id, Is.EqualTo("quest_teacher_ada_letters"));
                Assert.That(spec.quests[0].runtimeDefinition.giverNpcId, Is.EqualTo("teacher_ada"));
                Assert.That(spec.quests[0].runtimeDefinition.objectives, Has.Count.EqualTo(1));
                Assert.That(spec.quests[0].runtimeDefinition.objectives[0].targetId, Is.EqualTo("line_match"));
                Assert.That(registry.questLines, Contains.Item(spec.runtimeLine));
            }
            finally
            {
                AssetDatabase.DeleteAsset(folderPath);
            }
        }

        private static LineMatchQuestConfigSO CreateLineMatchConfig(string gameId)
        {
            LineMatchQuestConfigSO config = ScriptableObject.CreateInstance<LineMatchQuestConfigSO>();
            typeof(QuestMiniGameConfigSO)
                .GetField("gameId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(config, gameId);
            typeof(LineMatchQuestConfigSO)
                .GetField("levelConfig", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(
                    config,
                    ScriptableObject.CreateInstance<Puzzle.Gameplay.MiniGames.LetterConnection.LetterConnectionLevelConfigSO>());
            return config;
        }
    }
}

