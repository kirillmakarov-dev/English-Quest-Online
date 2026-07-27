using UnityEngine;

/// <summary>
/// Editor-friendly debug config for the project log level menu.
/// Keep the asset in Resources/Editor so debug tools can load it consistently.
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
