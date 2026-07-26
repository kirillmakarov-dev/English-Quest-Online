using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Shared logic for all lever types: cooldown, animation, and IInteractable.
/// Subclasses define what happens on activation via HandleActivation().
/// </summary>
public abstract class LeverInteractableBase : MonoBehaviour, IInteractable
{
    [SerializeField] protected Animator _animator;

    private bool _isCoolingDown;

    public bool IsCoolingDown => _isCoolingDown;
    public string InteractionPrompt => "Pull Lever";
    public bool CanInteract => !_isCoolingDown;

    /// <summary>Raised after every activation. Used by NetworkedLeverInteractable.</summary>
    public event Action Activated;

    public bool Interact(PlayerInteraction interactor)
    {
        if (!CanInteract) return false;

        Activate();
        return true;
    }

    public void Activate()
    {
        HandleActivation();
        PlayAnimation();
        Activated?.Invoke();
    }

    protected abstract void HandleActivation();

    public void PlayAnimation()
    {
        _animator.Play("LeverPull", 0, 0f);
        StartCoroutine(ResetCooldownAfterAnimation());
    }

    private IEnumerator ResetCooldownAfterAnimation()
    {
        _isCoolingDown = true;
        yield return null;
        yield return new WaitUntil(() => _animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f);
        _isCoolingDown = false;
    }
}
