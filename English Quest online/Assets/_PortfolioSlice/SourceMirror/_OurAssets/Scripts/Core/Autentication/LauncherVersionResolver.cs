using System;

/// <summary>
/// Resolves the game version passed by the launcher via command-line arguments.
/// </summary>
public static class LauncherVersionResolver
{
    private const string ArgPrefix = "-gameVersion=";

    /// <summary>
    /// Scans <see cref="Environment.GetCommandLineArgs"/> for <c>-gameVersion=...</c>.
    /// Returns an empty string when the argument is absent.
    /// </summary>
    public static string FromCommandLine()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg.StartsWith(ArgPrefix, StringComparison.Ordinal))
                return arg.Substring(ArgPrefix.Length);

            if (arg == "-gameVersion" && i + 1 < args.Length)
                return args[i + 1];
        }

        return string.Empty;
    }
}
