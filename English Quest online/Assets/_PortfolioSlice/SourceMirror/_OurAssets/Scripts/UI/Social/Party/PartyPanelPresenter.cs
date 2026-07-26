using Fusion;
using UnityEngine;

public class PartyPanelPresenter : MonoBehaviour, ISocialPanelModule
{
    [SerializeField] private PartyPanelView _view;

    private SocialPanelContext _context;

    public void Bind(PartyPanelView view)
    {
        _view = view;
        WireViewEvents();
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
        RenderPartyPanel();
    }

    public void OnPanelOpened()
    {
        Refresh();
    }

    public void OnPanelClosed() { }

    public void Dispose()
    {
        UnwireViewEvents();
    }

    private void RenderPartyPanel()
    {
        if (_view == null || _context?.Party == null)
            return;

        _view.Render(_context.Party.GetMyParty());
    }

    private void OnLeavePartyPressed()
    {
        _context?.Party?.LeaveParty();
    }

    private void OnKickRequested(PlayerRef target)
    {
        _context?.Party?.Kick(target);
    }

    private void WireViewEvents()
    {
        if (_view == null)
            return;

        _view.OnLeavePartyPressed -= OnLeavePartyPressed;
        _view.OnKickRequested -= OnKickRequested;
        _view.OnLeavePartyPressed += OnLeavePartyPressed;
        _view.OnKickRequested += OnKickRequested;
    }

    private void UnwireViewEvents()
    {
        if (_view == null)
            return;

        _view.OnLeavePartyPressed -= OnLeavePartyPressed;
        _view.OnKickRequested -= OnKickRequested;
    }
}
