using UnityEngine;

/// <summary>
/// Allows any system (abilities, buffs, etc.) to push animator parameters
/// without modifying PlayerAnimator. Register via PlayerAnimator.RegisterDriver.
/// </summary>
public interface IAnimatorDriver
{
    /// <summary>
    /// When true, PlayerAnimator will force the movement speed parameter to 0,
    /// preventing run/walk animations from playing while this driver is active.
    /// </summary>
    bool SuppressMovementSpeed { get; }

    /// <summary>
    /// Called whenever the active Animator is acquired or replaced (e.g. on form change).
    /// Use this to cache parameter hashes and presence flags.
    /// </summary>
    void OnAnimatorAcquired(Animator animator);

    /// <summary>
    /// Called every Render frame. Write animator parameters here.
    /// </summary>
    void Drive(Animator animator);
}
