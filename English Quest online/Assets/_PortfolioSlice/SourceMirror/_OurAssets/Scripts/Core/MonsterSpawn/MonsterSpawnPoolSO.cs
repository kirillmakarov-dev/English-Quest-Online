using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Weighted pool of monster definitions used by <see cref="MonsterSpawnPoint"/>.
/// </summary>
[CreateAssetMenu(
    menuName = ScriptableObjectMenuPaths.GameplayMonstersSpawnPool,
    fileName = "MonsterSpawnPool")]
public class MonsterSpawnPoolSO : ScriptableObject
{
    [SerializeField] private List<MonsterSpawnPoolEntry> _entries = new();

    public IReadOnlyList<MonsterSpawnPoolEntry> Entries => _entries;

    public MonsterDefinitionSO Select(System.Random rng = null)
    {
        rng ??= new System.Random();
        return MonsterSpawnPoolSelector.Select(_entries, rng);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            MonsterSpawnPoolEntry entry = _entries[i];
            if (entry == null)
                continue;

            if (entry.monster == null)
                Debug.LogWarning($"[MonsterSpawnPoolSO] Entry {i} in '{name}' has no monster assigned.", this);
            else if (entry.weight < 1)
                Debug.LogWarning($"[MonsterSpawnPoolSO] Entry {i} in '{name}' has weight below 1.", this);
        }
    }
#endif
}
