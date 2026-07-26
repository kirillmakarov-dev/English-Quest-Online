using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Facade over overlapping scene-load signals for a single Fusion runner.
/// </summary>
public enum SceneTransitionPhase
{
    Idle,
    FusionLoading,
    LocalLoading,
    PlayableWait,
    Ready
}

public static class SceneTransitionState
{
    public static SceneTransitionPhase GetPhase(NetworkRunner runner, Scene scene = default)
    {
        if (runner == null || !runner.IsRunning)
            return SceneTransitionPhase.Idle;

        if (FusionSceneTransitionService.IsLoading(runner))
            return SceneTransitionPhase.FusionLoading;

        if (runner.SceneManager != null && runner.SceneManager.IsBusy)
            return SceneTransitionPhase.FusionLoading;

        if (!scene.IsValid())
            scene = runner.SimulationUnityScene;

        if (scene.IsValid() && LocalPlayerReadiness.IsReady(runner, scene))
            return SceneTransitionPhase.Ready;

        if (scene.IsValid() && scene.isLoaded)
            return SceneTransitionPhase.PlayableWait;

        return SceneTransitionPhase.Idle;
    }

    public static bool IsTransitioning(NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning)
            return false;

        SceneTransitionPhase phase = GetPhase(runner);
        return phase is SceneTransitionPhase.FusionLoading
            or SceneTransitionPhase.LocalLoading
            or SceneTransitionPhase.PlayableWait;
    }

    public static void MarkLocalLoading(NetworkRunner runner, bool loading)
    {
        if (runner == null)
            return;

        if (loading)
            s_localLoadingRunners.Add(runner);
        else
            s_localLoadingRunners.Remove(runner);
    }

    public static SceneTransitionPhase GetPhaseIncludingLocal(NetworkRunner runner, Scene scene = default)
    {
        if (runner != null && s_localLoadingRunners.Contains(runner))
            return SceneTransitionPhase.LocalLoading;

        return GetPhase(runner, scene);
    }

    private static readonly System.Collections.Generic.HashSet<NetworkRunner> s_localLoadingRunners = new();

    public static void ClearRunner(NetworkRunner runner)
    {
        if (runner == null)
            return;

        s_localLoadingRunners.Remove(runner);
    }
}
