using System.Collections.Generic;
using UnityEngine;

namespace EnglishQuest.QuestSystem
{
    public class QuestWorldResolver : IQuestWorldResolver
    {
        private readonly NpcCatalogSO _npcCatalog;
        private readonly AreaCatalogSO _areaCatalog;
        private readonly InteractableCatalogSO _interactableCatalog;
        private readonly QuestCatalogSO _questCatalog;
        private readonly HashSet<string> _warnedNpcIds = new();
        private readonly HashSet<string> _warnedAreaIds = new();
        private readonly HashSet<string> _warnedInteractableIds = new();
        private readonly HashSet<string> _warnedQuestIds = new();

        public QuestWorldResolver(QuestWorldCatalogSetSO catalogSet)
        {
            if (catalogSet == null)
                return;

            _npcCatalog = catalogSet.npcCatalog;
            _areaCatalog = catalogSet.areaCatalog;
            _interactableCatalog = catalogSet.interactableCatalog;
            _questCatalog = catalogSet.questCatalog;
        }

        public bool TryGetNpc(string npcId, out NpcCatalogEntry entry)
        {
            entry = null;
            if (_npcCatalog == null || string.IsNullOrEmpty(npcId))
            {
                WarnOnce(_warnedNpcIds, npcId, "NpcCatalog is not assigned.");
                return false;
            }

            if (_npcCatalog.TryGetById(npcId, out entry))
                return true;

            WarnOnce(_warnedNpcIds, npcId, $"Unknown npc id '{npcId}'.");
            return false;
        }

        public bool TryGetArea(string areaId, out AreaCatalogEntry entry)
        {
            entry = null;
            if (_areaCatalog == null || string.IsNullOrEmpty(areaId))
            {
                WarnOnce(_warnedAreaIds, areaId, "AreaCatalog is not assigned.");
                return false;
            }

            if (_areaCatalog.TryGetById(areaId, out entry))
                return true;

            WarnOnce(_warnedAreaIds, areaId, $"Unknown area id '{areaId}'.");
            return false;
        }

        public bool TryGetInteractable(string interactableId, out InteractableCatalogEntry entry)
        {
            entry = null;
            if (_interactableCatalog == null || string.IsNullOrEmpty(interactableId))
            {
                WarnOnce(_warnedInteractableIds, interactableId, "InteractableCatalog is not assigned.");
                return false;
            }

            if (_interactableCatalog.TryGetById(interactableId, out entry))
                return true;

            WarnOnce(_warnedInteractableIds, interactableId, $"Unknown interactable id '{interactableId}'.");
            return false;
        }

        public bool TryGetQuestDefinition(string questId, out QuestDefinitionSO definition)
        {
            definition = null;
            if (_questCatalog == null || string.IsNullOrEmpty(questId))
            {
                WarnOnce(_warnedQuestIds, questId, "QuestCatalog is not assigned.");
                return false;
            }

            if (_questCatalog.TryGetDefinition(questId, out definition))
                return true;

            WarnOnce(_warnedQuestIds, questId, $"Unknown quest id '{questId}'.");
            return false;
        }

        private static void WarnOnce(HashSet<string> warned, string id, string message)
        {
            string key = string.IsNullOrEmpty(id) ? message : id;
            if (warned.Contains(key))
                return;

            warned.Add(key);
            AppLog.Warning($"[QuestWorldResolver] {message}");
        }
    }
}

