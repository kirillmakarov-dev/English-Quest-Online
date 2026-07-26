using Fusion;
using UnityEngine.SceneManagement;
using UnityServiceLocator;

/// <summary>
/// Resolves scene-scoped <see cref="ILocalPlayerReadiness"/> services and clears them per runner.
/// </summary>
public static class LocalPlayerReadiness
{
    public static bool TryGet(Scene scene, out ILocalPlayerReadiness readiness) =>
        ServiceLocator.TryGetForScene(scene, out readiness);

    public static bool TryGet(NetworkRunner runner, Scene scene, out ILocalPlayerReadiness readiness)
    {
        readiness = null;
        if (runner == null || !scene.IsValid())
            return false;

        if (TryGet(scene, out readiness))
            return true;

        Scene simulationScene = runner.SimulationUnityScene;
        if (simulationScene.IsValid() && simulationScene.handle != scene.handle)
            return TryGet(simulationScene, out readiness);

        Scene runnerScene = runner.gameObject.scene;
        if (runnerScene.IsValid() && runnerScene.handle != scene.handle)
            return TryGet(runnerScene, out readiness);

        return false;
    }

    public static bool IsReady(NetworkRunner runner, Scene scene) =>
        TryGet(runner, scene, out ILocalPlayerReadiness readiness) && readiness.IsReady;

    public static bool TryPublishReady(LocalPlayerReadyArgs args)
    {
        if (!args.IsValid)
            return false;

        ServiceLocator.RefreshForScene(args.Scene);

        if (!TryGet(args.Runner, args.Scene, out ILocalPlayerReadiness readiness))
            return false;

        readiness.NotifyReady(args);
        return true;
    }

    public static void Clear(NetworkRunner runner)
    {
        if (runner == null)
            return;

        ClearScene(runner.gameObject.scene);

        Scene simulationScene = runner.SimulationUnityScene;
        if (simulationScene.IsValid())
            ClearScene(simulationScene);
    }

    private static void ClearScene(Scene scene)
    {
        if (TryGet(scene, out ILocalPlayerReadiness readiness))
            readiness.Clear();
    }
}
