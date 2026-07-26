using System;
using UnityEngine;

/// <summary>
/// Static facade for <see cref="ILogService"/>. Prefer this over <see cref="Debug.Log"/>
/// so output can be toggled globally via the service log level.
/// </summary>
public static class AppLog
{
    static ILogService _service = new LogService(LogLevel.None);

    /// <summary>Current minimum log level. Defaults to <see cref="LogLevel.None"/>.</summary>
    public static LogLevel Level
    {
        get => _service.Level;
        set => _service.Level = value;
    }

    /// <summary>Replace the active logger (e.g. after ServiceLocator registration).</summary>
    public static void Register(ILogService service)
    {
        if (service != null)
            _service = service;
    }

    public static void Info(string message, UnityEngine.Object context = null) => _service.LogInfo(message, context);

    public static void Warning(string message, UnityEngine.Object context = null) => _service.LogWarning(message, context);

    public static void Error(string message, UnityEngine.Object context = null) => _service.LogError(message, context);

    public static void Exception(Exception exception, UnityEngine.Object context = null) => _service.LogException(exception, context);
}
