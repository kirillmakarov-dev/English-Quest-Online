using System;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;

public interface ILocalPlayerReadiness
{
    event Action<LocalPlayerReadyArgs> Ready;

    Scene Scene { get; }

    bool IsReady { get; }

    bool TryGet(out LocalPlayerReadyArgs args);

    CinemachineCamera TryGetFollowCamera();

    UniTask<LocalPlayerReadyArgs> WaitReadyAsync(int maxFrames = 600);

    void NotifyReady(LocalPlayerReadyArgs args);

    void Clear();
}
