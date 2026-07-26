using Fusion;
using UnityEngine.SceneManagement;

/// <summary>
/// Resolves which <see cref="NetworkRunner"/> owns a loaded Unity scene.
/// </summary>
public static class SceneNetworkRunner
{
    public static bool TryGetForScene(Scene scene, out NetworkRunner runner)
    {
        runner = null;
        if (!scene.IsValid())
            return false;

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

            Scene runnerScene = instance.gameObject.scene;
            if (runnerScene.IsValid() && runnerScene.handle == scene.handle)
            {
                runner = instance;
                return true;
            }
        }

        return false;
    }

    public static bool IsProvidingInputForScene(Scene scene)
    {
        return TryGetForScene(scene, out NetworkRunner runner) && runner.ProvideInput;
    }
}
