using System.Collections;
using Fusion;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Configuration")]
    public Transform itemHolderPos; // Where objects are held
    [SerializeField] private KeyCode _interactionKey = KeyCode.E;
    [SerializeField] private KeyCode _alternateInteractionKey = KeyCode.I;
    
    // Events mapping to Observer Pattern
    public event System.Action<IInteractable> OnHoverTargetChanged;
    public event System.Action<IInteractable> OnActiveInteractionChanged;

    // State
    public IInteractable CurrentInteractable { get; private set; } // The object we are looking at/touching
    
    public IInteractable ActiveInteraction { get; private set; } // The object we are strictly bound to (like holding item)

    // Helper property for PickableItems to access the hold point
    public Transform HoldPoint => itemHolderPos;

    public string InteractionKeyLabel =>
        _alternateInteractionKey != KeyCode.None
            ? $"{_interactionKey}/{_alternateInteractionKey}"
            : _interactionKey.ToString();

    /// <summary>
    /// True when this local player should read keyboard input.
    /// In Fusion Multi-Peer mode only the focused runner has <see cref="NetworkRunner.ProvideInput"/>.
    /// </summary>
    public bool CanProcessLocalInput => HasNetworkInputAuthority || HasLocalDemoInputFallback;

    private bool HasNetworkInputAuthority =>
        Object != null && Object.IsValid && Object.HasInputAuthority
        && Runner != null && Runner.IsRunning && Runner.ProvideInput;

    // The portfolio scene can use a scene-authored Starter Assets player before Fusion owns it.
    private bool HasLocalDemoInputFallback =>
        GetComponent<NetworkObject>() == null && gameObject.scene.IsValid();

    private Coroutine _reenableNotifyCoroutine;

    public override void Spawned()
    {
        if (itemHolderPos == null)
        {
            PlayerRoot root = PlayerRoot.Get(this);
            if (root != null)
                itemHolderPos = root.transform;
        }
    }

    private void OnEnable()
    {
        // Re-enabled after dialogue lock — refresh next frame so UI controllers run after HUD restore.
        if (_reenableNotifyCoroutine != null)
            StopCoroutine(_reenableNotifyCoroutine);

        _reenableNotifyCoroutine = StartCoroutine(NotifyListenersAfterReenable());
    }

    private void OnDisable()
    {
        if (_reenableNotifyCoroutine != null)
        {
            StopCoroutine(_reenableNotifyCoroutine);
            _reenableNotifyCoroutine = null;
        }
    }

    private IEnumerator NotifyListenersAfterReenable()
    {
        yield return null;
        ValidateInteractableState(notifyEvenIfUnchanged: true);
        _reenableNotifyCoroutine = null;
    }

    private void Update()
    {
        // 1. Input Handling (focused local player only — one runner in Multi-Peer mode)
        if (CanProcessLocalInput && WasInteractionKeyPressed())
            TryInteract();

        // 2. Cleanup: runs after input so same-frame interactions are reflected immediately.
        // Handles cases where OnTriggerExit doesn't fire (e.g., Destroy(object) or CanInteract flipping false).
        ValidateInteractableState(notifyEvenIfUnchanged: false);
    }

    private void ValidateInteractableState(bool notifyEvenIfUnchanged)
    {
        bool changeDetectedHover = false;
        bool changeDetectedActive = false;

        if (CurrentInteractable != null && (IsGoneOrInactive(CurrentInteractable) || !CurrentInteractable.CanInteract))
        {
            CurrentInteractable = null;
            changeDetectedHover = true;
        }

        if (ActiveInteraction != null && (IsGoneOrInactive(ActiveInteraction) || !ActiveInteraction.CanInteract))
        {
            ActiveInteraction = null;
            changeDetectedActive = true;
        }

        if (changeDetectedHover || notifyEvenIfUnchanged)
            OnHoverTargetChanged?.Invoke(CurrentInteractable);

        if (changeDetectedActive || notifyEvenIfUnchanged)
            OnActiveInteractionChanged?.Invoke(ActiveInteraction);
    }

    private void TryInteract()
    {
        // Priority 1: Interact with what we are holding/bound to
        if (IsInteractableAvailable(ActiveInteraction))
        {
            ActiveInteraction.Interact(this);
            return;
        }

        // Priority 2: Interact with what we are looking at
        if (IsInteractableAvailable(CurrentInteractable))
        {
            bool success = CurrentInteractable.Interact(this);
            if (success && !IsInteractableAvailable(CurrentInteractable))
                SetCurrentInteractable(null);
        }
    }

    private bool IsInteractableAvailable(IInteractable interactable)
    {
        return !IsGoneOrInactive(interactable) && interactable.CanInteract;
    }

    private bool WasInteractionKeyPressed()
    {
        if (WasKeyPressed(_interactionKey))
            return true;

        return _alternateInteractionKey != KeyCode.None && WasKeyPressed(_alternateInteractionKey);
    }

    private bool WasKeyPressed(KeyCode keyCode)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && TryGetInputSystemKey(keyCode, out Key inputSystemKey))
            return Keyboard.current[inputSystemKey].wasPressedThisFrame;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(keyCode);
#else
        return false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static bool TryGetInputSystemKey(KeyCode keyCode, out Key inputSystemKey)
    {
        return System.Enum.TryParse(keyCode.ToString(), out inputSystemKey);
    }
#endif

    // Helper to check if a Unity Object underlying an interface has been destroyed or deactivated
    private bool IsGoneOrInactive(IInteractable interactable)
    {
        // If it's pure C# null, it's null.
        if (interactable == null) return true;
        
        // If it's a Unity Object (MonoBehaviour), check if Unity considers it destroyed.
        if (interactable is Object unityObj && unityObj == null) return true;

        // If the GameObject is inactive, treat it the same as destroyed — it can't be interacted with.
        if (interactable is Component comp && !comp.gameObject.activeInHierarchy) return true;
        
        return false;
    }

    // Methods for Interactables to lock/unlock the player

    /// <summary>
    /// Immediately clears any reference to the given interactable from both active and hover state.
    /// Called when a held item is removed from the network (despawned) without going through Drop().
    /// </summary>
    public void ForceReleaseInteractable(IInteractable interactable)
    {
        if (Object != null && !Object.HasInputAuthority) return;

        if (ActiveInteraction == interactable)
        {
            ActiveInteraction = null;
            OnActiveInteractionChanged?.Invoke(null);
        }
        if (CurrentInteractable == interactable)
        {
            CurrentInteractable = null;
            OnHoverTargetChanged?.Invoke(null);
        }
    }

    public void LockInteraction(IInteractable interactable)
    {
        if (Object != null && !Object.HasInputAuthority) return;
        ActiveInteraction = interactable;
        OnActiveInteractionChanged?.Invoke(ActiveInteraction);
    }

    public void UnlockInteraction(IInteractable interactable)
    {
        if (Object != null && !Object.HasInputAuthority) return;
        if (ActiveInteraction == interactable)
        {
            ActiveInteraction = null;
            OnActiveInteractionChanged?.Invoke(ActiveInteraction);
        }
    }

    public void ReleaseAllInteractableBindings()
    {
        if (Object != null && !Object.HasInputAuthority) return;

        if (ActiveInteraction != null)
        {
            ActiveInteraction = null;
            OnActiveInteractionChanged?.Invoke(null);
        }

        if (CurrentInteractable != null)
        {
            CurrentInteractable = null;
            OnHoverTargetChanged?.Invoke(null);
        }
    }

    // Called by the InteractionZoneTrigger
    public void SetCurrentInteractable(IInteractable interactable)
    {
        if (Object != null && !Object.HasInputAuthority) return;
        
        // Prevent redundant calls
        if (CurrentInteractable == interactable) return;

        CurrentInteractable = interactable;
        
        OnHoverTargetChanged?.Invoke(CurrentInteractable);
    }
}
