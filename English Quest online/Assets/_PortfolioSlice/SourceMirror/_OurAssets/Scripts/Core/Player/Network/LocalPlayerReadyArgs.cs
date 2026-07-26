using Fusion;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;

public readonly struct LocalPlayerReadyArgs
{
    public NetworkRunner Runner { get; }
    public NetworkObject Player { get; }
    public Scene Scene { get; }
    public CinemachineCamera FollowCamera { get; }

    public LocalPlayerReadyArgs(
        NetworkRunner runner,
        NetworkObject player,
        Scene scene,
        CinemachineCamera followCamera)
    {
        Runner = runner;
        Player = player;
        Scene = scene;
        FollowCamera = followCamera;
    }

    public bool IsValid => Runner != null && Player != null && Player.IsValid && Scene.IsValid();
}
