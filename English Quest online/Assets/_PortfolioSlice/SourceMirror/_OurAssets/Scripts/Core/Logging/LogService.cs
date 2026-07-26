using System;
using UnityEngine;

/// <summary>
/// Filters Unity console output by <see cref="LogLevel"/>.
/// Starts silent (<see cref="LogLevel.None"/>) until <see cref="Level"/> is changed.
/// </summary>
public sealed class LogService : ILogService
{
    public LogLevel Level { get; set; }

    public LogService(LogLevel initialLevel = LogLevel.None)
    {
        Level = initialLevel;
    }

    public void LogInfo(string message, UnityEngine.Object context = null)
    {
        if (Level < LogLevel.Info) return;
        Debug.Log(message, context);
    }

    public void LogWarning(string message, UnityEngine.Object context = null)
    {
        if (Level < LogLevel.Warning) return;
        Debug.LogWarning(message, context);
    }

    public void LogError(string message, UnityEngine.Object context = null)
    {
        if (Level < LogLevel.Error) return;
        Debug.LogError(message, context);
    }

    public void LogException(Exception exception, UnityEngine.Object context = null)
    {
        if (Level < LogLevel.Error || exception == null) return;
        Debug.LogException(exception, context);
    }
}
