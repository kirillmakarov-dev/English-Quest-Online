using System.Collections.Generic;
using EnglishKingdom.QuestSystem;
using UnityEngine;

[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.CoreQuestLine, fileName = "QuestLine_")]
public class QuestLineSO : ScriptableObject
{
    public string lineId;
    public string npcId;
    public string displayName;
    [TextArea] public string theme;
    public List<QuestDefinitionSO> quests = new();

    [Header("Runtime")]
    [Tooltip("Optional world catalog set for this line. Used when QuestLineRegistrySO / registrar has no shared set assigned.")]
    public QuestWorldCatalogSetSO worldCatalogSet;
}
