using Cysharp.Threading.Tasks;

using Fusion;

using Fusion.Sockets;

using System;

using UnityEngine;

using UnityEngine.SceneManagement;

using UnityServiceLocator;

using EnglishQuest.UI.Loading;



/// <summary>

/// Central Fusion disconnect/shutdown handling: clears travel state and surfaces unexpected disconnects.

/// </summary>

public class NetworkLifecycleHandler : MonoBehaviour

{

    public event Action<NetworkRunner, ShutdownReason> UnexpectedShutdown;

    public event Action<NetworkRunner, NetConnectFailedReason> ConnectFailed;



    private static NetworkLifecycleHandler s_instance;



    private void Awake()

    {

        if (s_instance != null && s_instance != this)

        {

            Destroy(this);

            return;

        }



        s_instance = this;

        SubscribeToCallbackHub();

    }



    private void OnDestroy()

    {

        UnsubscribeFromCallbackHub();



        if (s_instance == this)

            s_instance = null;

    }



    private void SubscribeToCallbackHub()

    {

        NetworkRunnerCallbackHub.Instance.Shutdown += HandleShutdown;

        NetworkRunnerCallbackHub.Instance.DisconnectedFromServer += HandleDisconnectedFromServer;

        NetworkRunnerCallbackHub.Instance.ConnectFailed += HandleConnectFailed;

    }



    private void UnsubscribeFromCallbackHub()

    {

        NetworkRunnerCallbackHub.Instance.Shutdown -= HandleShutdown;

        NetworkRunnerCallbackHub.Instance.DisconnectedFromServer -= HandleDisconnectedFromServer;

        NetworkRunnerCallbackHub.Instance.ConnectFailed -= HandleConnectFailed;

    }



    private void HandleShutdown(NetworkRunner runner, ShutdownReason shutdownReason)

    {

        CleanupRunnerState(runner);



        bool isLastRunner = NetworkRunner.Instances.Count == 0;



        if (shutdownReason != ShutdownReason.Ok)

        {

            AppLog.Warning($"[NetworkLifecycleHandler] Runner '{runner?.name}' shutdown: {shutdownReason}");

            UnexpectedShutdown?.Invoke(runner, shutdownReason);

        }

        else if (isLastRunner)

        {

            TryGracefulReturnToMenu();

        }

    }



    private void HandleDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)

    {

        AppLog.Warning($"[NetworkLifecycleHandler] Disconnected from server on '{runner?.name}': {reason}");

        CleanupRunnerState(runner);

    }



    private void HandleConnectFailed(

        NetworkRunner runner,

        NetAddress remoteAddress,

        NetConnectFailedReason reason)

    {

        NetworkSessionErrorService.ReportConnectFailed(reason);

        ConnectFailed?.Invoke(runner, reason);

    }



    private static void CleanupRunnerState(NetworkRunner runner)

    {

        FusionSceneTransitionService.ClearRunner(runner);

        PlayerTravelArrival.Clear(runner);

        PartyTravelPresentationGate.ClearAll();

        TravelSceneLoadCoordinator.ClearRunner(runner);

        SceneTransitionState.ClearRunner(runner);

        LocalPlayerReadiness.Clear(runner);

        NetworkTravelStateCleanup.ClearTravelState(runner);

    }



    private static void TryGracefulReturnToMenu()

    {

        if (NetworkLevelManager.IsReturningToMenu)

            return;



        MonoBehaviour host = s_instance != null ? s_instance : null;

        if (host == null)

            return;



        ServiceLocator locator = ServiceLocator.For(host);

        if (locator != null && locator.TryGet<ILevelManager>(out ILevelManager levelManager))

        {

            levelManager.ReturnToMenu();

            return;

        }



        ReturnToMenuWithoutLevelManager(host).Forget();

    }



    private static async UniTaskVoid ReturnToMenuWithoutLevelManager(MonoBehaviour host)

    {

        if (host == null)

            return;



        ServiceLocator locator = ServiceLocator.For(host);

        if (locator != null && locator.TryGet<ILoadingScreenService>(out ILoadingScreenService loadingScreen))

        {

            await loadingScreen.LoadSceneNetworkAsync(async () =>

            {

                if (locator.TryGet<INetworkSessionService>(out INetworkSessionService netSession))

                    await netSession.Disconnect();



                SceneManager.LoadScene("Menu");

            });

            return;

        }



        if (locator != null && locator.TryGet<INetworkSessionService>(out INetworkSessionService session))

            await session.Disconnect();



        SceneManager.LoadScene("Menu");

    }

}



/// <summary>

/// Clears world-travel state when a Fusion runner shuts down unexpectedly.

/// </summary>

internal static class NetworkTravelStateCleanup

{

    public static void ClearTravelState(NetworkRunner runner)

    {

        if (runner == null)

            return;



        WorldTravelService[] services = UnityEngine.Object.FindObjectsByType<WorldTravelService>(

            FindObjectsInactive.Include,

            FindObjectsSortMode.None);



        for (int i = 0; i < services.Length; i++)

            services[i]?.HandleRunnerDisconnected(runner);

    }

}



