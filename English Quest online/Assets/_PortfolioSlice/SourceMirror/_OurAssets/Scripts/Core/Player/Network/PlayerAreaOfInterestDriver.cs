using Fusion;
using UnityEngine;

/// <summary>
/// Registers the local player's area of interest each network tick (Shared Mode).
/// Requires Interest Management enabled in NetworkProjectConfig (ReplicationFeatures = 2).
/// </summary>
public class PlayerAreaOfInterestDriver : NetworkBehaviour
{
    [SerializeField] private float _radius = 35f;

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority)
            return;

        if (!Runner.Config.Simulation.AreaOfInterestEnabled)
            return;

        Runner.AddPlayerAreaOfInterest(Object.InputAuthority, transform.position, _radius);
    }
}
