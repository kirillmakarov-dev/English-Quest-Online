using EnglishKingdom.QuestSystem;
using UnityEngine;

[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.CoreQuestLine + "/Line Authoring Profile", fileName = "QuestLineAuthoringProfile_")]
public class QuestLineAuthoringProfileSO : ScriptableObject
{
    [Tooltip("Optional override for definition asset folder. Defaults to the line asset folder.")]
    public string definitionsFolder;

    [Tooltip("Optional override for quest prefab folder.")]
    public string prefabsFolder;

    public QuestWorldCatalogSetSO worldCatalogSet;
    public QuestCatalogSO questCatalog;
}
