using UnityEngine;

[AddComponentMenu(QuestSystemComponentMenuPaths.Authoring + "/Quest Definition Link")]
public class QuestDefinitionLink : MonoBehaviour
{
    [SerializeField] private QuestDefinitionSO definition;

    public QuestDefinitionSO Definition => definition;

    public void SetDefinition(QuestDefinitionSO questDefinition) => definition = questDefinition;
}
