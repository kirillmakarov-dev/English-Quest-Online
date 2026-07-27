namespace EnglishQuest.QuestSystem
{
    public interface IQuestWorldResolver
    {
        bool TryGetNpc(string npcId, out NpcCatalogEntry entry);
        bool TryGetArea(string areaId, out AreaCatalogEntry entry);
        bool TryGetInteractable(string interactableId, out InteractableCatalogEntry entry);
        bool TryGetQuestDefinition(string questId, out QuestDefinitionSO definition);
    }
}

