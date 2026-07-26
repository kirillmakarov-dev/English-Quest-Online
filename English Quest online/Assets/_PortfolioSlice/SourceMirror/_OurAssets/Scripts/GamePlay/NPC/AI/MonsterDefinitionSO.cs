using UnityEngine;

/// <summary>
/// Bundles spawn and tuning references for a monster archetype.
/// Behaviour and health SOs remain the edit surface for balance; this asset is the registry entry
/// used by <see cref="MonsterSpawner"/> and encounter catalogs.
/// </summary>
[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.GameplayMonsters + "/Monster Definition", fileName = "MonsterDefinition")]
public class MonsterDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Stable persistence key, e.g. \"chest_monster\". Do not change after release.")]
    [SerializeField] private string _monsterId;

    [SerializeField] private string _displayName;
    [SerializeField] private MonsterBehaviorConfigSO _behaviour;
    [SerializeField] private HealthStatsSO _health;
    [SerializeField] private GameObject _networkedPrefab;
    [SerializeField] private GameObject _localPrefab;

    public string MonsterId => _monsterId;
    public string DisplayName => _displayName;
    public MonsterBehaviorConfigSO Behaviour => _behaviour;
    public HealthStatsSO Health => _health;
    public GameObject NetworkedPrefab => _networkedPrefab;
    public GameObject LocalPrefab => _localPrefab;

    public bool HasNetworkedPrefab => _networkedPrefab != null;
    public bool HasLocalPrefab => _localPrefab != null;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(_monsterId))
            Debug.LogWarning($"[MonsterDefinitionSO] '{name}' is missing monsterId.", this);
    }
#endif
}
