using System;
using Fusion;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Binds the scene-level Cinemachine follow camera to the local player.
/// Camera resolution is scene-scoped via <see cref="PlayerSceneContext"/>.
/// </summary>
public static class PlayerSceneCamera
{
    public const int ActivePriority = 10;
    public const int InactivePriority = 0;

    public static CinemachineCamera ResolveFollowCamera(Scene scene)
    {
        return PlayerSceneContext.ResolveFollowCamera(scene);
    }

    public static CinemachineCamera ResolveFollowCamera(Scene scene, Transform preferredOwner)
    {
        if (preferredOwner != null)
        {
            CinemachineCamera[] ownedCameras = preferredOwner.GetComponentsInChildren<CinemachineCamera>(true);
            for (int i = 0; i < ownedCameras.Length; i++)
            {
                CinemachineCamera camera = ownedCameras[i];
                if (camera == null)
                    continue;

                if (camera.GetComponent<CinemachineThirdPersonFollow>() != null)
                    return camera;

                if (camera.GetComponent<CinemachineOrbitalFollow>() != null)
                    return camera;
            }

            for (int i = 0; i < ownedCameras.Length; i++)
            {
                if (ownedCameras[i] != null)
                    return ownedCameras[i];
            }
        }

        return ResolveFollowCamera(scene);
    }

    /// <summary>
    /// Gives each Fusion Multi-Peer runner its own Cinemachine output channel so brains
    /// do not all drive from the same highest-priority virtual camera.
    /// </summary>
    public static void ConfigureRunnerChannelIsolation(Scene scene, NetworkRunner runner)
    {
        if (!scene.IsValid() || runner == null || !runner.IsRunning)
            return;

        OutputChannels channel = ResolveRunnerOutputChannel(runner);

        Camera outputCamera = ResolveOutputCamera(scene);
        if (outputCamera != null)
        {
            if (outputCamera.TryGetComponent(out CinemachineBrain brain))
                brain.ChannelMask = channel;
        }

        ApplyOutputChannelToSceneCameras(scene, channel);
    }

    public static Camera ResolveOutputCamera(Scene scene)
    {
        if (!scene.IsValid())
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        Camera fallbackCamera = null;

        for (int i = 0; i < roots.Length; i++)
        {
            Camera[] cameras = roots[i].GetComponentsInChildren<Camera>(true);
            for (int c = 0; c < cameras.Length; c++)
            {
                Camera camera = cameras[c];
                if (camera == null || !camera.TryGetComponent(out CinemachineBrain _))
                    continue;

                if (fallbackCamera == null)
                    fallbackCamera = camera;

                if (camera.isActiveAndEnabled)
                    return camera;
            }
        }

        return fallbackCamera;
    }

    [Obsolete("Use PlayerSceneContext.ResolveFollowCamera or ILocalPlayerReadiness.TryGetFollowCamera instead.")]
    public static CinemachineCamera FindPlayerFollowCamera()
    {
        return ResolveFollowCamera(SceneManager.GetActiveScene());
    }

    public static void AssignFollow(CinemachineCamera cam, Transform target, Quaternion? facing = null)
    {
        if (cam == null || target == null)
            return;

        cam.Follow = target;
        cam.LookAt = target;
        cam.Priority = ActivePriority;
        FusionPhysicsSceneCameraCollision.EnsureOn(cam);

        if (facing.HasValue)
            AlignOrbit(cam, facing.Value);
    }

    public static void AssignFollow(Transform target, Quaternion? facing = null)
    {
        Scene scene = target != null ? target.gameObject.scene : SceneManager.GetActiveScene();
        AssignFollow(ResolveFollowCamera(scene), target, facing);
    }

    public static void AlignOrbit(Quaternion facing)
    {
        AlignOrbit(ResolveFollowCamera(SceneManager.GetActiveScene()), facing);
    }

    public static void AlignOrbit(CinemachineCamera cam, Quaternion facing)
    {
        if (cam == null)
            return;

        CinemachineOrbitalFollow orbital = cam.GetComponent<CinemachineOrbitalFollow>();
        if (orbital == null)
            return;

        Vector3 forward = facing * Vector3.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            return;

        float playerYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        orbital.HorizontalAxis.Value = playerYaw;
    }

    private static OutputChannels ResolveRunnerOutputChannel(NetworkRunner runner)
    {
        int index = 0;
        foreach (NetworkRunner instance in NetworkRunner.Instances)
        {
            if (instance == null || !instance.IsRunning)
                continue;

            if (instance == runner)
                return (OutputChannels)(1 << index);

            index++;
            if (index > 15)
                break;
        }

        return OutputChannels.Default;
    }

    private static void ApplyOutputChannelToSceneCameras(Scene scene, OutputChannels channel)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            CinemachineCamera[] cameras = roots[i].GetComponentsInChildren<CinemachineCamera>(true);
            for (int c = 0; c < cameras.Length; c++)
                cameras[c].OutputChannel = channel;
        }
    }
}
