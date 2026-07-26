using EnglishKingdom.PortfolioDemo;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInteractionController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerInteraction _playerInteraction;
    
    [Header("UI")]
    [SerializeField] private InteractionUI _interactionUI;
    [SerializeField] private PortfolioDemoHud _portfolioHud;
    private IInteractionUI _ui;

    private IInteractable _lastHoveredInteractable;
    private bool _wasProcessingLocalInput;

    /// <summary>Inject a UI implementation — for testing only.</summary>
    public void SetInteractionUIForTest(IInteractionUI ui) => _ui = ui;

    private bool CanProcessLocalInput =>
        _playerInteraction != null &&
        (_playerInteraction.CanProcessLocalInput || _playerInteraction.GetComponent<NetworkObject>() == null);

    private void OnEnable()
    {
        if (_playerInteraction == null) _playerInteraction = GetComponentInParent<PlayerInteraction>();
        ResolveInteractionUI();
        ResolvePortfolioHud();
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
        if (_playerInteraction == null)
            return;

        if (!CanProcessLocalInput)
        {
            _ui?.Hide();
            _portfolioHud?.SetInteractionPrompt(string.Empty);
            return;
        }

        var active = _playerInteraction.ActiveInteraction;
        var current = _playerInteraction.CurrentInteractable;
        string prompt = null;

        // Show UI for Active Interaction (Lock) first, then falling back to current hovered
        if (active != null && active.CanInteract)
        {
            prompt = active.InteractionPrompt;
        }
        else if (current != null && current.CanInteract)
        {
            prompt = current.InteractionPrompt;
        }

        if (string.IsNullOrEmpty(prompt))
        {
            _ui?.Hide();
            _portfolioHud?.SetInteractionPrompt(string.Empty);
            return;
        }

        _ui?.Show(prompt);
        _portfolioHud?.SetInteractionPrompt($"{_playerInteraction.InteractionKeyLabel}  {prompt}");
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

    private void ResolvePortfolioHud()
    {
        if (_portfolioHud != null
            && _portfolioHud.gameObject.scene == gameObject.scene)
        {
            return;
        }

        _portfolioHud = FindPortfolioHudInScene(gameObject.scene);
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

    private static PortfolioDemoHud FindPortfolioHudInScene(Scene scene)
    {
        if (!scene.IsValid())
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            PortfolioDemoHud hud = roots[i].GetComponentInChildren<PortfolioDemoHud>(true);
            if (hud != null)
                return hud;
        }

        return null;
    }
}
