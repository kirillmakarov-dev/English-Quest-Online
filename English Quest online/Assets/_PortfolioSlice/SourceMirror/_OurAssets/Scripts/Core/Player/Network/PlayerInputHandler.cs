using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public struct NetworkInputData : INetworkInput
{
    public Vector3 MoveDirection;
    public Vector3 LookDirection;
    public NetworkButtons Buttons;
}

public enum InputButton
{
    Jump = 0,
    Sprint = 1,
}

public class PlayerInputHandler : NetworkBehaviour, INetworkRunnerCallbacks
{
    private Vector3 _moveDirection;
    private Vector3 _lookDirection;
    private bool _jumpPressed;
    private bool _sprintHeld;
    private Camera _cam;
    private bool _hasFocus = true;

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            Runner.AddCallbacks(this);
            _lookDirection = GetFlatForward(transform);
        }
    }

    private static Vector3 GetFlatForward(Transform target)
    {
        Vector3 forward = target.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Object.HasInputAuthority)
        {
            Runner.RemoveCallbacks(this);
        }
    }

    // Called by Unity when the application window gains or loses OS focus (e.g. alt-tab, second monitor click).
    // Guard with Object null-check because Unity can fire this before Spawned() in edge cases.
    private void OnApplicationFocus(bool hasFocus)
    {
        _hasFocus = hasFocus;
        if (!hasFocus && Object != null && Object.HasInputAuthority)
            ClearInput();
    }

    // Called on mobile/editor pause; mirrors OnApplicationFocus for safety.
    private void OnApplicationPause(bool isPaused)
    {
        _hasFocus = !isPaused;
        if (isPaused && Object != null && Object.HasInputAuthority)
            ClearInput();
    }

    private void ClearInput()
    {
        _moveDirection = Vector3.zero;
        _jumpPressed   = false;
        _sprintHeld    = false;
    }

    private Camera ResolveSceneCamera()
    {
        Scene scene = gameObject.scene;
        if (!scene.IsValid())
            return Camera.main;

        return PlayerSceneCamera.ResolveOutputCamera(scene);
    }

    private void Update()
    {
        if (Object == null || !Object.IsValid)
            return;

        // Only gather input if we have Input Authority (local player)
        if (!Object.HasInputAuthority) return;

        // Don't read stale OS input while the window is not focused.
        // Fusion still calls OnInput() in the background — ClearInput() ensures we send zeroes.
        if (!_hasFocus) return;

        // Gather movement input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Each Fusion Multi-Peer client simulates in its own scene — never use Camera.main.
        if (_cam == null || _cam.gameObject.scene != gameObject.scene)
            _cam = ResolveSceneCamera();
        
        if (_cam != null)
        {
            Vector3 cameraForward = _cam.transform.forward;
            Vector3 cameraRight = _cam.transform.right;

            // Flatten vertically to keep movement on horizontal plane
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            // Calculate direction relative to camera
            _moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;
            _lookDirection = cameraForward;
        }
        else
        {
            // Fallback to world space if no camera found
            _moveDirection = new Vector3(horizontal, 0, vertical).normalized;
            _lookDirection = transform.forward;
        }

        // Gather jump input
        // We use GetButton so it stays true while held, or GetButtonDown with a reset logic.
        // For NetworkInput, it's often safer to just check status or accumulate "was pressed".
        if (Input.GetButton("Jump") || Input.GetKey(KeyCode.Space)) 
        {
            _jumpPressed = true;
        }
        else
        {
            _jumpPressed = false;
        }

        _sprintHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();
        data.LookDirection = _lookDirection;
        data.MoveDirection = _moveDirection;
        data.Buttons.Set(InputButton.Jump, _jumpPressed);
        data.Buttons.Set(InputButton.Sprint, _sprintHeld);

        input.Set(data);
    }

    // --- Diagnostics ---
    // Right-click this component in the Inspector → "Log Network Diagnostics"
    // to print RTT and tick info to the console. Useful for diagnosing tab-switch lag.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [ContextMenu("Log Network Diagnostics")]
    private void LogNetworkDiagnostics()
    {
        if (Runner == null || !Runner.IsRunning)
        {
            Debug.Log("[PlayerInputHandler] Runner not running.");
            return;
        }
        double rtt = Runner.GetPlayerRtt(Runner.LocalPlayer);
        Debug.Log($"[PlayerInputHandler] RTT={rtt * 1000:F1}ms | Tick={Runner.Tick} | " +
                  $"HasFocus={_hasFocus} | MaxDeltaTime={Time.maximumDeltaTime:F3}s");
    }
#endif

    // --- Unused INetworkRunnerCallbacks ---

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { } 
}
