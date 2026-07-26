using Fusion;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A reusable NetworkBehaviour to trigger UnityEvents for all players in a Fusion Shared Mode room.
/// Call TriggerEvent() locally (e.g., from UI buttons or other local scripts) to broadcast the event over the network.
/// </summary>
public class NetworkEventRelay : NetworkBehaviour
{
    [Header("Collision Setup")]
    [Tooltip("If enabled, colliding with a player will automatically trigger the network event.")]
    [SerializeField] private bool _triggerOnPlayerCollision = false;

    [Tooltip("The tag used to identify player objects.")]
    [SerializeField] private string _playerTag = "Player";

    [Space]
    [Tooltip("If enabled, the event can only be triggered once.")]
    [SerializeField] private bool _runOnce = false;

    [Space]
    [Tooltip("The event that will be executed for ALL players when TriggerEvent is called.")]
    [SerializeField] private UnityEvent _onNetworkEventTriggered;

    [Networked, OnChangedRender(nameof(OnNetworkedTriggeredChanged))]
    private NetworkBool NetworkedTriggered { get; set; }

    public override void Spawned()
    {
        if (NetworkedTriggered)
            _onNetworkEventTriggered?.Invoke();
    }

    /// <summary>
    /// Call this method to tell all connected players (including yourself) to invoke the UnityEvent above.
    /// You can link this to UI Button OnClick events or other standard local UnityEvents.
    /// </summary>
    public void TriggerEvent()
    {
        if (_runOnce && NetworkedTriggered)
            return;

        if (Object != null && Object.IsValid)
        {
            if (HasStateAuthority)
                SetTriggered();
            else
                RPC_RequestTrigger();
        }
        else
        {
            // Fallback for offline mode / editor testing
            _onNetworkEventTriggered?.Invoke();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestTrigger()
    {
        SetTriggered();
    }

    private void SetTriggered()
    {
        if (_runOnce && NetworkedTriggered)
            return;

        NetworkedTriggered = true;
    }

    private void OnNetworkedTriggeredChanged()
    {
        if (NetworkedTriggered)
            _onNetworkEventTriggered?.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggerOnPlayerCollision && other.CompareTag(_playerTag))
        {
            if (TryGetComponent<BoxCollider>(out _))
                TriggerEvent();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_triggerOnPlayerCollision && collision.gameObject.CompareTag(_playerTag))
        {
            if (TryGetComponent<BoxCollider>(out _))
                TriggerEvent();
        }
    }
}
