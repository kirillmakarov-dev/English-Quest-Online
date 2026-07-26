#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class MonsterSpawnPoolValidator
{
    [MenuItem("Tools/English Kingdom/Monsters/Validate Spawn Pools")]
    public static void ValidateFromMenu()
    {
        int issueCount = ValidateAll(logEachIssue: true);
        Debug.Log($"[MonsterSpawnPoolValidator] Validation finished with {issueCount} issue(s).");
    }

    public static int ValidateAll(bool logEachIssue)
    {
        string[] guids = AssetDatabase.FindAssets("t:MonsterSpawnPoolSO");
        int issueCount = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var pool = AssetDatabase.LoadAssetAtPath<MonsterSpawnPoolSO>(path);
            if (pool == null)
                continue;

            issueCount += ValidatePool(pool, logEachIssue);
        }

        return issueCount;
    }

    private static int ValidatePool(MonsterSpawnPoolSO pool, bool logEachIssue)
    {
        int issues = 0;
        var entries = pool.Entries;

        if (entries == null || entries.Count == 0)
        {
            LogIssue(pool, "Pool has no entries.", logEachIssue);
            return 1;
        }

        int validEntries = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            MonsterSpawnPoolEntry entry = entries[i];
            if (entry == null)
            {
                LogIssue(pool, $"Entry {i} is null.", logEachIssue);
                issues++;
                continue;
            }

            if (entry.monster == null)
            {
                LogIssue(pool, $"Entry {i} has no monster definition.", logEachIssue);
                issues++;
                continue;
            }

            if (entry.weight < 1)
            {
                LogIssue(pool, $"Entry {i} weight must be at least 1.", logEachIssue);
                issues++;
                continue;
            }

            if (!entry.monster.HasNetworkedPrefab && !entry.monster.HasLocalPrefab)
            {
                LogIssue(pool, $"Entry {i} monster '{entry.monster.name}' has no prefab assigned.", logEachIssue);
                issues++;
                continue;
            }

            validEntries++;
        }

        if (validEntries == 0)
        {
            LogIssue(pool, "Pool has no valid weighted entries.", logEachIssue);
            issues++;
        }

        return issues;
    }

    private static void LogIssue(Object context, string message, bool logEachIssue)
    {
        if (logEachIssue)
            Debug.LogWarning($"[MonsterSpawnPoolValidator] {message}", context);
    }
}
#endif
