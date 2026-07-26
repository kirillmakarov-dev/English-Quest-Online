/// <summary>
/// Canonical Create > menu paths for project ScriptableObjects.
/// Use these constants in [CreateAssetMenu(menuName = ...)] attributes.
/// </summary>
public static class ScriptableObjectMenuPaths
{
    public const string Root = "English Kingdom";

    public const string Dev = Root + "/Dev";

    public const string CoreWords = Root + "/Core/Words";
    public const string CoreAudio = Root + "/Core/Audio";
    public const string CoreCombat = Root + "/Core/Combat";
    public const string CoreCombatAttackShapes = CoreCombat + "/Attack Shapes";
    public const string CoreAbilities = Root + "/Core/Abilities";
    public const string CoreRewards = Root + "/Core/Rewards";
    public const string CoreProgression = Root + "/Core/Progression";
    public const string CoreQuest = Root + "/Core/Quest";
    public const string CoreQuestDefinition = CoreQuest + "/Quest Definition";
    public const string CoreQuestLine = CoreQuest + "/Quest Line";
    public const string CoreQuestLineRegistry = CoreQuest + "/Quest Line Registry";
    public const string CoreQuestLineBuildSpec = CoreQuest + "/Line Build Spec";
    public const string CoreQuestCatalog = CoreQuest + "/Quest Catalog";
    public const string CoreQuestNpcCatalog = CoreQuest + "/NPC Catalog";
    public const string CoreQuestAreaCatalog = CoreQuest + "/Area Catalog";
    public const string CoreQuestInteractableCatalog = CoreQuest + "/Interactable Catalog";
    public const string CoreQuestWorldCatalogSet = CoreQuest + "/World Catalog Set";
    public const string CoreQuestMiniGameConfig = CoreQuest + "/Mini Game Config";
    public const string CoreStats = Root + "/Core/Stats";
    public const string CoreLipSync = Root + "/Core/Lip Sync";
    public const string CoreDialogue = Root + "/Core/Dialogue";
    public const string CoreNetworking = Root + "/Core/Networking";

    public const string GameplayAnimals = Root + "/Gameplay/Animals";
    public const string GameplayAnimalsMovement = GameplayAnimals + "/Movement";
    public const string GameplayMonsters = Root + "/Gameplay/Monsters";
    public const string GameplayMonstersSpawnPool = GameplayMonsters + "/Spawn Pool";
    public const string GameplayDayNight = Root + "/Gameplay/Day Night";
    public const string GameplayNotebook = Root + "/Gameplay/Notebook";
    public const string GameplayWorldTravel = Root + "/Gameplay/World Travel";

    public const string MiniGamesWordOrdering = Root + "/MiniGames/Word Ordering";
    public const string MiniGamesLetterOrdering = Root + "/MiniGames/Letter Ordering";

    public const string FeaturesWordReveal = Root + "/Features/Word Reveal";

    public const string UI = Root + "/UI";
}
