using System;
using System.Collections.Generic;
using System.Reflection;
using EnglishKingdom.QuestSystem;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishKingdom.Tests.QuestSystem
{
    /// <summary>
    /// Shared Edit Mode helpers for quest system tests.
    /// </summary>
    internal static class QuestSystemTestSupport
    {
        public static ServiceLocator CreateServiceLocator(GameObject host = null)
        {
            if (host == null)
                host = new GameObject("ServiceLocator (Test)");

            var locator = host.GetComponent<ServiceLocator>();
            if (locator == null)
                locator = host.AddComponent<ServiceLocator>();

            InjectGlobalLocator(locator);
            return locator;
        }

        public static void InjectGlobalLocator(ServiceLocator locator)
        {
            typeof(ServiceLocator)
                .GetField("global", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, locator);
        }

        public static void ClearGlobalLocator()
        {
            InjectGlobalLocator(null);
        }

        public static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (field == null)
                throw new MissingFieldException(target.GetType().FullName, fieldName);

            field.SetValue(target, value);
        }

        public static void InvokeAwake(MonoBehaviour behaviour)
        {
            MethodInfo awake = behaviour.GetType().GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            awake?.Invoke(behaviour, null);
        }

        public static QuestInfo CreateQuestInfo(
            string id,
            QuestState state = QuestState.CAN_START,
            List<QuestStep> steps = null,
            List<QuestRequirement> requirements = null)
        {
            var go = new GameObject(id);
            var quest = go.AddComponent<QuestInfo>();
            quest.id = id;
            quest.displayName = id;
            quest.questSteps = steps ?? new List<QuestStep>();
            quest.requirements = requirements ?? new List<QuestRequirement>();
            quest.SetState(state);
            return quest;
        }

        public static QuestDefinitionSO CreateDefinition(
            string id,
            string npcId,
            int levelRequired = 0,
            string prerequisiteQuestId = null,
            bool waitForNpcTurnIn = false)
        {
            var definition = ScriptableObject.CreateInstance<QuestDefinitionSO>();
            definition.id = id;
            definition.displayName = id;
            definition.description = $"{id} description";
            definition.giverNpcId = npcId;
            definition.levelRequired = levelRequired;
            definition.prerequisiteQuestId = prerequisiteQuestId;
            definition.waitForNpcTurnIn = waitForNpcTurnIn;
            return definition;
        }

        public static GameObject CreateQuestPrefabTemplate()
        {
            var prefabGo = new GameObject("QuestPrefabTemplate");
            var questInfo = prefabGo.AddComponent<QuestInfo>();
            questInfo.questSteps = new List<QuestStep>();
            questInfo.requirements = new List<QuestRequirement>();
            return prefabGo;
        }

        public static LetterOrderingQuestConfigSO CreateLetterOrderingQuestConfig(string gameId)
        {
            var config = ScriptableObject.CreateInstance<LetterOrderingQuestConfigSO>();
            SetPrivateField(config, "gameId", gameId);
            return config;
        }

        public static QuestInfo CreateObjectiveQuestWithDefinition(QuestDefinitionSO definition)
        {
            var go = new GameObject(definition.id);
            var quest = go.AddComponent<QuestInfo>();
            var link = go.AddComponent<QuestDefinitionLink>();
            link.SetDefinition(definition);
            quest.id = definition.id;
            quest.questSteps = new List<QuestStep>();
            return quest;
        }

        public static InteractableCatalogSO CreateInteractableCatalog(params InteractableCatalogEntry[] entries)
        {
            var catalog = ScriptableObject.CreateInstance<InteractableCatalogSO>();
            SetPrivateField(catalog, "entries", new List<InteractableCatalogEntry>(entries));
            return catalog;
        }
    }

    internal sealed class StubQuestService : IQuestService
    {
        public List<QuestInfo> Quests { get; } = new();
        public IReadOnlyList<QuestInfo> AllQuests => Quests;

        public event System.Action<QuestInfo> OnQuestStarted;
        public event System.Action<QuestInfo> OnQuestUpdated;
        public event System.Action<QuestInfo> OnQuestCompleted;
        public event System.Action<QuestInfo> OnQuestStateChanged;
        public event System.Action<QuestObjectiveProgressEvent> OnObjectiveProgressChanged;

        public void ReportObjectiveProgress(QuestInfo questInfo, int stepIndex, int current, int target)
        {
            if (questInfo == null)
                return;

            var progress = new ObjectiveProgress
            {
                Current = current,
                Target = target > 0 ? target : 1,
                Status = current >= target ? QuestStepStatus.COMPLETED : QuestStepStatus.IN_PROGRESS
            };
            questInfo.SetObjectiveProgress(stepIndex, progress);
        }

        public ObjectiveProgress GetObjectiveProgress(QuestInfo questInfo, int stepIndex) =>
            questInfo != null ? questInfo.GetObjectiveProgress(stepIndex) : ObjectiveProgress.NotStarted(1);

        public void CompleteObjectiveStep(QuestInfo questInfo, int stepIndex, string finalState = "")
        {
            if (questInfo == null || questInfo.state != QuestState.IN_PROGRESS)
                return;

            if (stepIndex != questInfo.currentStepIndex)
                return;

            questInfo.StoreQuestStepState(new QuestStepState(finalState, QuestStepStatus.COMPLETED), stepIndex);
            questInfo.MoveToNextStep();

            if (!questInfo.CurrentStepExists())
                questInfo.SetState(QuestState.CAN_FINISH);
        }

        public void StartQuest(QuestInfo questInfo)
        {
            if (questInfo != null && questInfo.state == QuestState.CAN_START)
            {
                questInfo.SetState(QuestState.IN_PROGRESS);
                OnQuestStarted?.Invoke(questInfo);
                OnQuestStateChanged?.Invoke(questInfo);
            }
        }

        public void FinishQuest(QuestInfo questInfo)
        {
            if (questInfo != null && questInfo.state == QuestState.CAN_FINISH)
            {
                questInfo.SetState(QuestState.FINISHED);
                OnQuestCompleted?.Invoke(questInfo);
                OnQuestStateChanged?.Invoke(questInfo);
            }
        }

        public void RegisterQuest(QuestInfo questInfo)
        {
            if (questInfo != null && !Quests.Contains(questInfo))
                Quests.Add(questInfo);
        }

        public void RegisterQuestStep(QuestStep step, QuestInfo questInfo, int stepIndex) { }

        public bool IsQuestCompleted(QuestInfo questInfo) =>
            questInfo != null && questInfo.state == QuestState.FINISHED;

        public QuestInfo GetQuestById(string questId) =>
            Quests.Find(q => q != null && q.id == questId);

        public void ReevaluateQuestRequirements() { }
    }

    internal sealed class StubPlayerLevelProvider : IPlayerLevelProvider
    {
        public int Level = 1;
        public int CurrentLevel => Level;
    }
}
