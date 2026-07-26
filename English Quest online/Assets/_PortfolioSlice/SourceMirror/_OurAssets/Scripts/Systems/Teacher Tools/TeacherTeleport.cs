using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public partial class TeacherTeleport : NetworkBehaviour, INetworkRunnerCallbacks
{
    private static readonly List<TeacherTeleport> s_instances = new();

    /// <summary>
    /// Last spawned instance. Prefer <see cref="TryGetForRunner"/> in Multi-Peer.
    /// </summary>
    public static TeacherTeleport Instance { get; private set; }

    [SerializeField] private float _verticalOffset = 5f;

    [Networked]
    public PlayerRef GuiderPlayer { get; set; }

    public bool IsLocalPlayerGuider =>
        Runner != null && Runner.IsRunning && GuiderPlayer == Runner.LocalPlayer;

    public bool IsGuider(PlayerRef player) => GuiderPlayer == player;

    public static bool TryGetForRunner(NetworkRunner runner, out TeacherTeleport teleport)
    {
        teleport = null;
        if (runner == null)
            return false;

        for (int i = s_instances.Count - 1; i >= 0; i--)
        {
            TeacherTeleport candidate = s_instances[i];
            if (candidate == null)
            {
                s_instances.RemoveAt(i);
                continue;
            }

            if (candidate.Runner == runner)
            {
                teleport = candidate;
                return true;
            }
        }

        return false;
    }

    public override void Spawned()
    {
        if (!s_instances.Contains(this))
            s_instances.Add(this);

        Instance = this;
        Runner.AddCallbacks(this);
        TryClaimGuiderIfEligible();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        s_instances.Remove(this);

        if (Instance == this)
            Instance = s_instances.Count > 0 ? s_instances[s_instances.Count - 1] : null;

        runner.RemoveCallbacks(this);
    }

    public override void FixedUpdateNetwork()
    {
        if (GuiderPlayer != PlayerRef.None)
            return;

        TryClaimGuiderIfEligible();
    }

    private void TryClaimGuiderIfEligible()
    {
        if (!PlayerRoleProfile.IsGuider)
            return;

        if (Runner == null || !Runner.IsRunning)
            return;

        // Multi-Peer shares one PlayerRoleProfile process-wide. Prefer the session master
        // so only one local peer becomes guider and host Game views stay predictable.
        if (HasMultipleLocalRunners() && !Runner.IsSharedModeMasterClient && !Runner.IsServer)
            return;

        if (Runner.GetPlayerObject(Runner.LocalPlayer) == null)
            return;

        RPC_RequestClaimGuider(Runner.LocalPlayer);
    }

    private static bool HasMultipleLocalRunners()
    {
        int running = 0;
        foreach (NetworkRunner runner in NetworkRunner.Instances)
        {
            if (runner != null && runner.IsRunning)
                running++;

            if (running > 1)
                return true;
        }

        return false;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestClaimGuider(PlayerRef requester, RpcInfo info = default)
    {
        if (GuiderPlayer != PlayerRef.None && GuiderPlayer != requester)
            return;

        GuiderPlayer = requester;
        AppLog.Info($"[GuiderService] Guider assigned to player {requester}.");
    }

    public void TeleportAllStudentsToTeacher()
    {
        if (Runner == null || !IsLocalPlayerGuider)
            return;

        var localPlayerObj = Runner.GetPlayerObject(Runner.LocalPlayer);
        if (localPlayerObj == null)
        {
            AppLog.Warning("[TeacherTeleport] Could not find the teacher's player object.");
            return;
        }

        AppLog.Info("[TeacherTeleport] Requesting teleport for all clients.");
        RPC_RequestTeleport(Runner.LocalPlayer, localPlayerObj.transform.position, localPlayerObj.transform.rotation);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestTeleport(PlayerRef sender, Vector3 teacherPosition, Quaternion teacherRotation)
    {
        if (!IsGuider(sender))
            return;

        Rpc_TeleportToTeacher(teacherPosition, teacherRotation);
        AppLog.Info("[TeacherTeleport] Teleport RPC sent to all players.");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_TeleportToTeacher(Vector3 teacherPosition, Quaternion teacherRotation)
    {
        var localPlayerObj = Runner.GetPlayerObject(Runner.LocalPlayer);
        if (localPlayerObj == null) return;

        var ncc = localPlayerObj.GetComponent<NetworkCharacterController>();
        if (ncc == null || !ncc.Object.IsValid) return;

        if (IsGuider(Runner.LocalPlayer))
            return;

        Vector3 targetPosition = teacherPosition + Vector3.up * _verticalOffset;
        ncc.Teleport(targetPosition, teacherRotation);
        AppLog.Info("[TeacherTeleport] Local player teleported above the teacher.");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!Object.HasStateAuthority)
            return;

        if (GuiderPlayer == player)
        {
            GuiderPlayer = PlayerRef.None;
            AppLog.Info($"[GuiderService] Guider {player} left — slot cleared.");
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
