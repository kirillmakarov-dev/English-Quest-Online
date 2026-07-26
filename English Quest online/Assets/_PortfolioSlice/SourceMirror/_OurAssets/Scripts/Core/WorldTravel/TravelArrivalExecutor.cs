using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class TravelArrivalExecutor
{
    public static async UniTask TeleportToDestinationAsync(
        PlayerInteraction interactor,
        MonoBehaviour serviceContext,
        WorldMapNodeData destination)
    {
        NetworkRunner runner = TravelInteractorResolver.GetRunner(interactor);
        Scene destinationScene = ResolveDestinationScene(interactor, serviceContext, destination.scene.BuildIndex);
        if (!destinationScene.IsValid())
        {
            AppLog.Error("[WorldTravelService] Travel destination scene is not loaded.");
            PlayerTravelArrival.Clear(runner);
            return;
        }

        if (!PlayerSpawnPoint.HasSpawnPointsInScene(destinationScene))
            await PlayerSpawnPoint.WaitForSpawnPointsInSceneAsync(destinationScene, maxFrames: 30);

        NetworkObject playerObject = await ResolveLocalPlayerAsync(interactor, serviceContext, destinationScene);
        if (!PlayerArrivalUtility.IsUsablePlayerObject(playerObject, destinationScene))
        {
            AppLog.Error("[WorldTravelService] No local player object found for travel arrival.");
            PlayerTravelArrival.Clear(runner);
            return;
        }

        if (!playerObject.HasStateAuthority)
            return;

        if (PlayerArrivalUtility.TryApplyArrival(playerObject, destinationScene, destination))
            return;

        if (PlayerArrivalUtility.TryApplyDefaultSceneSpawn(playerObject, destinationScene))
            return;

        PlayerTravelArrival.Clear(runner);
        string spawnId = string.IsNullOrEmpty(destination.spawnPointId) ? destination.id : destination.spawnPointId;
        AppLog.Warning($"[WorldTravelService] Failed to place player at '{spawnId}' in scene '{destinationScene.name}'.");
    }

    private static Scene ResolveDestinationScene(
        PlayerInteraction interactor,
        MonoBehaviour serviceContext,
        int targetBuildIndex)
    {
        if (targetBuildIndex < 0)
            return default;

        string sceneName = SceneLoadWaitUtility.ResolveSceneName(targetBuildIndex);
        string scenePath = SceneLoadWaitUtility.ResolveScenePath(targetBuildIndex);
        TravelInteractorResolver.TryGetFusionRunner(interactor, serviceContext, out NetworkRunner runner);

        if (SceneLoadWaitUtility.TryResolveLoadedDestinationScene(
                targetBuildIndex,
                sceneName,
                scenePath,
                runner,
                out Scene destinationScene))
        {
            return destinationScene;
        }

        return default;
    }

    private static async UniTask<NetworkObject> ResolveLocalPlayerAsync(
        PlayerInteraction interactor,
        MonoBehaviour serviceContext,
        Scene destinationScene)
    {
        if (TravelInteractorResolver.TryGetFusionRunner(interactor, serviceContext, out NetworkRunner runner))
        {
            LocalPlayerReadiness.TryGet(runner, destinationScene, out ILocalPlayerReadiness readiness);
            LocalPlayerReadyArgs ready = default;

            if (readiness != null
                && readiness.TryGet(out ready)
                && PlayerArrivalUtility.IsUsablePlayerObject(ready.Player, destinationScene))
            {
                return ready.Player;
            }

            NetworkObject playerObject = runner.GetPlayerObject(runner.LocalPlayer);
            if (!PlayerArrivalUtility.IsUsablePlayerObject(playerObject, destinationScene))
            {
                AppLog.Info("[WorldTravelService] Waiting for local player ready after travel scene load.");
                if (readiness == null)
                    LocalPlayerReadiness.TryGet(runner, destinationScene, out readiness);

                ready = readiness != null
                    ? await readiness.WaitReadyAsync(maxFrames: 120)
                    : default;
                playerObject = ready.Player;
            }

            if (PlayerArrivalUtility.IsUsablePlayerObject(playerObject, destinationScene))
                return playerObject;
        }

        return TravelInteractorResolver.ResolvePlayerObject(interactor, serviceContext, destinationScene);
    }
}
