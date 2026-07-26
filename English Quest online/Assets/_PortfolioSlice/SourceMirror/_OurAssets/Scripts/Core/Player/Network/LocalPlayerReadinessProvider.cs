using System;
using Cysharp.Threading.Tasks;
using Fusion;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene-scoped readiness state for the local player. Registered on each scene's ServiceLocator.
/// </summary>
[DisallowMultipleComponent]
public sealed class LocalPlayerReadinessProvider : MonoBehaviour, ILocalPlayerReadiness
{
    public event Action<LocalPlayerReadyArgs> Ready;

    public Scene Scene => gameObject.scene;

    public bool IsReady => TryGet(out LocalPlayerReadyArgs args) && args.IsValid;

    private LocalPlayerReadyArgs _readyArgs;
    private bool _hasReadyArgs;

    public bool TryGet(out LocalPlayerReadyArgs args)
    {
        args = _readyArgs;
        return _hasReadyArgs;
    }

    public CinemachineCamera TryGetFollowCamera()
    {
        if (TryGet(out LocalPlayerReadyArgs args) && args.FollowCamera != null)
            return args.FollowCamera;

        return PlayerSceneContext.ResolveFollowCamera(Scene);
    }

    public async UniTask<LocalPlayerReadyArgs> WaitReadyAsync(int maxFrames = 600)
    {
        for (int frame = 0; frame < maxFrames; frame++)
        {
            if (TryGet(out LocalPlayerReadyArgs args) && args.IsValid)
                return args;

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        }

        AppLog.Warning("[LocalPlayerReadiness] Timed out waiting for local player ready.", this);
        return default;
    }

    public void NotifyReady(LocalPlayerReadyArgs args)
    {
        if (!args.IsValid)
            return;

        _readyArgs = args;
        _hasReadyArgs = true;
        Ready?.Invoke(args);
    }

    public void Clear()
    {
        _readyArgs = default;
        _hasReadyArgs = false;
    }

#if UNITY_EDITOR
    internal void NotifyReadyForTests(NetworkRunner runner, NetworkObject player = null)
    {
        _readyArgs = new LocalPlayerReadyArgs(runner, player, Scene, null);
        _hasReadyArgs = true;
    }
#endif
}
