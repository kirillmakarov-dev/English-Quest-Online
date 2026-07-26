using System;
using UnityEngine;

/// <summary>
/// Central logging contract resolved via <c>ServiceLocator.Global</c>.
/// </summary>
public interface ILogService
{
    LogLevel Level { get; set; }

    void LogInfo(string message, UnityEngine.Object context = null);
    void LogWarning(string message, UnityEngine.Object context = null);
    void LogError(string message, UnityEngine.Object context = null);
    void LogException(Exception exception, UnityEngine.Object context = null);
}
