using System.Collections.Generic;

/// <summary>
/// Weighted random selection for <see cref="MonsterSpawnPoolSO"/> entries.
/// </summary>
public static class MonsterSpawnPoolSelector
{
    public static MonsterDefinitionSO Select(IReadOnlyList<MonsterSpawnPoolEntry> entries, System.Random rng)
    {
        if (entries == null || entries.Count == 0 || rng == null)
            return null;

        int totalWeight = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            MonsterSpawnPoolEntry entry = entries[i];
            if (entry?.monster != null && entry.weight > 0)
                totalWeight += entry.weight;
        }

        if (totalWeight <= 0)
            return null;

        int roll = rng.Next(0, totalWeight);
        int cumulative = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            MonsterSpawnPoolEntry entry = entries[i];
            if (entry?.monster == null || entry.weight <= 0)
                continue;

            cumulative += entry.weight;
            if (roll < cumulative)
                return entry.monster;
        }

        return null;
    }
}
