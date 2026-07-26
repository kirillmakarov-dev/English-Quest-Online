using System.Collections;
using UnityEngine;

/// <summary>
/// Base class for modular actions (e.g., play sound, wait, animate).
/// Inherit from this to create specific gameplay actions.
/// </summary>
public abstract class GameAction : MonoBehaviour
{
    /// <summary>
    /// Executes the action. Returns an IEnumerator so it can be waited on.
    /// </summary>
    public abstract IEnumerator Execute();
}
