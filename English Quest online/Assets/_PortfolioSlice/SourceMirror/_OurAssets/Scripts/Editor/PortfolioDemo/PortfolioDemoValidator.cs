using System.Collections.Generic;
using System.IO;
using EnglishQuest.QuestSystem;
using UnityEditor;
using UnityEngine;

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
            ValidateSessionProfile(errors, warnings);

            EmitMessages(errors, warnings, infos);

            string summary =
                $"Validation finished.\n\nErrors: {errors.Count}\nWarnings: {warnings.Count}\nInfo: {infos.Count}";
            EditorUtility.DisplayDialog("Validate Portfolio Demo", summary, "OK");
        }

        private static void ValidateSceneFile(List<string> errors, List<string> warnings, List<string> infos)
        {
            if (!File.Exists(ScenePath))
            {
                errors.Add($"Missing scene: {ScenePath}");
                return;
            }

            infos.Add($"Scene found: {ScenePath}");
            string sceneText = File.ReadAllText(ScenePath);

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
        }

        private static void ValidateQuestRegistry(List<string> errors, List<string> warnings, List<string> infos)
        {
            QuestLineRegistrySO registry = AssetDatabase.LoadAssetAtPath<QuestLineRegistrySO>(RegistryPath);
            if (registry == null)
            {
                errors.Add($"Missing quest registry asset: {RegistryPath}");
                return;
            }

            infos.Add($"Quest registry found: {RegistryPath}");

            if (registry.questLines == null || registry.questLines.Count != 3)
                errors.Add("Quest registry must contain exactly 3 quest lines for the MVP slice.");

            var lineIds = new HashSet<string>();
            var questIds = new HashSet<string>();

            foreach (QuestLineSO line in registry.questLines)
            {
                if (line == null)
                {
                    errors.Add("Quest registry contains a null quest line reference.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line.lineId))
                    errors.Add($"Quest line '{line.name}' has an empty lineId.");
                else if (!lineIds.Add(line.lineId))
                    errors.Add($"Duplicate quest line id: {line.lineId}");

                if (string.IsNullOrWhiteSpace(line.npcId))
                    errors.Add($"Quest line '{line.name}' has an empty npcId.");

                if (line.quests == null || line.quests.Count == 0)
                    errors.Add($"Quest line '{line.name}' has no quests assigned.");

                if (line.quests != null)
                {
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

                        if (definition.startDialogue == null)
                            warnings.Add($"Quest '{definition.id}' has no start dialogue.");

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

                            if (objective.type == QuestObjectiveType.CompleteMiniGame)
                            {
                                if (string.IsNullOrWhiteSpace(objective.targetId))
                                    errors.Add($"Quest '{definition.id}' objective {i} has an empty mini-game targetId.");

                                if (objective.miniGameConfig == null)
                                    errors.Add($"Quest '{definition.id}' objective {i} is missing QuestMiniGameConfigSO.");
                                else if (objective.miniGameConfig.GameId != objective.targetId)
                                    errors.Add(
                                        $"Quest '{definition.id}' objective {i} targetId '{objective.targetId}' " +
                                        $"does not match config GameId '{objective.miniGameConfig.GameId}'.");
                            }
                        }
                    }
                }
            }

            foreach (QuestLineSO line in registry.questLines)
            {
                if (line == null || string.IsNullOrWhiteSpace(line.prerequisiteLineId))
                    continue;

                if (!lineIds.Contains(line.prerequisiteLineId))
                    errors.Add(
                        $"Quest line '{line.lineId}' references missing prerequisite line '{line.prerequisiteLineId}'.");
            }
        }

        private static void ValidateSessionProfile(List<string> errors, List<string> warnings)
        {
            NetworkSessionProfile profile = AssetDatabase.LoadAssetAtPath<NetworkSessionProfile>(SessionProfilePath);
            if (profile == null)
            {
                errors.Add($"Missing network session profile: {SessionProfilePath}");
                return;
            }

            if (profile.MaxPlayers != 2)
                warnings.Add("Open-world network session profile is not capped to 2 players.");

            if (profile.ResolveInitialSceneBuildIndex() < 0)
                errors.Add("Network session profile does not resolve a valid initial scene from Build Settings.");
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
}
