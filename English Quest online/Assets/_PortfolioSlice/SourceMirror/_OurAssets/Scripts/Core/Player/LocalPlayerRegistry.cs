using Fusion;
using UnityEngine;

/// <summary>
/// Resolves the local player's transform via the Fusion Runner.
/// LOD distance is always calculated client-locally — never networked.
/// </summary>
public static class LocalPlayerRegistry
{
    /// <summary>
    /// Returns the local player's transform, or null if the player object is
    /// not yet spawned. Uses Runner.GetPlayerObject so no manual registration is needed.
    /// </summary>
    public static Transform GetLocalTransform(NetworkRunner runner)
    {
        if (runner == null) return null;
        var playerObject = runner.GetPlayerObject(runner.LocalPlayer);
        return playerObject != null ? playerObject.transform : null;
    }
}
