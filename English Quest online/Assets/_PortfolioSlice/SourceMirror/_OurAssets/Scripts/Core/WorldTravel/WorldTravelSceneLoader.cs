using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityServiceLocator;

internal static class WorldTravelSceneLoader
{
    public const float JoinRetryTimeoutSeconds = 15f;
    public const float JoinRetryIntervalSeconds = 0.5f;

    public static async UniTask<bool> SwitchToDestinationSessionAsync(
        TravelSessionPlan sessionPlan,
        WorldMapNodeData destination,
        PlayerInteraction interactor,
        MonoBehaviour serviceContext)
    {
        if (!sessionPlan.IsValid)
        {
            AppLog.Error("[WorldTravelService] Travel session plan is invalid.");
            return false;
        }

        if (destination.destinationType != WorldMapDestinationType.LoadScene)
            return true;

        if (!destination.scene.IsValid)
        {
            AppLog.Error($"[WorldTravelService] Invalid scene reference for '{destination.id}'.");
            return false;
        }

        int buildIndex = destination.scene.BuildIndex;
        if (!TravelInteractorResolver.TryGetFusionRunner(interactor, serviceContext, out NetworkRunner runner))
            return await LoadOfflineSceneAsync(buildIndex);

        if (!TryResolveNetworkSession(serviceContext, runner, out INetworkSessionService netSession))
        {
            AppLog.Error("[WorldTravelService] INetworkSessionService not available for travel.");
            return false;
        }

        return await ExecuteSessionSwitchAsync(netSession, runner, sessionPlan, buildIndex);
    }

    private static async UniTask<bool> ExecuteSessionSwitchAsync(
        INetworkSessionService netSession,
        NetworkRunner runner,
        TravelSessionPlan sessionPlan,
        int buildIndex)
    {
        BindRunnerForSessionSwitch(netSession, runner);

        if (NeedsDisconnect(runner, sessionPlan.SessionName))
            await netSession.Disconnect();

        if (sessionPlan.CreatesSession)
        {
            await netSession.StartSharedSession(
                sessionPlan.SessionName,
                buildIndex,
                sessionPlan.MaxPlayers,
                enableClientSessionCreation: true);
            return netSession.Runner != null && netSession.Runner.IsRunning;
        }

        return await JoinSessionWithRetryAsync(netSession, sessionPlan.SessionName);
    }

    private static async UniTask<bool> JoinSessionWithRetryAsync(
        INetworkSessionService netSession,
        string sessionName)
    {
        float elapsed = 0f;
        while (elapsed < JoinRetryTimeoutSeconds)
        {
            try
            {
                await netSession.JoinSharedSession(sessionName);
                if (netSession.Runner != null && netSession.Runner.IsRunning)
                    return true;
            }
            catch (System.Exception ex)
            {
                if (elapsed + JoinRetryIntervalSeconds >= JoinRetryTimeoutSeconds)
                {
                    AppLog.Error($"[WorldTravelService] Failed to join travel session: {ex.Message}");
                    return false;
                }
            }

            elapsed += JoinRetryIntervalSeconds;
            await UniTask.Delay(
                System.TimeSpan.FromSeconds(JoinRetryIntervalSeconds),
                ignoreTimeScale: true);
        }

        AppLog.Error($"[WorldTravelService] Timed out joining travel session after {JoinRetryTimeoutSeconds:0.#}s.");
        return false;
    }

    private static bool NeedsDisconnect(NetworkRunner runner, string targetSessionName)
    {
        if (runner == null || !runner.IsRunning || string.IsNullOrWhiteSpace(targetSessionName))
            return false;

        if (!runner.SessionInfo.IsValid || string.IsNullOrEmpty(runner.SessionInfo.Name))
            return true;

        return runner.SessionInfo.Name != targetSessionName;
    }

    private static async UniTask<bool> LoadOfflineSceneAsync(int buildIndex)
    {
        if (buildIndex < 0)
            return false;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
        if (loadOperation == null)
            return false;

        while (!loadOperation.isDone)
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

        return true;
    }

    private static bool TryResolveNetworkSession(
        MonoBehaviour serviceContext,
        NetworkRunner runner,
        out INetworkSessionService netSession)
    {
        if (serviceContext != null
            && ServiceLocator.For(serviceContext).TryGet(out netSession))
        {
            BindRunnerForSessionSwitch(netSession, runner);
            return true;
        }

        if (ServiceLocator.Global != null && ServiceLocator.Global.TryGet(out netSession))
        {
            BindRunnerForSessionSwitch(netSession, runner);
            return true;
        }

        netSession = null;
        return false;
    }

    private static void BindRunnerForSessionSwitch(INetworkSessionService netSession, NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning || netSession == null)
            return;

        if (netSession is GameNetworkManager networkManager)
            networkManager.UseRunner(runner);
    }
}
