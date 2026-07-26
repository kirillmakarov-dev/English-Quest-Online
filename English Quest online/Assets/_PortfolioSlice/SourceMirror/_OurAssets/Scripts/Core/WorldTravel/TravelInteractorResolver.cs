using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityServiceLocator;

internal static class TravelInteractorResolver
{
    public static NetworkRunner GetRunner(PlayerInteraction interactor)
    {
        if (interactor?.Object == null)
            return null;

        NetworkRunner runner = interactor.Object.Runner;
        return runner != null && runner.IsRunning ? runner : null;
    }

    public static bool TryGetFusionRunner(
        PlayerInteraction interactor,
        MonoBehaviour serviceContext,
        out NetworkRunner runner)
    {
        runner = GetRunner(interactor);
        if (runner != null)
            return true;

        if (serviceContext != null
            && ServiceLocator.For(serviceContext).TryGet<INetworkSessionService>(out var netSession)
            && netSession.Runner != null
            && netSession.Runner.IsRunning)
        {
            runner = netSession.Runner;
            return true;
        }

        return false;
    }

    public static NetworkObject ResolvePlayerObject(
        PlayerInteraction interactor,
        MonoBehaviour serviceContext,
        Scene activeScene)
    {
        if (interactor != null)
        {
            NetworkObject fromInteractor = interactor.GetComponent<NetworkObject>()
                ?? interactor.GetComponentInParent<NetworkObject>();
            if (PlayerArrivalUtility.IsUsablePlayerObject(fromInteractor, activeScene))
                return fromInteractor;
        }

        if (TryGetFusionRunner(interactor, serviceContext, out NetworkRunner runner))
        {
            NetworkObject fromRunner = runner.GetPlayerObject(runner.LocalPlayer);
            if (PlayerArrivalUtility.IsUsablePlayerObject(fromRunner, activeScene))
                return fromRunner;
        }

        return null;
    }
}
