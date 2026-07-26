/// <summary>
/// Scene service that owns MapleStory-style spawn point lifecycle and respawn timers.
/// </summary>
public interface IMonsterSpawnService
{
    void RegisterPoint(MonsterSpawnPoint point);
    void UnregisterPoint(MonsterSpawnPoint point);
    void NotifyMobDied(MonsterSpawnPoint point, MonsterSpawnInstance instance);
}
