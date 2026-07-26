using System;
using Fusion;
using Fusion.Sockets;

/// <summary>
/// Surfaces user-facing network session errors to UI listeners.
/// </summary>
public static class NetworkSessionErrorService
{
    public static event Action<string> ErrorRaised;

    public static void Report(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        AppLog.Error($"[NetworkSession] {message}");
        ErrorRaised?.Invoke(message);
    }

    public static void ReportStartGameFailure(ShutdownReason reason)
    {
        Report(FormatStartGameFailure(reason));
    }

    public static void ReportConnectFailed(NetConnectFailedReason reason)
    {
        Report($"Could not connect to the server ({reason}). Check your internet connection and try again.");
    }

    public static string FormatStartGameFailure(ShutdownReason reason)
    {
        return reason switch
        {
            ShutdownReason.GameNotFound => "Session not found. Check the session name and try again.",
            ShutdownReason.GameIsFull => "Session is full. Try again later or join another session.",
            ShutdownReason.GameClosed => "This session is closed.",
            ShutdownReason.ConnectionTimeout => "Connection timed out. Please try again.",
            ShutdownReason.ConnectionRefused => "Connection refused. The server may be unavailable.",
            ShutdownReason.InvalidAuthentication => "Authentication failed. Please sign in again.",
            _ => $"Could not join the session ({reason}). Please try again."
        };
    }
}
