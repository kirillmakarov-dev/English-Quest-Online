using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Spawns a pooled <see cref="DamageFloatingText"/> prefab (Feel MMF_Player sequence) above this entity on hit.
/// Subscribes to <see cref="HealthComponent.OnDamageTaken"/> — works on all clients via health RPCs.
/// </summary>
[RequireComponent(typeof(HealthComponent))]
public class DamageFloatingTextReaction : MonoBehaviour
{
    public const string DefaultTextPrefabPath =
        "Assets/_OurAssets/Art/Prefabs/GamePlay/Combat/DamageFloatingText.prefab";

    [SerializeField] private HealthComponent _health;
    [Tooltip("Drag DamageFloatingText.prefab from Assets/_OurAssets/Art/Prefabs/GamePlay/Combat/.")]
    [SerializeField] private DamageFloatingText _textPrefab;
    [SerializeField] private Transform _spawnAnchor;
    [SerializeField] private Vector3 _baseOffset = new(0f, 1.3f, 0f);

    [Header("Spawn Area")]
    [SerializeField] private Vector3 _spawnAreaMin = new(-0.4f, 0f, -0.4f);
    [SerializeField] private Vector3 _spawnAreaMax = new(0.4f, 0.3f, 0.4f);

    [Header("Pool")]
    [Tooltip("Initial pooled instances created up front for this prefab.")]
    [FormerlySerializedAs("_poolSize")]
    [SerializeField] private int _defaultCapacity = 10;
    [Tooltip("Hard cap before excess instances are destroyed instead of pooled.")]
    [SerializeField] private int _maxPoolSize = 32;
    [SerializeField] private Transform _poolRoot;

    private void Awake()
    {
        if (_health == null)
            _health = GetComponent<HealthComponent>();

        if (_spawnAnchor == null)
            _spawnAnchor = FindVisualAnchor(transform) ?? transform;

        if (_textPrefab == null)
            _textPrefab = LoadDefaultTextPrefab();
    }

    private void OnEnable()
    {
        if (_health == null) return;
        _health.OnDamageTaken += HandleDamageTaken;
    }

    private void OnDisable()
    {
        if (_health != null)
            _health.OnDamageTaken -= HandleDamageTaken;
    }

    private static DamageFloatingText LoadDefaultTextPrefab()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<DamageFloatingText>(DefaultTextPrefabPath);
#else
        return null;
#endif
    }

    private static Transform FindVisualAnchor(Transform start)
    {
        for (Transform current = start; current != null; current = current.parent)
        {
            Transform visual = current.Find("Visual");
            if (visual != null)
                return visual;

            Transform visualRoot = current.Find("VisualRoot");
            if (visualRoot != null)
                return visualRoot;
        }

        return null;
    }

    private void HandleDamageTaken(float amount, DamageInfo info)
    {
        if (amount <= 0f || _textPrefab == null) return;

        Vector3 randomOffset = new(
            Random.Range(_spawnAreaMin.x, _spawnAreaMax.x),
            Random.Range(_spawnAreaMin.y, _spawnAreaMax.y),
            Random.Range(_spawnAreaMin.z, _spawnAreaMax.z));

        Vector3 spawnPosition = _spawnAnchor.position + _baseOffset + randomOffset;
        int damage = Mathf.RoundToInt(amount);

        DamageFloatingTextPool.Spawn(
            _textPrefab,
            _defaultCapacity,
            _maxPoolSize,
            _poolRoot,
            damage,
            spawnPosition);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_textPrefab == null)
            _textPrefab = LoadDefaultTextPrefab();
    }
#endif
}
