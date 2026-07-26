using Fusion;

/// <summary>
/// Static access to guider quest control RPCs hosted on <see cref="TeacherTeleport"/>.
/// Prefer runner-scoped calls in Fusion Multi-Peer.
/// </summary>
public static class GuiderQuestControlNetworked
{
    public static TeacherTeleport Instance => TeacherTeleport.Instance;

    public static bool TryGetForRunner(NetworkRunner runner, out TeacherTeleport teleport) =>
        TeacherTeleport.TryGetForRunner(runner, out teleport);

    public static void RequestSnapshot(NetworkRunner runner, PlayerRef targetPlayer)
    {
        if (TryGetForRunner(runner, out TeacherTeleport teleport))
            teleport.RequestQuestSnapshot(targetPlayer);
        else
            Instance?.RequestQuestSnapshot(targetPlayer);
    }

    public static void RequestSnapshot(PlayerRef targetPlayer)
    {
        if (GuiderService.TryGetForRunner(GetFocusedRunner(), out TeacherTeleport focused))
            focused.RequestQuestSnapshot(targetPlayer);
        else
            Instance?.RequestQuestSnapshot(targetPlayer);
    }

    public static void ApplyAction(NetworkRunner runner, PlayerRef targetPlayer, string questId, GuiderQuestAction action)
    {
        if (TryGetForRunner(runner, out TeacherTeleport teleport))
            teleport.ApplyQuestAction(targetPlayer, questId, action);
        else
            Instance?.ApplyQuestAction(targetPlayer, questId, action);
    }

    public static void ApplyAction(PlayerRef targetPlayer, string questId, GuiderQuestAction action)
    {
        if (GuiderService.TryGetForRunner(GetFocusedRunner(), out TeacherTeleport focused))
            focused.ApplyQuestAction(targetPlayer, questId, action);
        else
            Instance?.ApplyQuestAction(targetPlayer, questId, action);
    }

    private static NetworkRunner GetFocusedRunner()
    {
        foreach (NetworkRunner runner in NetworkRunner.Instances)
        {
            if (runner != null && runner.IsRunning && runner.ProvideInput)
                return runner;
        }

        return null;
    }
}
