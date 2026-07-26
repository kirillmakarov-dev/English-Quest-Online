using Fusion;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Registers the local player's <see cref="HealthComponent"/> with ServiceLocator
/// so HUD and other systems can resolve it without scene searches.
/// </summary>
[RequireComponent(typeof(HealthComponent))]
public class LocalPlayerHealthProvider : MonoBehaviour, ILocalPlayerHealth
{
    /// <summary>Fired when the local player's health provider registers with ServiceLocator.</summary>
    public static event System.Action<ILocalPlayerHealth> OnLocalHealthReady;

    [SerializeField] private HealthComponent _health;

    public HealthComponent Health => _health;

    private void Awake()
    {
        if (_health == null)
            _health = GetComponent<HealthComponent>();
    }

    private void Start()
    {
        if (!IsLocalPlayer()) return;

        ServiceLocator.ForSceneOf(this).Register<ILocalPlayerHealth>(this);
        OnLocalHealthReady?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (!IsLocalPlayer()) return;

        ServiceLocator.ForSceneOf(this).DeregisterIfRegistered<ILocalPlayerHealth>();
    }

    private bool IsLocalPlayer()
    {
        var networkObject = GetComponentInParent<NetworkObject>();
        return networkObject == null || networkObject.HasInputAuthority;
    }
}
