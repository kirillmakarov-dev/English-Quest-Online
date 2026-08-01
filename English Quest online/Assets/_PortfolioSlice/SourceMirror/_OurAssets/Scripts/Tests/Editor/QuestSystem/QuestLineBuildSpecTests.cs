using System.Collections.Generic;
using EnglishQuest.QuestSystem;
using NUnit.Framework;
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
    }
}

