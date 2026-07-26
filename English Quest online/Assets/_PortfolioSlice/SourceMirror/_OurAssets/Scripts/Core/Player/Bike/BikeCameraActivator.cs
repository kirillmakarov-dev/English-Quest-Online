using Fusion;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Activates/deactivates the bike's Cinemachine cameras locally per-client.
/// Cameras are plain MonoBehaviour — never networked. Each client sets Priority
/// independently based on whether the local player is the rider.
/// Pattern mirrors PlayerCamera.cs (priority 10 = active, 0 = inactive).
/// </summary>
public class BikeCameraActivator : NetworkBehaviour
{
    [SerializeField] private NetworkBikeMount _bikeMount;

    /// <summary>
    /// Assign the same CinemachineCamera objects that are referenced in the
    /// bike's CameraController here. Priority is managed by this component;
    /// CameraController handles shake / FOV / switching.
    /// </summary>
    [SerializeField] private CinemachineCamera[] _bikeCameras;

    // -------------------------------------------------------------------------
    // Fusion lifecycle
    // -------------------------------------------------------------------------

    public override void Spawned()
    {
        // Ensure no bike camera activates on clients that aren't the rider.
        SetCameraPriority(0);

        _bikeMount.OnLocalMounted   += HandleLocalMounted;
        _bikeMount.OnLocalDismounted += HandleLocalDismounted;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_bikeMount == null) return;
        _bikeMount.OnLocalMounted   -= HandleLocalMounted;
        _bikeMount.OnLocalDismounted -= HandleLocalDismounted;
    }

    // -------------------------------------------------------------------------
    // Event handlers
    // -------------------------------------------------------------------------

    private void HandleLocalMounted()   => SetCameraPriority(10);
    private void HandleLocalDismounted() => SetCameraPriority(0);

    // -------------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------a--------------------

    private void SetCameraPriority(int priority)
    {
        if (_bikeCameras == null) return;
        foreach (var cam in _bikeCameras)
        {
            if (cam != null)
                cam.Priority = priority;
        }
    }
}
