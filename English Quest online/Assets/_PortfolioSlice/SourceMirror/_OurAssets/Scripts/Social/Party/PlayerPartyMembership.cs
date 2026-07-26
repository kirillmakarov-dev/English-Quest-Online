using System;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityServiceLocator;

public class PlayerPartyMembership : NetworkBehaviour
{
    public event Action OnMembershipChanged;
    public event Action<PlayerRef> OnInviteReceived;

    [Networked, OnChangedRender(nameof(OnPartyLeaderChanged))]
    public PlayerRef PartyLeader { get; set; }

    public bool IsSolo => PartyLeader == Object.InputAuthority;
    public bool IsPartyLeader => PartyLeader == Object.InputAuthority;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
            PartyLeader = Object.InputAuthority;
    }

    public void LeaveParty()
    {
        if (!Object.HasStateAuthority)
            return;

        PartyLeader = Object.InputAuthority;
    }

    public void HandleLeaderDisconnected(PlayerRef disconnectedLeader)
    {
        if (!Object.HasStateAuthority)
            return;

        if (PartyLeader == disconnectedLeader && !IsPartyLeader)
            PartyLeader = Object.InputAuthority;
    }

    public void SendInviteTo(PlayerRef targetPlayer, PlayerRef fromPlayer)
    {
        if (Runner == null || !Runner.IsRunning || !targetPlayer.IsRealPlayer)
            return;

        NetworkObject targetObject = Runner.GetPlayerObject(targetPlayer);
        if (targetObject == null)
        {
            AppLog.Warning($"[PlayerPartyMembership] Could not find player object for invite target {targetPlayer}.");
            return;
        }

        PlayerPartyMembership targetMembership = targetObject.GetComponent<PlayerPartyMembership>();
        if (targetMembership == null)
        {
            AppLog.Warning($"[PlayerPartyMembership] Could not find membership for invite target {targetPlayer}.");
            return;
        }

        targetMembership.RpcReceiveInvite(fromPlayer);
    }

    public void RequestKick(PlayerRef targetPlayer, PlayerRef leaderPlayer)
    {
        if (Runner == null || !Runner.IsRunning || !targetPlayer.IsRealPlayer)
            return;

        NetworkObject targetObject = Runner.GetPlayerObject(targetPlayer);
        PlayerPartyMembership targetMembership = targetObject != null
            ? targetObject.GetComponent<PlayerPartyMembership>()
            : null;
        targetMembership?.RpcKickFromParty(leaderPlayer);
    }

    public void BroadcastPartyTravelStarted(
        string mapId,
        string fromNodeId,
        string toNodeId,
        int presentationEpoch,
        string sessionName,
        WorldTravelSessionMode sessionMode)
    {
        if (!Object.HasStateAuthority || !IsPartyLeader)
            return;

        RpcPartyTravelStarted(
            mapId,
            fromNodeId,
            toNodeId,
            PartyLeader,
            presentationEpoch,
            sessionName,
            sessionMode);
    }

    public void RequestTravelSceneLoad(int buildIndex)
    {
        if (Object?.Runner == null)
            return;

        NetworkSessionSceneLoadRequest.TryRequest(Object.Runner, buildIndex);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcPartyTravelStarted(
        string mapId,
        string fromNodeId,
        string toNodeId,
        PlayerRef leaderRef,
        int presentationEpoch,
        string sessionName,
        WorldTravelSessionMode sessionMode,
        RpcInfo info = default)
    {
        if (!Object.HasInputAuthority)
            return;

        if (!leaderRef.IsRealPlayer || PartyLeader != leaderRef)
            return;

        if (IsPartyLeader)
            return;

        PlayerInteraction interaction = PlayerRoot.Resolve<PlayerInteraction>(Object);
        if (interaction == null)
        {
            AppLog.Warning("[PlayerPartyMembership] Could not resolve PlayerInteraction for party travel follow.");
            return;
        }

        if (!ServiceLocator.For(interaction).TryGet(out IWorldTravelService travelService))
        {
            AppLog.Warning("[PlayerPartyMembership] IWorldTravelService not available for party travel follow.");
            return;
        }

        HandlePartyTravelFollowAsync(
                travelService,
                mapId,
                fromNodeId,
                toNodeId,
                interaction,
                sessionName,
                sessionMode,
                presentationEpoch)
            .Forget();
    }

    private async UniTaskVoid HandlePartyTravelFollowAsync(
        IWorldTravelService travelService,
        string mapId,
        string fromNodeId,
        string toNodeId,
        PlayerInteraction interaction,
        string sessionName,
        WorldTravelSessionMode sessionMode,
        int presentationEpoch)
    {
        try
        {
            await travelService.FollowPartyTravelAsync(
                mapId,
                fromNodeId,
                toNodeId,
                interaction,
                sessionName,
                sessionMode,
                presentationEpoch);
        }
        catch (Exception ex)
        {
            AppLog.Error($"[PlayerPartyMembership] Party travel follow failed: {ex.Message}");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.InputAuthority)]
    private void RpcReceiveInvite(PlayerRef fromPlayer, RpcInfo info = default)
    {
        if (!Object.HasInputAuthority || fromPlayer == Object.InputAuthority)
            return;

        Debug.Log(
            $"[SocialPanel] RpcReceiveInvite on '{name}' from {fromPlayer}. " +
            $"subscribers={(OnInviteReceived != null ? OnInviteReceived.GetInvocationList().Length : 0)}",
            this);

        OnInviteReceived?.Invoke(fromPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RpcKickFromParty(PlayerRef requestedByLeader, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority)
            return;

        if (PartyLeader != requestedByLeader)
            return;

        PartyLeader = Object.InputAuthority;
    }

    public bool TryJoinParty(PlayerRef leaderRef, int currentPartySize)
    {
        if (!Object.HasStateAuthority)
            return false;

        if (leaderRef == Object.InputAuthority)
            return false;

        if (currentPartySize >= PartyConstants.MaxSize)
            return false;

        if (PartyLeader == leaderRef)
            return true;

        if (!IsSolo)
            return false;

        PartyLeader = leaderRef;
        return true;
    }

    private void OnPartyLeaderChanged()
    {
        OnMembershipChanged?.Invoke();
    }
}
