using Fusion;
using Unity.Cinemachine;

public class PlayerCamera : NetworkBehaviour
{
    public CinemachineCamera  vcam;

    public override void Spawned()
    {
        // If we have Input Authority (Local Player), we want the camera to follow us.
        if (Object.HasInputAuthority)
        {
            vcam.Priority = 10; // Ensure this camera takes priority over any others in the scene
            vcam.Follow = transform;
            vcam.LookAt = transform;
        }
        else
        {
            // IMPORTANT: For remote players (Proxies), we must lower the priority 
            // so the Cinemachine Brain does not track them instead of the local player.
            vcam.Priority = 0;
        }
    }
}
