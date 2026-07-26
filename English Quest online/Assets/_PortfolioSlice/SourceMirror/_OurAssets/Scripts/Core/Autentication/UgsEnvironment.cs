using System;
using Unity.Services.Core;
using Unity.Services.Core.Environments.Internal;
using Unity.Services.Core.Internal;

/// <summary>
/// Reads the active Unity Gaming Services environment
/// (<c>development</c> / <c>production</c> from Project Settings).
/// </summary>
public static class UgsEnvironment
{
    public const string Production = "production";
    public const string Development = "development";

    /// <summary>
    /// True when the active UGS environment is anything other than production
    /// (including local editor before services finish initializing).
    /// </summary>
    public static bool IsNonProduction
    {
        get
        {
            string environmentName = TryGetName();
            if (!string.IsNullOrWhiteSpace(environmentName))
                return !string.Equals(environmentName, Production, StringComparison.OrdinalIgnoreCase);

#if UNITY_EDITOR
            // ProjectSettings default UGS environment is development.
            return true;
#else
            return false;
#endif
        }
    }

    public static string TryGetName()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            return null;

        CoreRegistry registry = CoreRegistry.Instance;
        if (registry == null)
            return null;

        if (!registry.TryGetServiceComponent(out IEnvironments environments) || environments == null)
            return null;

        return environments.Current;
    }
}
