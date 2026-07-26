using Fusion;
using UnityEngine;

public class GuiderPlayerQuestPanel : GameplayUIBase
{
    [SerializeField] private GuiderPlayerQuestPanelView _view;

    private bool _isOpen;
    private PlayerRef _targetPlayer;
    private string _targetDisplayName;
    private PlayerInteraction _localPlayerInteraction;
    private TeacherTeleport _subscribedTeleport;

    protected override PlayerLockSystem.LockType[] LocksToApply => new[]
    {
        PlayerLockSystem.LockType.Movement,
        PlayerLockSystem.LockType.Camera,
        PlayerLockSystem.LockType.Cursor
    };

    private void Awake()
    {
        if (_view == null)
            _view = GetComponentInChildren<GuiderPlayerQuestPanelView>(true);

        if (_view == null)
            AppLog.Warning("[GuiderPlayerQuestPanel] GuiderPlayerQuestPanelView is not assigned.", this);

        WireViewEvents();
    }

    private void OnEnable()
    {
        WireViewEvents();
        SubscribeNetworkEvents();
    }

    private void OnDisable()
    {
        UnwireViewEvents();
        UnsubscribeNetworkEvents();

        if (_isOpen)
            CloseInternal(endInteraction: true);
    }

    private void Update()
    {
        if (!_isOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void Open(PlayerRef targetPlayer, string displayName, PlayerInteraction localInteractor = null)
    {
        if (!IsLocalGuiderForThisPanel())
        {
            AppLog.Warning("[GuiderPlayerQuestPanel] Open ignored — local player is not the session guider.", this);
            return;
        }

        if (!targetPlayer.IsRealPlayer)
        {
            AppLog.Warning("[GuiderPlayerQuestPanel] Open ignored — target player is invalid.", this);
            return;
        }

        if (_view == null)
            _view = GetComponentInChildren<GuiderPlayerQuestPanelView>(true);

        if (_view == null)
        {
            AppLog.Warning("[GuiderPlayerQuestPanel] Cannot open — view is missing.", this);
            return;
        }

        _localPlayerInteraction = localInteractor;
        _targetPlayer = targetPlayer;
        _targetDisplayName = string.IsNullOrEmpty(displayName) ? targetPlayer.ToString() : displayName;

        if (_isOpen)
            CloseInternal(endInteraction: false);

        // Prefab starts inactive under SocialPanelCanvas — must enable the root,
        // not only the child _panelRoot (inactive parents stay invisible).
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        transform.SetAsLastSibling();

        _isOpen = true;
        SubscribeNetworkEvents();
        BeginInteraction(_localPlayerInteraction);
        _view.Show(_targetDisplayName);
        RequestSnapshot();
    }

    public void Close()
    {
        if (!_isOpen)
            return;

        CloseInternal(endInteraction: true);
    }

    private void CloseInternal(bool endInteraction)
    {
        _isOpen = false;
        _view?.Hide();

        if (endInteraction)
            EndInteraction();

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private void RequestSnapshot()
    {
        if (!TryGetTeleport(out TeacherTeleport teleport))
        {
            _view?.SetStatus("Guider quest service is not available in this scene.");
            return;
        }

        _view?.SetStatus("Loading quests...");
        teleport.RequestQuestSnapshot(_targetPlayer);
    }

    private void HandleSnapshotReceived(GuiderQuestSnapshotReceivedArgs args)
    {
        if (!_isOpen || args.TargetPlayer != _targetPlayer)
            return;

        _view?.RenderEntries(args.Entries, this);
    }

    private void HandleQuestActionRequested(string questId, GuiderQuestAction action)
    {
        if (!_isOpen)
            return;

        if (!TryGetTeleport(out TeacherTeleport teleport))
            return;

        _view?.SetStatus("Applying action...");
        teleport.ApplyQuestAction(_targetPlayer, questId, action);
    }

    private bool IsLocalGuiderForThisPanel()
    {
        if (!SceneNetworkRunner.TryGetForScene(gameObject.scene, out NetworkRunner runner))
            return GuiderService.IsLocalPlayerGuider;

        return GuiderService.IsLocalPlayerGuiderFor(runner);
    }

    private bool TryGetTeleport(out TeacherTeleport teleport)
    {
        if (SceneNetworkRunner.TryGetForScene(gameObject.scene, out NetworkRunner runner)
            && TeacherTeleport.TryGetForRunner(runner, out teleport))
        {
            return true;
        }

        teleport = TeacherTeleport.Instance;
        return teleport != null;
    }

    private void WireViewEvents()
    {
        if (_view == null)
            return;

        _view.OnClosePressed -= Close;
        _view.OnRefreshPressed -= RequestSnapshot;
        _view.OnQuestActionRequested -= HandleQuestActionRequested;
        _view.OnClosePressed += Close;
        _view.OnRefreshPressed += RequestSnapshot;
        _view.OnQuestActionRequested += HandleQuestActionRequested;
    }

    private void UnwireViewEvents()
    {
        if (_view == null)
            return;

        _view.OnClosePressed -= Close;
        _view.OnRefreshPressed -= RequestSnapshot;
        _view.OnQuestActionRequested -= HandleQuestActionRequested;
    }

    private void SubscribeNetworkEvents()
    {
        UnsubscribeNetworkEvents();

        if (!TryGetTeleport(out TeacherTeleport teleport))
            return;

        _subscribedTeleport = teleport;
        _subscribedTeleport.OnGuiderQuestSnapshotReceived += HandleSnapshotReceived;
    }

    private void UnsubscribeNetworkEvents()
    {
        if (_subscribedTeleport == null)
            return;

        _subscribedTeleport.OnGuiderQuestSnapshotReceived -= HandleSnapshotReceived;
        _subscribedTeleport = null;
    }
}
