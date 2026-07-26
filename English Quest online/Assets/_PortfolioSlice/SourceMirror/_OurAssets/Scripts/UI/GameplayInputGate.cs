/// <summary>
/// Shared helper for hotkey-driven UI openers to respect modal gameplay input locks.
/// </summary>
public static class GameplayInputGate
{
    public static bool IsBlockedByAnother(object self, IPlayerLockSystem locks)
    {
        return locks.IsLockedByOther(PlayerLockSystem.LockType.GameplayInput, self);
    }
}
