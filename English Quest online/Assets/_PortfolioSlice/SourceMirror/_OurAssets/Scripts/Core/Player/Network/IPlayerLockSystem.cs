public interface IPlayerLockSystem
{
    void Lock(PlayerLockSystem.LockType type, object source);
    void Unlock(PlayerLockSystem.LockType type, object source);
    void Lock(object source, params PlayerLockSystem.LockType[] types);
    void Unlock(object source, params PlayerLockSystem.LockType[] types);
    bool IsLocked(PlayerLockSystem.LockType type);
    bool IsLockedByOther(PlayerLockSystem.LockType type, object self);
}
