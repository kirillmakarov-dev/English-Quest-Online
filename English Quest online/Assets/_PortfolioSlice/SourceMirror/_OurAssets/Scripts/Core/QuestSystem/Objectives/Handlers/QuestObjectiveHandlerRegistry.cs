using System.Collections.Generic;

namespace EnglishKingdom.QuestSystem
{
    public class QuestObjectiveHandlerRegistry
    {
        private readonly IQuestWorldResolver _resolver;
        private readonly IQuestService _questService;
        private readonly Dictionary<string, ICustomQuestObjectiveHandler> _customHandlers = new();

        public QuestObjectiveHandlerRegistry(IQuestWorldResolver resolver, IQuestService questService)
        {
            _resolver = resolver;
            _questService = questService;
        }

        public void RegisterCustomHandler(ICustomQuestObjectiveHandler handler)
        {
            if (handler == null || string.IsNullOrEmpty(handler.HandlerId))
                return;

            _customHandlers[handler.HandlerId] = handler;
        }

        public void HandleNpcInteracted(QuestObjectiveEvents.NpcInteracted e)
        {
            if (!_resolver.TryGetNpc(e.NpcId, out _))
                return;

            ForEachActiveObjective(QuestObjectiveType.TalkToNpc, e.NpcId, (quest, def, index) =>
            {
                CompleteObjective(quest, index, def, 1, 1);
            });

            ForEachActiveObjective(QuestObjectiveType.DeliverItem, e.NpcId, (quest, def, index) =>
            {
                // Inventory is intentionally excluded from the portfolio slice.
                // Keep deliver-item steps as no-op placeholders until a new inventory system is built.
                if (def.requiredItemId <= 0)
                    AppLog.Warning($"[QuestObjectiveHandlerRegistry] DeliverItem step {index} on quest '{quest.id}' has no requiredItemId.");

                CompleteObjective(quest, index, def, 1, 1);
            });
        }

        public void HandleAreaEntered(QuestObjectiveEvents.AreaEntered e)
        {
            if (!_resolver.TryGetArea(e.AreaId, out _))
                return;

            ForEachActiveObjective(QuestObjectiveType.EnterArea, e.AreaId, (quest, def, index) =>
            {
                CompleteObjective(quest, index, def, 1, 1);
            });
        }

        public void HandleItemCollected(QuestObjectiveEvents.ItemCollected e)
        {
            if (!_resolver.TryGetInteractable(e.ItemId, out InteractableCatalogEntry entry) ||
                entry.kind != InteractableCatalogKind.Collectible)
                return;

            ForEachActiveObjective(QuestObjectiveType.Collect, e.ItemId, (quest, def, index) =>
            {
                ObjectiveProgress progress = _questService.GetObjectiveProgress(quest, index);
                int target = def.GetTargetCount();
                int next = progress.Current + 1;
                if (next >= target)
                    CompleteObjective(quest, index, def, target, target);
                else
                    _questService.ReportObjectiveProgress(quest, index, next, target);
            });
        }

        public void HandleMiniGameCompleted(QuestObjectiveEvents.MiniGameCompleted e)
        {
            if (!_resolver.TryGetInteractable(e.GameId, out InteractableCatalogEntry entry) ||
                entry.kind != InteractableCatalogKind.MiniGame)
                return;

            ForEachActiveObjective(QuestObjectiveType.CompleteMiniGame, e.GameId, (quest, def, index) =>
            {
                int minScore = def.GetParameterInt("minScore", 0);
                if (minScore > 0 && e.Score < minScore)
                    return;

                CompleteObjective(quest, index, def, 1, 1);
            });
        }

        public void HandleCustomObjective(QuestObjectiveEvents.CustomObjectiveSignaled e)
        {
            if (string.IsNullOrEmpty(e.HandlerId))
                return;

            ForEachActiveObjective(QuestObjectiveType.Custom, e.HandlerId, (quest, def, index) =>
            {
                if (!_customHandlers.TryGetValue(e.HandlerId, out ICustomQuestObjectiveHandler handler))
                {
                    AppLog.Warning($"[QuestObjectiveHandlerRegistry] No custom handler for '{e.HandlerId}'.");
                    return;
                }

                if (handler.TryHandle(quest, def, index, e.Payload))
                    CompleteObjective(quest, index, def, 1, 1);
            });
        }

        private void CompleteObjective(QuestInfo quest, int stepIndex, QuestObjectiveDefinition definition, int current, int target)
        {
            _questService.ReportObjectiveProgress(quest, stepIndex, current, target);
            _questService.CompleteObjectiveStep(quest, stepIndex, definition?.targetId ?? string.Empty);
        }

        private void ForEachActiveObjective(
            QuestObjectiveType type,
            string targetId,
            System.Action<QuestInfo, QuestObjectiveDefinition, int> action)
        {
            if (_questService == null || string.IsNullOrEmpty(targetId))
                return;

            foreach (QuestInfo quest in _questService.AllQuests)
            {
                if (quest == null || !quest.UsesObjectives() || quest.state != QuestState.IN_PROGRESS)
                    continue;

                if (!quest.TryGetObjectiveDefinition(quest.currentStepIndex, out QuestObjectiveDefinition definition))
                    continue;

                if (definition.type != type || definition.targetId != targetId)
                    continue;

                action(quest, definition, quest.currentStepIndex);
            }
        }
    }
}
