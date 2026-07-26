using Cysharp.Threading.Tasks;
using Fusion;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayerArrivalUtility
{
    public static async UniTask<NetworkObject> WaitForLocalPlayerAsync(
        NetworkRunner runner,
        Scene scene,
        int maxFrames = 600)
    {
        if (runner == null || !runner.IsRunning)
            return null;

        for (int frame = 0; frame < maxFrames; frame++)
        {
            NetworkObject playerObject = runner.GetPlayerObject(runner.LocalPlayer);
            if (IsUsablePlayerObject(playerObject, scene))
                return playerObject;

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        }

        AppLog.Warning("[PlayerArrivalUtility] Timed out waiting for local player object after scene load.");
        return null;
    }

    public static async UniTask WaitForFusionSceneLoadDoneAsync(int versionBeforeLoad, int maxFrames = 600)
    {
        for (int frame = 0; frame < maxFrames; frame++)
        {
            if (PlayerSpawnCoordinator.SceneLoadDoneVersion > versionBeforeLoad)
                return;

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        }

        AppLog.Warning("[PlayerArrivalUtility] Timed out waiting for Fusion scene load to finish.");
    }

    public static bool TryResolveSpawnTransform(
        NetworkRunner runner,
        Scene scene,
        bool consumePendingTravel,
        out Vector3 position,
        out Quaternion rotation)
    {
        if (consumePendingTravel
            && PlayerTravelArrival.TryConsumeForScene(runner, scene, out PlayerTravelArrival.PendingArrival arrival)
            && PlayerTravelArrival.TryResolveTransform(arrival, scene, out position, out rotation))
        {
            return true;
        }

        if (!consumePendingTravel
            && PlayerTravelArrival.TryPeekForScene(runner, scene, out PlayerTravelArrival.PendingArrival peekedArrival)
            && PlayerTravelArrival.TryResolveTransform(peekedArrival, scene, out position, out rotation))
        {
            return true;
        }

        return PlayerSpawnPoint.TryGetRandomSpawnPoint(scene, out position, out rotation);
    }

    public static bool TryApplyArrival(NetworkObject playerObject, Scene scene, in WorldMapNodeData destination)
    {
        if (playerObject == null || !playerObject.IsValid)
            return false;

        NetworkCharacterController controller = playerObject.GetComponent<NetworkCharacterController>();
        if (controller == null)
            return false;

        if (playerObject.HasStateAuthority == false)
            return false;

        string spawnId = string.IsNullOrEmpty(destination.spawnPointId) ? destination.id : destination.spawnPointId;
        if (!TryResolveTransform(spawnId, scene, destination, out Vector3 position, out Quaternion rotation))
            return false;

        ApplyTransform(controller, playerObject, position, rotation);
        AppLog.Info($"[PlayerArrivalUtility] Placed player at '{spawnId}' ({position}).");
        return true;
    }

    public static bool TryApplyArrival(NetworkObject playerObject, Scene scene, in PlayerTravelArrival.PendingArrival arrival)
    {
        if (playerObject == null || !playerObject.IsValid)
            return false;

        NetworkCharacterController controller = playerObject.GetComponent<NetworkCharacterController>();
        if (controller == null)
            return false;

        if (playerObject.HasStateAuthority == false)
            return false;

        if (!PlayerTravelArrival.TryResolveTransform(arrival, scene, out Vector3 position, out Quaternion rotation))
            return false;

        ApplyTransform(controller, playerObject, position, rotation);
        AppLog.Info($"[PlayerArrivalUtility] Placed player for travel node '{arrival.SpawnNodeId}' ({position}).");
        return true;
    }

    public static bool TryApplyDefaultSceneSpawn(NetworkObject playerObject, Scene scene)
    {
        if (playerObject == null || !playerObject.IsValid || !playerObject.HasStateAuthority)
            return false;

        NetworkCharacterController controller = playerObject.GetComponent<NetworkCharacterController>();
        if (controller == null)
            return false;

        if (!PlayerSpawnPoint.TryGetRandomSpawnPoint(scene, out Vector3 position, out Quaternion rotation))
            return false;

        ApplyTransform(controller, playerObject, position, rotation);
        AppLog.Info($"[PlayerArrivalUtility] Placed player at scene spawn ({position}).");
        return true;
    }

    private static bool TryResolveTransform(
        string spawnId,
        Scene scene,
        in WorldMapNodeData destination,
        out Vector3 position,
        out Quaternion rotation)
    {
        if (PlayerSpawnPoint.TryResolveTravelSpawn(spawnId, scene, out position, out rotation))
            return true;

        if (destination.worldPosition != Vector3.zero)
        {
            position = destination.worldPosition;
            rotation = Quaternion.Euler(destination.worldRotationEuler);
            return true;
        }

        position = Vector3.zero;
        rotation = Quaternion.identity;
        return false;
    }

    private static void ApplyTransform(
        NetworkCharacterController controller,
        NetworkObject playerObject,
        Vector3 position,
        Quaternion rotation)
    {
        controller.Teleport(position, rotation);
        controller.Velocity = Vector3.zero;

        if (playerObject.HasInputAuthority)
        {
            Scene scene = playerObject.gameObject.scene;
            CinemachineCamera followCamera = PlayerSceneContext.ResolveFollowCamera(scene);
            PlayerSceneCamera.AssignFollow(followCamera, playerObject.transform, rotation);
            PlayerSceneCamera.AlignOrbit(followCamera, rotation);
        }
    }

    public static bool IsUsablePlayerObject(NetworkObject playerObject, Scene? requiredScene = null)
    {
        if (playerObject == null || !playerObject.IsValid)
            return false;

        if (!requiredScene.HasValue)
            return true;

        Scene scene = requiredScene.Value;
        return scene.IsValid() && playerObject.gameObject.scene == scene;
    }

    public static void ClearStalePlayerObject(NetworkRunner runner, PlayerRef player, Scene scene)
    {
        if (runner == null || !runner.IsRunning)
            return;

        NetworkObject existingObject = runner.GetPlayerObject(player);
        if (existingObject == null)
            return;

        if (IsUsablePlayerObject(existingObject, scene))
            return;

        runner.SetPlayerObject(player, null);
    }
}
