using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInteractionController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerInteraction _playerInteraction;
    
    [Header("UI")]
    [SerializeField] private InteractionUI _interactionUI;
    private IInteractionUI _ui;

    private IInteractable _lastHoveredInteractable;
    private bool _wasProcessingLocalInput;

    /// <summary>Inject a UI implementation — for testing only.</summary>
    public void SetInteractionUIForTest(IInteractionUI ui) => _ui = ui;

    private bool CanProcessLocalInput =>
        _playerInteraction != null && _playerInteraction.CanProcessLocalInput;

    private void OnEnable()
    {
        if (_playerInteraction == null) _playerInteraction = GetComponentInParent<PlayerInteraction>();
        ResolveInteractionUI();
        _ui ??= _interactionUI;  // use injected stub in tests, otherwise the serialized component
        _wasProcessingLocalInput = CanProcessLocalInput;

        if (_playerInteraction != null)
        {
            _playerInteraction.OnHoverTargetChanged       += HandleHoverTargetChanged;
            _playerInteraction.OnActiveInteractionChanged += HandleActiveInteractionChanged;
        }

        // Refresh UI immediately — player may have re-activated after dismounting the bike.
        UpdateUI();
    }

    private void Update()
    {
        if (_playerInteraction == null)
            return;

        bool processing = CanProcessLocalInput;
        if (processing == _wasProcessingLocalInput)
            return;

        _wasProcessingLocalInput = processing;

        UpdateUI();
    }

    private void OnDisable()
    {
        if (_playerInteraction != null)
        {
            _playerInteraction.OnHoverTargetChanged       -= HandleHoverTargetChanged;
            _playerInteraction.OnActiveInteractionChanged -= HandleActiveInteractionChanged;
        }
        _lastHoveredInteractable = null;
        _ui?.Hide();
    }

    private void HandleHoverTargetChanged(IInteractable newTarget)
    {
        _lastHoveredInteractable = newTarget;

        UpdateUI();
    }

    private void HandleActiveInteractionChanged(IInteractable activeInteraction)
    {
        UpdateUI();
    }

    private static bool IsAliveInteractable(IInteractable interactable)
    {
        return interactable is Component comp && comp != null;
    }

    private void UpdateUI()
    {
        if (_ui == null || _playerInteraction == null)
            return;

        if (!CanProcessLocalInput)
        {
            _ui.Hide();
            return;
        }

        var active = _playerInteraction.ActiveInteraction;
        var current = _playerInteraction.CurrentInteractable;

        // Show UI for Active Interaction (Lock) first, then falling back to current hovered
        if (active != null && active.CanInteract)
        {
            _ui.Show(active.InteractionPrompt);
        }
        else if (current != null && current.CanInteract)
        {
            _ui.Show(current.InteractionPrompt);
        }
        else
        {
            _ui.Hide();
        }
    }

    private void ResolveInteractionUI()
    {
        if (_interactionUI != null
            && _interactionUI.gameObject.scene == gameObject.scene)
        {
            return;
        }

        _interactionUI = FindInteractionUIInScene(gameObject.scene);
    }

    private static InteractionUI FindInteractionUIInScene(Scene scene)
    {
        if (!scene.IsValid())
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            InteractionUI ui = roots[i].GetComponentInChildren<InteractionUI>(true);
            if (ui != null)
                return ui;
        }

        return null;
    }
}
