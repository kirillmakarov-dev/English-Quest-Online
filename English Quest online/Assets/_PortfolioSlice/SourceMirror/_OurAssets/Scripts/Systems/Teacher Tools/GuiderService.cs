using Fusion;
using UnityEngine;

/// <summary>
/// Session guider access hosted on <see cref="TeacherTeleport"/>.
/// Prefer runner-scoped APIs in Fusion Multi-Peer — the static Instance can point at the wrong peer.
/// </summary>
public static class GuiderService
{
    public static TeacherTeleport Instance => TeacherTeleport.Instance;

    public static bool TryGetForRunner(NetworkRunner runner, out TeacherTeleport teleport) =>
        TeacherTeleport.TryGetForRunner(runner, out teleport);

    public static bool IsLocalPlayerGuiderFor(NetworkRunner runner) =>
        TryGetForRunner(runner, out TeacherTeleport teleport) && teleport.IsLocalPlayerGuider;

    /// <summary>
    /// True when the focused (ProvideInput) runner's local player holds the session guider slot.
    /// For Multi-Peer UI, prefer <see cref="IsLocalPlayerGuiderFor"/>.
    /// </summary>
    public static bool IsLocalPlayerGuider
    {
        get
        {
            if (TryGetFocused(out TeacherTeleport focused))
                return focused.IsLocalPlayerGuider;

            return Instance != null && Instance.IsLocalPlayerGuider;
        }
    }

    public static bool IsGuider(PlayerRef player)
    {
        if (TryGetFocused(out TeacherTeleport focused))
            return focused.IsGuider(player);

        return Instance != null && Instance.IsGuider(player);
    }

    private static bool TryGetFocused(out TeacherTeleport teleport)
    {
        teleport = null;

        foreach (NetworkRunner runner in NetworkRunner.Instances)
        {
            if (runner == null || !runner.IsRunning || !runner.ProvideInput)
                continue;

            if (TryGetForRunner(runner, out teleport))
                return true;
        }

        return false;
    }
}
