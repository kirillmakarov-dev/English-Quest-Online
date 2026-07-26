using System;
using Fusion;

/// <summary>
/// Abstraction over the networked bike mount state.
/// Decouples <see cref="BikeEntrySystem"/> and <see cref="BikeRiderVisualsController"/>
/// from the concrete Fusion implementation, enabling unit testing with stubs.
/// </summary>
public interface IBikeMount
{
    /// <summary>The player currently occupying the bike, or <see cref="PlayerRef.None"/>.</summary>
    PlayerRef RidingPlayer { get; }

    /// <summary>True only on the machine whose local player is the current rider.</summary>
    bool IsLocalPlayerRiding { get; }

    /// <summary>True when any player (local or remote) is riding.</summary>
    bool IsOccupied { get; }

    /// <summary>Fired when the riding player changes. Args: (previousRider, currentRider).</summary>
    event Action<PlayerRef, PlayerRef> OnRiderChanged;

    /// <summary>Fired locally when the local player becomes the rider.</summary>
    event Action OnLocalMounted;

    /// <summary>Fired locally when the local player stops being the rider.</summary>
    event Action OnLocalDismounted;

    void Mount();
    void Dismount();
}
