using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A "wrong interaction" trigger. Interactable either while a linked QuestStep is
/// active (gated, e.g. the WrongDoor of a room) or unconditionally (free, e.g. a
/// trap object that always punishes interaction).
///
/// Fires OnWrongSelected so sibling components (knockback adapter, score, sfx, …)
/// can react without this class knowing about them. Has no knowledge of sequence,
/// neighbors, or quest structure beyond the optional <c>_roomStep</c> reference.
/// </summary>
public class InteractionReaction : MonoBehaviour, IInteractable
{
    [SerializeField] private string _answerText;

    [Tooltip("Optional. If set, this object is interactable ONLY while the given step is active. " +
             "Leave null for a free-fire trap that is always interactable.")]
    [SerializeField] private QuestStep _gatingStep;

    [SerializeField] private UnityEvent _Reaction;

    /// <summary>
    /// Raised when the player commits an interaction with this object.
    /// Open-ended hook so sibling components (knockback, score, sfx, UI rumble, …)
    /// can react without this class needing to know about them.
    /// Carries the PlayerInteraction that triggered the interaction.
    /// </summary>
    public event Action<PlayerInteraction> OnWrongSelected;

    public string InteractionPrompt => _answerText;

    // If no step is linked, the object behaves as a free-fire trap (always interactable).
    // If a step is linked, the object is only interactable while that step is active.
    public bool CanInteract => _gatingStep == null || _gatingStep.StepIsActive;

    public bool Interact(PlayerInteraction interactor)
    {
        if (!CanInteract) return false;

        AppLog.Info($"[WrongInteraction] '{name}' — \"{_answerText}\"");
        _Reaction?.Invoke();
        OnWrongSelected?.Invoke(interactor);
        return true;
    }
}
