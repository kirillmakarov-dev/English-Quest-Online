using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PartyService : IPartyService
{
    private INetworkSessionService _sessionService;
    private Component _context;

    public event Action OnPartyChanged;

    public void Bind(INetworkSessionService sessionService, Component context = null)
    {
        _sessionService = sessionService;
        _context = context;
    }

    public PartySnapshot GetMyParty()
    {
        NetworkRunner runner = ResolveRunner();
        if (runner == null || !runner.IsRunning)
            return EmptySnapshot();

        PlayerRef localPlayer = runner.LocalPlayer;
        PlayerPartyMembership localMembership = PartyMembershipUtility.GetMembership(runner, localPlayer);
        if (localMembership == null)
            return EmptySnapshot();

        PlayerRef leaderRef = localMembership.PartyLeader;
        List<PartyMemberInfo> members = BuildMemberList(runner, leaderRef, localPlayer);
        int openSlotCount = Mathf.Max(0, PartyConstants.MaxSize - members.Count);
        bool isLeader = leaderRef == localPlayer;
        bool isInMultiMemberParty = members.Count > 1;

        return new PartySnapshot(members, openSlotCount, isLeader, isInMultiMemberParty);
    }

    public bool CanInvite(PlayerRef target)
    {
        NetworkRunner runner = ResolveRunner();
        if (runner == null || !runner.IsRunning)
            return false;

        PlayerRef localPlayer = runner.LocalPlayer;
        if (!target.IsRealPlayer || target == localPlayer)
            return false;

        PlayerPartyMembership localMembership = PartyMembershipUtility.GetMembership(runner, localPlayer);
        if (localMembership == null || !localMembership.IsPartyLeader)
            return false;

        if (GetPartySize(localPlayer) >= PartyConstants.MaxSize)
            return false;

        PlayerPartyMembership targetMembership = PartyMembershipUtility.GetMembership(runner, target);
        if (targetMembership == null)
            return false;

        if (targetMembership.PartyLeader == localPlayer)
            return false;

        return targetMembership.IsSolo;
    }

    public void LeaveParty()
    {
        NetworkRunner runner = ResolveRunner();
        if (runner == null || !runner.IsRunning)
            return;

        PlayerPartyMembership localMembership = PartyMembershipUtility.GetMembership(runner, runner.LocalPlayer);
        localMembership?.LeaveParty();
        NotifyChanged();
    }

    public void Kick(PlayerRef target)
    {
        NetworkRunner runner = ResolveRunner();
        if (runner == null || !runner.IsRunning)
            return;

        PlayerRef localPlayer = runner.LocalPlayer;
        PlayerPartyMembership localMembership = PartyMembershipUtility.GetMembership(runner, localPlayer);
        if (localMembership == null || !localMembership.IsPartyLeader)
            return;

        localMembership.RequestKick(target, localPlayer);
        NotifyChanged();
    }

    public int GetPartySize(PlayerRef leaderRef)
    {
        NetworkRunner runner = ResolveRunner();
        if (runner == null || !runner.IsRunning || !leaderRef.IsRealPlayer)
            return 0;

        int count = 0;
        foreach (PlayerRef player in FusionCoSessionRunners.EnumeratePlayers(runner))
        {
            PlayerPartyMembership membership = PartyMembershipUtility.GetMembership(runner, player);
            if (membership != null && membership.PartyLeader == leaderRef)
                count++;
        }

        return count;
    }

    public PlayerRef GetPartyLeader(PlayerRef member)
    {
        NetworkRunner runner = ResolveRunner();
        if (runner == null || !runner.IsRunning)
            return PlayerRef.None;

        PlayerPartyMembership membership = PartyMembershipUtility.GetMembership(runner, member);
        return membership != null ? membership.PartyLeader : PlayerRef.None;
    }

    public void HandlePlayerLeft(PlayerRef player)
    {
        NetworkRunner runner = ResolveRunner();
        if (runner == null || !runner.IsRunning)
            return;

        foreach (PlayerRef activePlayer in FusionCoSessionRunners.EnumeratePlayers(runner))
        {
            PlayerPartyMembership membership = PartyMembershipUtility.GetMembership(runner, activePlayer);
            membership?.HandleLeaderDisconnected(player);
        }

        NotifyChanged();
    }

    public void TrackMembership(PlayerPartyMembership membership)
    {
        if (membership == null)
            return;

        membership.OnMembershipChanged -= OnMembershipChanged;
        membership.OnMembershipChanged += OnMembershipChanged;
    }

    public void UntrackMembership(PlayerPartyMembership membership)
    {
        if (membership == null)
            return;

        membership.OnMembershipChanged -= OnMembershipChanged;
    }

    public void NotifyChanged()
    {
        OnPartyChanged?.Invoke();
    }

    private void OnMembershipChanged()
    {
        NotifyChanged();
    }

    private static PartySnapshot EmptySnapshot()
    {
        return new PartySnapshot(Array.Empty<PartyMemberInfo>(), PartyConstants.MaxSize, true, false);
    }

    private static List<PartyMemberInfo> BuildMemberList(NetworkRunner runner, PlayerRef leaderRef, PlayerRef localPlayer)
    {
        List<PartyMemberInfo> members = new();

        foreach (PlayerRef player in FusionCoSessionRunners.EnumeratePlayers(runner))
        {
            PlayerPartyMembership membership = PartyMembershipUtility.GetMembership(runner, player);
            if (membership == null || membership.PartyLeader != leaderRef)
                continue;

            members.Add(new PartyMemberInfo(
                player,
                PartyMembershipUtility.GetDisplayName(runner, player),
                player == leaderRef,
                player == localPlayer));
        }

        members.Sort((a, b) =>
        {
            if (a.IsLeader != b.IsLeader)
                return a.IsLeader ? -1 : 1;

            return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
        });

        return members;
    }

    private NetworkRunner ResolveRunner()
    {
        NetworkRunner boundRunner = _sessionService?.Runner;
        if (boundRunner != null && boundRunner.IsRunning)
            return boundRunner;

        if (_context != null
            && SceneNetworkRunner.TryGetForScene(_context.gameObject.scene, out NetworkRunner sceneRunner)
            && sceneRunner.IsRunning)
        {
            return sceneRunner;
        }

        return null;
    }
}
