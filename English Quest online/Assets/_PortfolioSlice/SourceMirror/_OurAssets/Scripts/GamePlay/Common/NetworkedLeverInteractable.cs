using Fusion;
using UnityEngine;

/// <summary>
/// Add this alongside any LeverInteractableBase subclass to make the lever networked.
/// The master client activates normally; the Activated event triggers an RPC
/// that replicates the activation on all proxies.
/// Works with both LeverInteractable (one-shot) and ToggleLeverInteractable (toggle).
/// Without this component, either lever type operates fully locally per-player.
/// </summary>
public class NetworkedLeverInteractable : NetworkBehaviour
{
    private LeverInteractableBase _lever;

    public override void Spawned()
    {
        _lever = GetComponent<LeverInteractableBase>();
        _lever.Activated += OnLeverActivated;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_lever != null)
            _lever.Activated -= OnLeverActivated;
    }

    private void OnLeverActivated()
    {
        if (!Runner.IsSharedModeMasterClient) return;
        RPC_ActivateOnProxies();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    private void RPC_ActivateOnProxies()
    {
        _lever.Activate();
    }
}
