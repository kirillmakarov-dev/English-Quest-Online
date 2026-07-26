using System;
using UnityEngine;

[Serializable]
public class MonsterSpawnPoolEntry
{
    public MonsterDefinitionSO monster;
    [Min(1)] public int weight = 1;
}
