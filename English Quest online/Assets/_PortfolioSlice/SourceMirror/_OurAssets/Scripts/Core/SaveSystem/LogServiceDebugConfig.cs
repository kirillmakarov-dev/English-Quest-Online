using UnityEngine;

/// <summary>
/// Editor-friendly debug config for <see cref="LogServiceBootstrapper"/>.
/// Keep the asset at Assets/_OurAssets/Resources/Editor/LogServiceDebugConfig.asset
/// so it is easy to change from Tools > English Kingdom > Debug.
/// </summary>
[CreateAssetMenu(
    menuName = "English Kingdom/Dev/Log Service Debug Config",
    fileName = "LogServiceDebugConfig")]
public sealed class LogServiceDebugConfig : ScriptableObject
{
    [Tooltip("Minimum severity emitted by LogService.")]
    public LogLevel initialLevel = LogLevel.None;

    private const string ResourcePath = "Editor/LogServiceDebugConfig";

    public static LogServiceDebugConfig Load() =>
        Resources.Load<LogServiceDebugConfig>(ResourcePath);
}
