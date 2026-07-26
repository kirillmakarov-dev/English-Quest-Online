using UnityEngine;

/// <summary>
/// Caps the application frame rate and physics catch-up at startup.
/// Place this component on any persistent GameObject in the PreLoad scene.
/// Application.targetFrameRate is an engine-global — no DontDestroyOnLoad needed.
/// </summary>
public class ApplicationSettings : MonoBehaviour
{
    [Header("Frame Rate")]
    [Tooltip("Target frames per second. 60 is recommended for multiplayer (Photon Fusion) on broad hardware.")]
    [Range(15, 144)]
    [SerializeField] private int _targetFps = 60;

    [Header("Simulation Stability")]
    [Tooltip(
        "Maximum time Unity is allowed to catch up in one frame (seconds). " +
        "Without this, alt-tabbing out and returning causes a massive tick burst: " +
        "all the time you were away gets simulated in one frame → the player freezes then snaps. " +
        "0.1 = at most 100 ms of catch-up per frame regardless of how long you were away.")]
    [Range(0.033f, 0.5f)]
    [SerializeField] private float _maxDeltaTime = 0.1f;

    private static int s_targetFps = 60;

    private void Awake()
    {
        s_targetFps = _targetFps;
        Application.targetFrameRate = _targetFps;
        Time.maximumDeltaTime = _maxDeltaTime;
    }

    /// <summary>
    /// Re-applies frame-rate cap after other systems (e.g. SettingsService) touch vSync.
    /// </summary>
    public static void Apply()
    {
        Application.targetFrameRate = s_targetFps;
    }

    public static void Apply(int targetFps, float maxDeltaTime)
    {
        s_targetFps = targetFps;
        Application.targetFrameRate = targetFps;
        Time.maximumDeltaTime = maxDeltaTime;
    }
}
