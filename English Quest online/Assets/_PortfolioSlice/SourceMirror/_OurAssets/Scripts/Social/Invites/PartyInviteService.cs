using System;
using Fusion;
using UnityEngine;

public class PartyInviteService : IPartyInviteService
{
    private INetworkSessionService _sessionService;
    private IPartyService _partyService;
    private PartyInvite? _pendingInvite;

    public event Action OnInviteStateChanged;

    public PartyInvite? PendingInvite => _pendingInvite;

    public void Bind(INetworkSessionService sessionService, IPartyService partyService)
    {
        if (_partyService != null)
            _partyService.OnPartyChanged -= HandlePartyChanged;

        _sessionService = sessionService;
        _partyService = partyService;

        if (_partyService != null)
            _partyService.OnPartyChanged += HandlePartyChanged;
    }

    public void TrackMembership(PlayerPartyMembership membership)
    {
        if (membership == null)
            return;

        membership.OnInviteReceived -= HandleInviteReceived;
        membership.OnInviteReceived += HandleInviteReceived;
        membership.OnMembershipChanged -= HandleMembershipChanged;
        membership.OnMembershipChanged += HandleMembershipChanged;
    }

    public void UntrackMembership(PlayerPartyMembership membership)
    {
        if (membership == null)
            return;

        membership.OnInviteReceived -= HandleInviteReceived;
        membership.OnMembershipChanged -= HandleMembershipChanged;
    }

    public void SendInvite(PlayerRef target)
    {
        NetworkRunner runner = _sessionService?.Runner;
        if (runner == null || !runner.IsRunning)
        {
            Debug.LogWarning($"[SocialPanel] SendInvite({target}) aborted — runner not ready.");
            return;
        }

        if (!CanInviteTarget(runner, target, out string blockReason))
        {
            Debug.LogWarning($"[SocialPanel] SendInvite({target}) blocked — {blockReason}");
            AppLog.Warning($"[PartyInviteService] Cannot invite that player right now. {blockReason}");
            return;
        }

        PlayerPartyMembership localMembership = PartyMembershipUtility.GetMembership(runner, runner.LocalPlayer);
        if (localMembership == null)
        {
            Debug.LogWarning($"[SocialPanel] SendInvite({target}) aborted — local party membership not ready.");
            AppLog.Warning("[PartyInviteService] Local party membership is not ready.");
            return;
        }

        Debug.Log($"[SocialPanel] SendInvite({target}) sending via network...");
        localMembership.SendInviteTo(target, runner.LocalPlayer);
        Debug.Log($"[SocialPanel] SendInvite({target}) RPC dispatched.");
    }

    private bool CanInviteTarget(NetworkRunner runner, PlayerRef target, out string blockReason)
    {
        blockReason = null;
        PlayerRef localPlayer = runner.LocalPlayer;
        if (!target.IsRealPlayer || target == localPlayer)
        {
            blockReason = "target is not a remote real player";
            return false;
        }

        PlayerPartyMembership localMembership = PartyMembershipUtility.GetMembership(runner, localPlayer);
        if (localMembership == null)
        {
            blockReason = "local membership missing";
            return false;
        }

        if (!localMembership.IsPartyLeader)
        {
            blockReason = "local player is not party leader";
            return false;
        }

        if (GetPartySizeOnRunner(runner, localPlayer) >= PartyConstants.MaxSize)
        {
            blockReason = "party is full";
            return false;
        }

        PlayerPartyMembership targetMembership = PartyMembershipUtility.GetMembership(runner, target);
        if (targetMembership == null)
        {
            blockReason = "target membership missing";
            return false;
        }

        if (!targetMembership.IsSolo)
        {
            blockReason = "target is already in a party";
            return false;
        }

        if (targetMembership.PartyLeader == localPlayer)
        {
            blockReason = "target is already in your party";
            return false;
        }

        return true;
    }

    private static int GetPartySizeOnRunner(NetworkRunner runner, PlayerRef leaderRef)
    {
        int count = 0;
        foreach (PlayerRef player in FusionCoSessionRunners.EnumeratePlayers(runner))
        {
            PlayerPartyMembership membership = PartyMembershipUtility.GetMembership(runner, player);
            if (membership != null && membership.PartyLeader == leaderRef)
                count++;
        }

        return count;
    }

    public void AcceptInvite()
    {
        if (_pendingInvite == null)
            return;

        NetworkRunner runner = _sessionService?.Runner;
        if (runner == null || !runner.IsRunning)
            return;

        PlayerRef leaderRef = _pendingInvite.Value.From;
        PlayerPartyMembership localMembership = PartyMembershipUtility.GetMembership(runner, runner.LocalPlayer);
        if (localMembership == null)
            return;

        int partySize = _partyService.GetPartySize(leaderRef);
        if (!localMembership.TryJoinParty(leaderRef, partySize))
        {
            AppLog.Warning("[PartyInviteService] Failed to join party.");
            ClearPendingInvite();
            return;
        }

        ClearPendingInvite();
        _partyService.NotifyChanged();
    }

    public void DeclineInvite()
    {
        ClearPendingInvite();
    }

    private void HandleInviteReceived(PlayerRef fromPlayer)
    {
        NetworkRunner runner = _sessionService?.Runner;
        if (runner == null || !runner.IsRunning)
        {
            Debug.LogWarning($"[SocialPanel] HandleInviteReceived({fromPlayer}) aborted — runner not ready.");
            return;
        }

        PlayerPartyMembership localMembership = PartyMembershipUtility.GetMembership(runner, runner.LocalPlayer);
        if (localMembership == null)
        {
            Debug.LogWarning($"[SocialPanel] HandleInviteReceived({fromPlayer}) aborted — local membership missing.");
            return;
        }

        if (!localMembership.IsSolo)
        {
            ClearPendingInvite();
            Debug.LogWarning($"[SocialPanel] HandleInviteReceived({fromPlayer}) ignored — local player is not solo.");
            return;
        }

        if (_partyService.GetPartySize(fromPlayer) >= PartyConstants.MaxSize)
        {
            Debug.LogWarning($"[SocialPanel] HandleInviteReceived({fromPlayer}) ignored — inviter party is full.");
            return;
        }

        string displayName = PartyMembershipUtility.GetDisplayName(runner, fromPlayer);
        _pendingInvite = new PartyInvite(fromPlayer, displayName);

        Debug.Log(
            $"[SocialPanel] Pending invite stored from '{displayName}' ({fromPlayer}). " +
            $"listeners={(OnInviteStateChanged != null ? OnInviteStateChanged.GetInvocationList().Length : 0)}");

        OnInviteStateChanged?.Invoke();
    }

    private void HandleMembershipChanged()
    {
        NetworkRunner runner = _sessionService?.Runner;
        if (runner == null || !runner.IsRunning)
            return;

        if (_partyService != null && _partyService.GetPartySize(runner.LocalPlayer) > 1)
            ClearPendingInvite();
    }

    private void HandlePartyChanged()
    {
        NetworkRunner runner = _sessionService?.Runner;
        if (runner == null || !runner.IsRunning || _partyService == null)
            return;

        if (_partyService.GetPartySize(runner.LocalPlayer) > 1)
            ClearPendingInvite();
    }

    private void ClearPendingInvite()
    {
        if (_pendingInvite == null)
            return;

        _pendingInvite = null;
        OnInviteStateChanged?.Invoke();
    }
}
