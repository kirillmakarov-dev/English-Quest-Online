using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A GameAction that runs other GameActions in sequence.
/// Use this to create complex sequences like "Wait -> Move -> Impulse -> Wait -> Cutscene".
/// </summary>
public class Action_Sequence : GameAction
{
    [Tooltip("Drag GameAction components here in the order you want them to execute.")]
    [SerializeField] private List<GameAction> _actions;

    [ContextMenu("Execute Sequence")]
    public void ExecuteSequence()
    {
        StartCoroutine(Execute());
    }
    public override IEnumerator Execute()
    {
        AppLog.Info("[Action_Sequence] Starting sequence.");
        foreach (var action in _actions)
        {
            if (action != null)
            {
                // Execute and wait for completion
                yield return StartCoroutine(action.Execute());
            }
        }
        AppLog.Info("[Action_Sequence] Sequence Complete.");
    }
}
