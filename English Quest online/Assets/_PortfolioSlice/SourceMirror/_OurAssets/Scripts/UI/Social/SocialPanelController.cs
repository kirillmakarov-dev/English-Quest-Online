using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityServiceLocator;

public class SocialPanelController : GameplayUIBase
{
    [Header("View")]
    [SerializeField] private SocialPanelView _view;

    [Header("Modules")]
    [SerializeField] private SessionPlayersPanelPresenter _playersPresenter;
    [SerializeField] private PartyPanelPresenter _partyPresenter;

    [Header("Input")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.G;

    private readonly List<ISocialPanelModule> _modules = new();
    private bool _isOpen;
    private SocialPanelContext _context;
    private bool _contextSubscribed;
    private PlayerInteraction _localPlayerInteraction;
    private ILocalPlayerReadiness _readiness;

    public bool IsOpen => _isOpen;

    private void Awake()
    {
        if (_view == null)
            _view = GetComponentInChildren<SocialPanelView>(true);

        if (_playersPresenter == null)
            _playersPresenter = GetComponent<SessionPlayersPanelPresenter>();

        if (_partyPresenter == null)
            _partyPresenter = GetComponent<PartyPanelPresenter>();

        WireViewEvents();

        RegisterModule(_playersPresenter);
        RegisterModule(_partyPresenter);
    }

    private void OnEnable()
    {
        if (ServiceLocator.For(this).TryGet(out _readiness))
            _readiness.Ready += HandleLocalPlayerReady;
        ResolveLocalPlayerInteraction();
        WireViewEvents();
        EnsureContext();
    }

    private void OnDisable()
    {
        if (_readiness != null)
        {
            _readiness.Ready -= HandleLocalPlayerReady;
            _readiness = null;
        }

        UnwireViewEvents();

        if (_isOpen)
            Close();
    }

    private void OnDestroy()
    {
        UnsubscribeContext();

        foreach (ISocialPanelModule module in _modules)
            module.Dispose();
    }

    private void Update()
    {
        ResolveLocalPlayerInteraction();

        if (_isOpen && !CanProcessLocalInput())
            Close();

        if (!CanProcessLocalInput())
            return;

        if (!Input.GetKeyDown(_toggleKey))
            return;

        if (_isOpen)
        {
            Close();
            return;
        }

        if (!IsSessionActive())
            return;

        if (IsBlockedByAnotherSystem())
            return;

        Open();
    }

    public void Open()
    {
        if (_isOpen || !EnsureContext())
            return;

        _isOpen = true;
        BeginInteraction(_localPlayerInteraction);
        EnsureCanvasVisible();
        _view?.Show();

        // Populate module content before layout rebuild so first open isn't an empty dark overlay.
        foreach (ISocialPanelModule module in _modules)
            module.OnPanelOpened();

        _view?.PrepareForDisplay();
        RefreshInviteBanner();
        UIDimmer.Instance.Show();
    }

    public void Close()
    {
        if (!_isOpen)
            return;

        _isOpen = false;
        EndInteraction();
        _view?.Hide();

        foreach (ISocialPanelModule module in _modules)
            module.OnPanelClosed();

        UIDimmer.Instance.Hide();
    }

    private void RegisterModule(ISocialPanelModule module)
    {
        if (module == null)
            return;

        _modules.Add(module);
    }

    private bool EnsureContext()
    {
        if (_context != null)
            return true;

        if (!TryResolveSocialServices(out var sessionPlayers, out var party, out var invites))
        {
            AppLog.Warning("[SocialPanelController] Social services are not ready yet.");
            return false;
        }

        _context = new SocialPanelContext(sessionPlayers, party, invites);
        SubscribeContextEvents();

        foreach (ISocialPanelModule module in _modules)
            module.Initialize(_context);

        return true;
    }

    private void SubscribeContextEvents()
    {
        if (_context == null || _contextSubscribed)
            return;

        _context.Invites.OnInviteStateChanged += HandleInviteStateChanged;
        _context.Party.OnPartyChanged += HandlePartyStateChanged;
        _context.SessionPlayers.OnPlayersChanged += HandlePlayersStateChanged;
        _contextSubscribed = true;
    }

    private void HandleInviteStateChanged()
    {
        RefreshInviteBanner();
        RefreshModulesIfOpen(rebuildSessionPlayers: false);
    }

    private void HandlePartyStateChanged()
    {
        RefreshInviteBanner();
        RefreshModulesIfOpen(rebuildSessionPlayers: true);
    }

    private void HandlePlayersStateChanged()
    {
        RefreshModulesIfOpen(rebuildSessionPlayers: false);
    }

    private void RefreshModulesIfOpen(bool rebuildSessionPlayers)
    {
        if (!_isOpen)
            return;

        foreach (ISocialPanelModule module in _modules)
        {
            if (module is SessionPlayersPanelPresenter playersPresenter)
            {
                if (rebuildSessionPlayers)
                    playersPresenter.RebuildAndRefresh();
                else
                    playersPresenter.Refresh();
                continue;
            }

            module.Refresh();
        }
    }

    private bool IsSessionActive()
    {
        if (SceneNetworkRunner.TryGetForScene(gameObject.scene, out NetworkRunner sceneRunner))
            return sceneRunner.IsRunning;

        if (ServiceLocator.Global.TryGet<INetworkSessionService>(out var session))
        {
            NetworkRunner runner = session.Runner;
            if (runner != null && runner.IsRunning)
                return true;
        }

        foreach (NetworkRunner runner in NetworkRunner.Instances)
        {
            if (runner != null && runner.IsRunning)
                return true;
        }

        return false;
    }

    private bool CanProcessLocalInput()
    {
        if (_localPlayerInteraction != null)
            return _localPlayerInteraction.CanProcessLocalInput;

        return SceneNetworkRunner.IsProvidingInputForScene(gameObject.scene);
    }

    private void ResolveLocalPlayerInteraction()
    {
        if (_localPlayerInteraction != null
            && _localPlayerInteraction.gameObject.scene == gameObject.scene)
        {
            return;
        }

        _localPlayerInteraction = null;

        if (!SceneNetworkRunner.TryGetForScene(gameObject.scene, out NetworkRunner runner))
            return;

        NetworkObject playerObject = runner.GetPlayerObject(runner.LocalPlayer);
        if (playerObject == null)
            return;

        _localPlayerInteraction = PlayerRoot.Resolve<PlayerInteraction>(playerObject);
    }

    private void HandleLocalPlayerReady(LocalPlayerReadyArgs args)
    {
        if (args.Scene != gameObject.scene)
            return;

        ResolveLocalPlayerInteraction();
        EnsureContext();
    }

    private bool IsBlockedByAnotherSystem()
    {
        if (_localPlayerInteraction != null)
        {
            if (!ServiceLocator.For(_localPlayerInteraction).TryGet(out IPlayerLockSystem lockSystem))
                return false;
            return GameplayInputGate.IsBlockedByAnother(this, lockSystem);
        }

        if (!ServiceLocator.For(this).TryGet(out IPlayerLockSystem fallbackLockSystem))
            return false;

        return GameplayInputGate.IsBlockedByAnother(this, fallbackLockSystem);
    }

    private void OnAcceptInvitePressed()
    {
        if (_context == null)
            return;

        _context.Invites.AcceptInvite();
    }

    private void OnDeclineInvitePressed()
    {
        if (_context == null)
            return;

        _context.Invites.DeclineInvite();
    }

    private void RefreshInviteBanner()
    {
        if (_view == null || _context == null)
        {
            Debug.Log("[SocialPanel] RefreshInviteBanner skipped — view or context not ready.", this);
            return;
        }

        PartyInvite? invite = _context.Invites.PendingInvite;
        if (invite == null)
        {
            Debug.Log("[SocialPanel] RefreshInviteBanner — no pending invite.", this);
            _view.HideInviteBanner();
            return;
        }

        Debug.Log(
            $"[SocialPanel] RefreshInviteBanner — showing invite from '{invite.Value.FromDisplayName}'. panelOpen={_isOpen}",
            this);
        _view.ShowInviteBanner($"{invite.Value.FromDisplayName} invited you to their party.");
    }

    private void WireViewEvents()
    {
        if (_view == null)
            return;

        _view.OnClosePressed -= Close;
        _view.OnAcceptInvitePressed -= OnAcceptInvitePressed;
        _view.OnDeclineInvitePressed -= OnDeclineInvitePressed;
        _view.OnClosePressed += Close;
        _view.OnAcceptInvitePressed += OnAcceptInvitePressed;
        _view.OnDeclineInvitePressed += OnDeclineInvitePressed;
    }

    private void UnwireViewEvents()
    {
        if (_view == null)
            return;

        _view.OnClosePressed -= Close;
        _view.OnAcceptInvitePressed -= OnAcceptInvitePressed;
        _view.OnDeclineInvitePressed -= OnDeclineInvitePressed;
    }

    private void UnsubscribeContext()
    {
        if (_context == null)
            return;

        if (_contextSubscribed)
        {
            _context.Invites.OnInviteStateChanged -= HandleInviteStateChanged;
            _context.Party.OnPartyChanged -= HandlePartyStateChanged;
            _context.SessionPlayers.OnPlayersChanged -= HandlePlayersStateChanged;
            _contextSubscribed = false;
        }

        _context = null;
    }

    private bool TryResolveSocialServices(
        out ISessionPlayerRegistry sessionPlayers,
        out IPartyService party,
        out IPartyInviteService invites)
    {
        sessionPlayers = null;
        party = null;
        invites = null;

        if (TryResolveRunnerSocialServices(out sessionPlayers, out party, out invites))
            return true;

        return TryResolveSocialServicesFromContext(ServiceLocatorContext, out sessionPlayers, out party, out invites);
    }

    private bool TryResolveRunnerSocialServices(
        out ISessionPlayerRegistry sessionPlayers,
        out IPartyService party,
        out IPartyInviteService invites)
    {
        sessionPlayers = null;
        party = null;
        invites = null;

        if (!SceneNetworkRunner.TryGetForScene(gameObject.scene, out NetworkRunner runner)
            || runner == null
            || !runner.IsRunning)
        {
            return false;
        }

        if (RunnerSocialServiceRegistry.TryGet(runner, out party)
            && RunnerSocialServiceRegistry.TryGet(runner, out invites)
            && RunnerSocialServiceRegistry.TryGet(runner, out sessionPlayers))
        {
            return true;
        }

        MonoBehaviour resolveContext = _localPlayerInteraction != null ? _localPlayerInteraction : this;
        return ServiceLocator.For(resolveContext).TryGet(out sessionPlayers)
               && ServiceLocator.For(resolveContext).TryGet(out party)
               && ServiceLocator.For(resolveContext).TryGet(out invites);
    }

    private static bool TryResolveSocialServicesFromContext(
        MonoBehaviour context,
        out ISessionPlayerRegistry sessionPlayers,
        out IPartyService party,
        out IPartyInviteService invites)
    {
        sessionPlayers = null;
        party = null;
        invites = null;

        if (context == null)
            return false;

        return ServiceLocator.For(context).TryGet(out sessionPlayers)
               && ServiceLocator.For(context).TryGet(out party)
               && ServiceLocator.For(context).TryGet(out invites);
    }

    private void EnsureCanvasVisible()
    {
        Canvas canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        if (!canvas.gameObject.activeSelf)
            canvas.gameObject.SetActive(true);

        if (canvas.transform is RectTransform root && root.localScale == Vector3.zero)
            root.localScale = Vector3.one;

        // Scale-0 canvases skip CanvasScaler sizing; force an immediate pass after unhiding.
        Canvas.ForceUpdateCanvases();
    }
}
