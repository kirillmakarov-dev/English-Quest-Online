using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;

/// <summary>
/// Single Fusion callback entry point that forwards runner events to subscribers in a defined order.
/// Inspired by the Fusion Social Hub <c>ConnectionCallbacks</c> pattern.
/// </summary>
public class NetworkRunnerCallbackHub : INetworkRunnerCallbacks
{
    public static NetworkRunnerCallbackHub Instance { get; } = new();

    private readonly HashSet<NetworkRunner> _registeredRunners = new();

    public event Action<NetworkRunner, PlayerRef> PlayerJoined;
    public event Action<NetworkRunner, PlayerRef> PlayerLeft;
    public event Action<NetworkRunner, ShutdownReason> Shutdown;
    public event Action<NetworkRunner, NetDisconnectReason> DisconnectedFromServer;
    public event Action<NetworkRunner, NetAddress, NetConnectFailedReason> ConnectFailed;
    public event Action<NetworkRunner> SceneLoadStart;
    public event Action<NetworkRunner> SceneLoadDone;
    public event Action<NetworkRunner> ConnectedToServer;

    public static void BindRunner(NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning)
            return;

        Instance.RegisterRunner(runner);
    }

    public static void UnbindRunner(NetworkRunner runner)
    {
        Instance.UnregisterRunner(runner);
    }

    private void RegisterRunner(NetworkRunner runner)
    {
        if (!_registeredRunners.Add(runner))
            return;

        runner.AddCallbacks(this);
    }

    private void UnregisterRunner(NetworkRunner runner)
    {
        if (runner == null)
            return;

        if (_registeredRunners.Remove(runner))
            runner.RemoveCallbacks(this);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) =>
        PlayerJoined?.Invoke(runner, player);

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        NetworkAuthorityService.ReclaimOrphanedAuthority(runner, player);
        PlayerLeft?.Invoke(runner, player);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        UnregisterRunner(runner);
        Shutdown?.Invoke(runner, shutdownReason);
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) =>
        DisconnectedFromServer?.Invoke(runner, reason);

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) =>
        ConnectFailed?.Invoke(runner, remoteAddress, reason);

    public void OnSceneLoadStart(NetworkRunner runner) =>
        SceneLoadStart?.Invoke(runner);

    public void OnSceneLoadDone(NetworkRunner runner) =>
        SceneLoadDone?.Invoke(runner);

    public void OnConnectedToServer(NetworkRunner runner) =>
        ConnectedToServer?.Invoke(runner);

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
