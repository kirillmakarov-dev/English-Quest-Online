using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityServiceLocator;

/// <summary>
/// Resolves the active <see cref="NetworkRunner"/> for monster spawning in a scene.
/// </summary>
public static class MonsterSpawnRunnerResolver
{
    public static bool TryGetSpawnRunner(MonoBehaviour context, out NetworkRunner runner)
    {
        runner = null;
        if (context == null)
            return false;

        Scene scene = context.gameObject.scene;

        if (SceneNetworkRunner.TryGetForScene(scene, out runner) && runner.IsRunning)
            return true;

        if (TryGetRunnerFromService(ServiceLocator.For(context), out runner))
            return true;

        if (TryGetRunnerFromService(ServiceLocator.Global, out runner))
            return true;

        foreach (NetworkRunner instance in NetworkRunner.Instances)
        {
            if (instance == null || !instance.IsRunning)
                continue;

            Scene simulationScene = instance.SimulationUnityScene;
            if (simulationScene.IsValid() && simulationScene.handle == scene.handle)
            {
                runner = instance;
                return true;
            }
        }

        runner = null;
        return false;
    }

    public static bool IsNetworkSessionActive(MonoBehaviour context)
    {
        return TryGetSpawnRunner(context, out _);
    }

    private static bool TryGetRunnerFromService(ServiceLocator locator, out NetworkRunner runner)
    {
        runner = null;
        if (locator == null || !locator.TryGet(out INetworkSessionService session))
            return false;

        runner = session.Runner;
        return runner != null && runner.IsRunning;
    }
}
