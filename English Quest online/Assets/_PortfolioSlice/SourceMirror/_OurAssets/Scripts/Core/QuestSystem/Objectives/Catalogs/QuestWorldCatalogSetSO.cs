using UnityEngine;

namespace EnglishKingdom.QuestSystem
{
    [CreateAssetMenu(menuName = ScriptableObjectMenuPaths.CoreQuestWorldCatalogSet, fileName = "QuestWorldCatalogSet_")]
    public class QuestWorldCatalogSetSO : ScriptableObject
    {
        public NpcCatalogSO npcCatalog;
        public AreaCatalogSO areaCatalog;
        public InteractableCatalogSO interactableCatalog;
        public QuestCatalogSO questCatalog;
    }
}
