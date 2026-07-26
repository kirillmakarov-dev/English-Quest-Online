using UnityEngine;
using Fusion;

/// <summary>
/// DEPRECATED: Please use DialogueLockListener and PlayerLockSystem instead.
/// This script is kept to avoid Missing Script errors until it is removed from prefabs.
/// </summary>
[System.Obsolete("This component is deprecated. Use PlayerLockSystem and DialogueLockListener instead.")]
public class DialogueMovementController : NetworkBehaviour
{
    private void Start()
    {
        AppLog.Warning("DialogueMovementController is deprecated on " + gameObject.name + ". Please remove it and add PlayerLockSystem and DialogueLockListener.");
    }
}
