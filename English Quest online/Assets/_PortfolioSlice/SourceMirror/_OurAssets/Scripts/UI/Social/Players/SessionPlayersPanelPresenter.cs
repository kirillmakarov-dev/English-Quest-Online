using Fusion;
using UnityEngine;

public class SessionPlayersPanelPresenter : MonoBehaviour, ISocialPanelModule
{
    [SerializeField] private SessionPlayersPanelView _view;
    [SerializeField] private GuiderPlayerQuestPanel _questPanel;

    private SocialPanelContext _context;
    private PlayerInteraction _localPlayerInteraction;

    public void Bind(SessionPlayersPanelView view)
    {
        _view = view;
        WireViewEvents();
    }

    private void Awake()
    {
        if (_questPanel == null)
            _questPanel = GetComponentInChildren<GuiderPlayerQuestPanel>(true);
    }

    private void OnEnable()
    {
        WireViewEvents();
    }

    private void OnDisable()
    {
        UnwireViewEvents();
    }

    public void Initialize(SocialPanelContext context)
    {
        _context = context;
        WireViewEvents();
    }

    public void Refresh()
    {
        Refresh(rebuildPlayerList: false);
    }

    public void RebuildAndRefresh()
    {
        Refresh(rebuildPlayerList: true);
    }

    public void OnPanelOpened()
    {
        RebuildAndRefresh();
    }

    public void OnPanelClosed()
    {
        _questPanel?.Close();
    }

    public void Dispose()
    {
        UnwireViewEvents();
    }

    private void Refresh(bool rebuildPlayerList)
    {
        if (_view == null || _context?.SessionPlayers == null || _context.Party == null)
            return;

        if (rebuildPlayerList)
            _context.SessionPlayers.Refresh();

        _view.Render(
            _context.SessionPlayers.Players,
            _context.Party.CanInvite,
            ShouldShowQuestButton);
    }

    private bool ShouldShowQuestButton(SessionPlayerInfo player)
    {
        if (player.IsLocal)
            return false;

        if (!SceneNetworkRunner.TryGetForScene(gameObject.scene, out NetworkRunner runner))
            return GuiderService.IsLocalPlayerGuider;

        return GuiderService.IsLocalPlayerGuiderFor(runner);
    }

    private void OnInviteRequested(PlayerRef target)
    {
        Debug.Log(
            $"[SocialPanel] Presenter received invite request for {target}. " +
            $"context={(_context != null)}, invites={(_context?.Invites != null)}",
            this);

        if (_context?.Invites == null)
        {
            Debug.LogWarning("[SocialPanel] Invite ignored — party invite service is not ready.", this);
            return;
        }

        _context.Invites.SendInvite(target);
    }

    private void OnQuestEditorRequested(PlayerRef target, string displayName)
    {
        Debug.Log(
            $"[SocialPanel] Quest editor requested for '{displayName}' ({target}). " +
            $"isGuider={IsLocalGuiderForThisPanel()}, questPanel={(_questPanel != null)}",
            this);

        if (!IsLocalGuiderForThisPanel())
            return;

        if (_questPanel == null)
            _questPanel = GetComponentInChildren<GuiderPlayerQuestPanel>(true);

        if (_questPanel == null)
        {
            Debug.LogWarning(
                "[SocialPanel] Quest editor panel is not wired. Run Tools/English Kingdom/Social/Create Guider Quest Prefabs.",
                this);
            return;
        }

        ResolveLocalPlayerInteraction();
        _questPanel.Open(target, displayName, _localPlayerInteraction);
    }

    private bool IsLocalGuiderForThisPanel()
    {
        if (!SceneNetworkRunner.TryGetForScene(gameObject.scene, out NetworkRunner runner))
            return GuiderService.IsLocalPlayerGuider;

        return GuiderService.IsLocalPlayerGuiderFor(runner);
    }

    private void ResolveLocalPlayerInteraction()
    {
        if (_localPlayerInteraction != null)
            return;

        if (!SceneNetworkRunner.TryGetForScene(gameObject.scene, out NetworkRunner runner))
            return;

        NetworkObject playerObject = runner.GetPlayerObject(runner.LocalPlayer);
        if (playerObject == null)
            return;

        _localPlayerInteraction = PlayerRoot.Resolve<PlayerInteraction>(playerObject);
    }

    private void WireViewEvents()
    {
        if (_view == null)
            return;

        _view.OnInviteRequested -= OnInviteRequested;
        _view.OnQuestEditorRequested -= OnQuestEditorRequested;
        _view.OnInviteRequested += OnInviteRequested;
        _view.OnQuestEditorRequested += OnQuestEditorRequested;
    }

    private void UnwireViewEvents()
    {
        if (_view == null)
            return;

        _view.OnInviteRequested -= OnInviteRequested;
        _view.OnQuestEditorRequested -= OnQuestEditorRequested;
    }
}
