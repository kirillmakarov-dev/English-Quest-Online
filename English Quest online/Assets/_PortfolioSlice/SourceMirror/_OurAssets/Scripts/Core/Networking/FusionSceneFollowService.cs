using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

/// <summary>
/// Loads a Fusion scene when the caller has authority, or waits for an authority-driven load.
/// Aligns level transitions with world-travel follow behavior.
/// </summary>
public static class FusionSceneFollowService
{
    public const float RemoteLoadAckTimeoutSeconds = 15f;

    public static async UniTask<bool> LoadOrWaitForSceneAsync(
        NetworkRunner runner,
        int buildIndex,
        PlayerInteraction sceneLoadRequester = null,
        string expectedSceneName = null,
        string expectedScenePath = null)
    {
        if (runner == null || !runner.IsRunning || buildIndex < 0)
            return false;

        if (SceneLoadHandle.CanLoadScene(runner))
        {
            return await FusionSceneTransitionService.LoadSceneAsync(
                runner,
                buildIndex,
                expectedSceneName,
                expectedScenePath);
        }

        if (sceneLoadRequester != null)
        {
            int sceneLoadVersionBefore = PlayerSpawnCoordinator.SceneLoadDoneVersion;
            bool requested = await TravelSceneLoadCoordinator.RequestSceneLoadAsync(
                sceneLoadRequester,
                buildIndex,
                RemoteLoadAckTimeoutSeconds);

            if (requested)
            {
                return await SceneLoadWaitUtility.ConfirmFusionDestinationReadyAsync(
                    runner,
                    buildIndex,
                    expectedSceneName,
                    expectedScenePath,
                    sceneLoadVersionBefore);
            }
        }
        else
        {
            TryRequestRemoteSceneLoadFromLocalPlayer(runner, buildIndex);
        }

        SceneLoadHandle waitHandle = SceneLoadHandle.BeginWaitForScene(
            buildIndex,
            runner,
            expectedSceneName,
            expectedScenePath);

        return waitHandle != null && await waitHandle.WaitUntilReadyAsync();
    }

    private static void TryRequestRemoteSceneLoadFromLocalPlayer(NetworkRunner runner, int buildIndex)
    {
        NetworkSessionSceneLoadRequest.TryRequest(runner, buildIndex);
    }
}
