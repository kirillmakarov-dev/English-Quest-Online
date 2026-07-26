using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class SessionPlayerRegistry : ISessionPlayerRegistry
{
    private readonly List<SessionPlayerInfo> _players = new();
    private INetworkSessionService _sessionService;
    private IPartyService _partyService;
    private Component _context;

    public event Action OnPlayersChanged;

    public IReadOnlyList<SessionPlayerInfo> Players => _players;

    public void Bind(INetworkSessionService sessionService, IPartyService partyService, Component context = null)
    {
        _sessionService = sessionService;
        _partyService = partyService;
        _context = context;
    }

    public void Refresh()
    {
        _players.Clear();

        NetworkRunner runner = ResolveRunner();
        if (runner == null || !runner.IsRunning)
        {
            OnPlayersChanged?.Invoke();
            return;
        }

        PlayerRef localPlayer = runner.LocalPlayer;
        PlayerRef myLeader = _partyService.GetPartyLeader(localPlayer);

        foreach (PlayerRef player in FusionCoSessionRunners.EnumeratePlayers(runner))
        {
            PlayerRef playerLeader = _partyService.GetPartyLeader(player);
            bool isInMyParty = myLeader.IsRealPlayer && playerLeader == myLeader;
            bool isPartyLeader = player == playerLeader;

            _players.Add(new SessionPlayerInfo(
                player,
                PartyMembershipUtility.GetDisplayName(runner, player),
                player == localPlayer,
                isInMyParty,
                isPartyLeader));
        }

        _players.Sort((a, b) =>
        {
            if (a.IsLocal != b.IsLocal)
                return a.IsLocal ? -1 : 1;

            return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
        });

        OnPlayersChanged?.Invoke();
    }

    private NetworkRunner ResolveRunner()
    {
        if (_context != null
            && SceneNetworkRunner.TryGetForScene(_context.gameObject.scene, out NetworkRunner sceneRunner)
            && sceneRunner.IsRunning)
        {
            return sceneRunner;
        }

        NetworkRunner boundRunner = _sessionService?.Runner;
        return boundRunner != null && boundRunner.IsRunning ? boundRunner : null;
    }
}
