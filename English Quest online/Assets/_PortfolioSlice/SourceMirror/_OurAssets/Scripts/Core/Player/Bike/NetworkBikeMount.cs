using System;
using Fusion;

/// <summary>
/// Tracks which player is currently riding this bike.
/// [Networked] PlayerRef is synced to all clients automatically.
/// Local events are fired in Render() so BikeCameraActivator / BikeEntrySystem
/// can react without polling every frame.
/// </summary>
public class NetworkBikeMount : NetworkBehaviour, IBikeMount
{
    // -------------------------------------------------------------------------
    // Networked state — one source of truth visible on every client.
    // PlayerRef.None means the bike is unoccupied.
    // -------------------------------------------------------------------------
    [Networked] public PlayerRef RidingPlayer { get; set; }

    private ChangeDetector _changes;

    // Pending-mount flag: set when Mount() is called before we have StateAuthority.
    // Committed in FixedUpdateNetwork once authority is transferred.
    private bool _mountRequested;

    // -------------------------------------------------------------------------
    // Events — fired locally so other scripts react without polling.
    // -------------------------------------------------------------------------
    public event Action<PlayerRef, PlayerRef> OnRiderChanged;
    public event Action OnLocalMounted;
    public event Action OnLocalDismounted;

    private PlayerRef _lastKnownRider;

    // -------------------------------------------------------------------------
    // Convenience helpers
    // -------------------------------------------------------------------------

    /// <summary>True only on the machine whose local player is riding.</summary>
    public bool IsLocalPlayerRiding => Runner != null && RidingPlayer == Runner.LocalPlayer;

    /// <summary>True when any player (remote or local) is currently riding.</summary>
    public bool IsOccupied => RidingPlayer != PlayerRef.None;

    // -------------------------------------------------------------------------
    // Fusion lifecycle
    // -------------------------------------------------------------------------

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _lastKnownRider = RidingPlayer;
    }

    public override void FixedUpdateNetwork()
    {
        // Once StateAuthority is transferred to us, commit the pending mount.
        // !IsOccupied guards against a race where two players both requested
        // authority simultaneously.
        if (_mountRequested && Object.HasStateAuthority && !IsOccupied)
        {
            _mountRequested = false;
            RidingPlayer = Runner.LocalPlayer;
        }
    }

    public override void Render()
    {
        foreach (var change in _changes.DetectChanges(this))
        {
            if (change == nameof(RidingPlayer))
            {
                PlayerRef previousRider = _lastKnownRider;
                PlayerRef currentRider = RidingPlayer;
                _lastKnownRider = currentRider;

                OnRiderChanged?.Invoke(previousRider, currentRider);

                if (Runner != null && currentRider == Runner.LocalPlayer && previousRider != Runner.LocalPlayer)
                    OnLocalMounted?.Invoke();
                else if (Runner != null && previousRider == Runner.LocalPlayer && currentRider != Runner.LocalPlayer)
                    OnLocalDismounted?.Invoke();
            }
        }
    }

    // -------------------------------------------------------------------------
    // Public API — called by BikeEntrySystem
    // -------------------------------------------------------------------------

    /// <summary>
    /// Claim StateAuthority over the bike and register the local player as rider.
    /// If authority is not yet held, the mount is committed on the next simulation
    /// tick after authority is granted (FixedUpdateNetwork).
    /// </summary>
    public void Mount()
    {
        if (Object.HasStateAuthority)
        {
            // Fast path: already owned (e.g. first mount after scene load).
            if (!IsOccupied)
                RidingPlayer = Runner.LocalPlayer;
        }
        else
        {
            // Slow path: request authority; FixedUpdateNetwork commits once granted.
            _mountRequested = true;
            Object.RequestStateAuthority();
        }
    }

    /// <summary>
    /// Clear the rider. StateAuthority stays with this client so a subsequent
    /// player can call RequestStateAuthority() to take it over.
    /// </summary>
    public void Dismount()
    {
        RidingPlayer = PlayerRef.None;
        _mountRequested = false;
    }
}
