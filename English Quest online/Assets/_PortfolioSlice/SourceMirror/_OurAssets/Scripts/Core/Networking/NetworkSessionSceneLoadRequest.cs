using Cysharp.Threading.Tasks;
using Fusion;

/// <summary>
/// Routes remote scene-load requests through <see cref="NetworkSessionBridge"/>.
/// </summary>
public static class NetworkSessionSceneLoadRequest
{
    public static bool TryRequest(NetworkRunner runner, int buildIndex)
    {
        if (runner == null || !runner.IsRunning || buildIndex < 0)
            return false;

        if (!NetworkSessionBridge.TryGetInstance(out NetworkSessionBridge bridge))
        {
            AppLog.Warning("[NetworkSessionSceneLoadRequest] NetworkSessionBridge is not available.");
            return false;
        }

        bridge.RequestSceneLoad(buildIndex);
        return true;
    }

    public static async UniTask<bool> RequestAndWaitForAckAsync(
        NetworkRunner runner,
        int buildIndex,
        float timeoutSeconds = FusionSceneFollowService.RemoteLoadAckTimeoutSeconds)
    {
        if (runner == null || !runner.IsRunning || buildIndex < 0)
            return false;

        var key = new TravelSceneLoadCoordinator.PendingKey(runner, buildIndex, runner.LocalPlayer);
        var tcs = new UniTaskCompletionSource<bool>();
        TravelSceneLoadCoordinator.RegisterPendingAck(key, tcs);

        try
        {
            if (!TryRequest(runner, buildIndex))
                return false;

            try
            {
                return await tcs.Task.Timeout(System.TimeSpan.FromSeconds(timeoutSeconds));
            }
            catch (System.TimeoutException)
            {
                AppLog.Warning(
                    $"[NetworkSessionSceneLoadRequest] Timed out waiting for scene load ack " +
                    $"(buildIndex={buildIndex}, runner='{runner.name}').");
                return false;
            }
        }
        finally
        {
            TravelSceneLoadCoordinator.UnregisterPendingAck(key);
        }
    }
}
