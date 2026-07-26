using System;
using UnityEngine;

public interface IDialogueService
{
    event Action OnDialogueStart;
    event Action OnDialogueEnd;

    /// <summary>True while a dialogue session is open.</summary>
    bool IsDialogueActive { get; }

    void StartDialogue(
        DialogueNode startNode,
        Transform speaker = null,
        Transform localPlayer = null,
        Sprite speakerSprite = null,
        string speakerName = "");
}
