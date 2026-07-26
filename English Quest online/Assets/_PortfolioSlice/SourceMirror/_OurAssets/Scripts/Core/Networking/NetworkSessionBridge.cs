using System;
using Cysharp.Threading.Tasks;
using Fusion;

/// <summary>
/// Networked bridge for session-level RPCs that MonoBehaviour managers cannot host directly.
/// Inspired by the Fusion Social Hub <c>App</c> pattern.
/// </summary>
public class NetworkSessionBridge : NetworkBehaviour
{
    private static NetworkSessionBridge s_instance;

    public static NetworkSessionBridge Instance => s_instance;

    public static bool TryGetInstance(out NetworkSessionBridge bridge)
    {
        bridge = s_instance;
        return bridge != null && bridge.Object != null && bridge.Object.IsValid;
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
            s_instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (s_instance == this)
            s_instance = null;
    }

    public void RequestSceneLoad(int buildIndex)
    {
        if (buildIndex < 0)
            return;

        RpcRequestSceneLoad(buildIndex);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RpcRequestSceneLoad(int buildIndex, RpcInfo info = default)
    {
        if (Runner == null || !Runner.IsRunning || !SceneLoadHandle.CanLoadScene(Runner))
        {
            if (info.Source.IsRealPlayer)
                RpcSceneLoadAck(buildIndex, false, info.Source);
            return;
        }

        HandleSceneLoadAsync(buildIndex, info.Source).Forget();
    }

    private async UniTaskVoid HandleSceneLoadAsync(int buildIndex, PlayerRef requester)
    {
        bool success = false;
        try
        {
            success = await FusionSceneTransitionService.LoadSceneAsync(Runner, buildIndex);
            if (!success)
                AppLog.Error($"[NetworkSessionBridge] Scene load failed for build index {buildIndex}.");
        }
        catch (Exception ex)
        {
            AppLog.Error($"[NetworkSessionBridge] Scene load exception for build index {buildIndex}: {ex.Message}");
        }
        finally
        {
            if (requester.IsRealPlayer)
                RpcSceneLoadAck(buildIndex, success, requester);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcSceneLoadAck(int buildIndex, bool success, PlayerRef requester, RpcInfo info = default)
    {
        if (Runner == null || Runner.LocalPlayer != requester)
            return;

        TravelSceneLoadCoordinator.NotifySceneLoadResult(Runner, buildIndex, requester, success);
    }
}
