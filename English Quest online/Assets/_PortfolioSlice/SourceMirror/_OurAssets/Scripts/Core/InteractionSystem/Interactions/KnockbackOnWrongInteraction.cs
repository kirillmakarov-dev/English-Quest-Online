using UnityEngine;

/// <summary>
/// Adapter: when the sibling <see cref="InteractionReaction"/> fires its
/// OnWrongSelected event, ask the scene-level <see cref="KnockbackController"/>
/// to push the interacting player backward. No knockback-specific component
/// lives on the player prefab — the controller drives the per-tick simulation
/// from the PreLoad scene.
///
/// Works for both shapes that <see cref="InteractionReaction"/> can take:
///   • Quest-gated WrongDoor (interactable only while a step is active)
///   • Free-fire trap         (interactable unconditionally, anywhere in the world)
///
/// Visual/audio feedback is owned by <see cref="InteractionReaction"/> itself
/// (its _wrongFeedback field). This adapter is concerned only with the physics:
/// translating the wrong-interaction event into a knockback request.
/// </summary>
[RequireComponent(typeof(InteractionReaction))]
public sealed class KnockbackOnWrongInteraction : MonoBehaviour
{
    [Header("Knockback")]
    [Tooltip("Initial horizontal push, in units/second. Direction is always the reverse of the player's forward.")]
    [SerializeField] private float _horizontalForce = 15f;

    [Tooltip("Initial upward lift on impact, in units/second.")]
    [SerializeField] private float _verticalForce = 5f;

    [Tooltip("How quickly the horizontal impulse fades (higher = sharper, shorter knockback). " +
             "Lower this for a long slide, raise it for a snappy punch.")]
    [SerializeField] private float _horizontalDecay = 6f;

    private InteractionReaction _reaction;

    private void Awake()
    {
        _reaction = GetComponent<InteractionReaction>();
    }

    private void OnEnable()
    {
        if (_reaction != null) _reaction.OnWrongSelected += HandleWrongSelected;
    }

    private void OnDisable()
    {
        if (_reaction != null) _reaction.OnWrongSelected -= HandleWrongSelected;
    }

    private void HandleWrongSelected(PlayerInteraction interactor)
    {
        if (interactor == null) return;

        if (KnockbackController.Instance == null)
        {
            AppLog.Warning("[KnockbackOnWrongInteraction] KnockbackController not present in the scene. Add a KnockbackController GameObject to this scene alongside the other scene NetworkBehaviours.", this);
            return;
        }

        Vector3 backward = -interactor.transform.forward;
        KnockbackController.Instance.Apply(interactor, backward, _horizontalForce, _verticalForce, _horizontalDecay);
    }
}
