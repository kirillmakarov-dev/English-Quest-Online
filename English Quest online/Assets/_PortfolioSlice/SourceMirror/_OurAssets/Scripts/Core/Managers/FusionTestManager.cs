using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;

// This forces the script to run BEFORE the PrototypeNetworkStart script
[DefaultExecutionOrder(-100)]
public class FusionTestManager : MonoBehaviour
{
    private const string MenuSceneName = "Menu";
    private const string ProductionRunnerRootName = "NetworkRunner";

    private void Awake()
    {
#if !UNITY_EDITOR
        Destroy(gameObject);
        return;
#endif

#if UNITY_EDITOR
        if (ShouldSuppressPrototypeBootstrap(transform.root))
        {
            AppLog.Info("FusionTestManager: Active editor session detected. Destroying prototype setup.");
            Destroy(gameObject);
            return;
        }
#endif

        EnsureMultiPeerTestComponents();
    }

    /// <summary>
    /// True when an existing Fusion session (menu flow, world travel, or external runners)
    /// should hide the Prototype Network Start UI in the newly loaded scene.
    /// </summary>
    internal static bool ShouldSuppressPrototypeBootstrap(Transform prototypeRoot = null)
    {
#if !UNITY_EDITOR
        return true;
#else
        if (IsMenuNetworkFlowActive())
            return true;

        if (IsWorldTravelFlowActive())
            return true;

        if (prototypeRoot != null && HasRunningExternalRunner(prototypeRoot))
            return true;

        return false;
#endif
    }

    internal static bool IsMenuNetworkFlowActive()
    {
#if !UNITY_EDITOR
        return true;
#else
        GameNetworkManager manager = FindFirstObjectByType<GameNetworkManager>(FindObjectsInactive.Include);
        if (!IsPersistentMenuNetworkManager(manager))
            return false;

        if (HasRunningProductionRunner(manager))
            return true;

        // Menu can still be loaded briefly while Fusion transitions into the gameplay scene.
        return IsSceneLoaded(MenuSceneName);
#endif
    }

    private static bool IsPersistentMenuNetworkManager(GameNetworkManager manager)
    {
        return manager != null && manager.gameObject.scene.name == "DontDestroyOnLoad";
    }

    private static bool HasRunningProductionRunner(GameNetworkManager manager)
    {
        NetworkRunner managerRunner = manager != null ? manager.Runner : null;
        if (IsProductionRunner(managerRunner) && managerRunner.IsRunning)
            return true;

        foreach (NetworkRunner runner in NetworkRunner.Instances)
        {
            if (IsProductionRunner(runner) && runner.IsRunning)
                return true;
        }

        return false;
    }

    private static bool IsProductionRunner(NetworkRunner runner)
    {
        if (runner == null)
            return false;

        GameObject runnerRoot = runner.transform.root.gameObject;
        return runnerRoot.scene.name == "DontDestroyOnLoad"
            && runnerRoot.name == ProductionRunnerRootName;
    }

    private static bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && scene.name == sceneName)
                return true;
        }

        return false;
    }

    private static bool IsWorldTravelFlowActive()
    {
        if (PlayerTravelArrival.HasPending)
            return true;

        WorldTravelService travelService = FindFirstObjectByType<WorldTravelService>(FindObjectsInactive.Include);
        return travelService != null && travelService.IsTraveling;
    }

    private static bool HasRunningExternalRunner(Transform prototypeRoot)
    {
        foreach (NetworkRunner runner in NetworkRunner.Instances)
        {
            if (runner == null || !runner.IsRunning)
                continue;

            if (runner.transform.root == prototypeRoot)
                continue;

            return true;
        }

        return false;
    }

    private void EnsureMultiPeerTestComponents()
    {
        if (GetComponent<PlayerSpawnCoordinator>() == null)
            gameObject.AddComponent<PlayerSpawnCoordinator>();
    }
}
