/// <summary>
/// Minimum severity emitted by <see cref="ILogService"/>.
/// Default is <see cref="None"/> — nothing is written until the level is raised.
/// </summary>
public enum LogLevel
{
    None = 0,
    Error = 1,
    Warning = 2,
    Info = 3,
}
