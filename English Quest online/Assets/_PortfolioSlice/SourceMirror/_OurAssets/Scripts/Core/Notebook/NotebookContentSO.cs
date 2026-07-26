using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NotebookContentSO", menuName = ScriptableObjectMenuPaths.GameplayNotebook + "/Notebook Content")]
public class NotebookContentSO : ScriptableObject
{
    public List<NotebookTopic> notebookTopics;

}
