using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene marker for a MapleStory-style monster spawn point. Delegates lifecycle to <see cref="MonsterSpawnDirector"/>.
/// </summary>
public class MonsterSpawnPoint : MonoBehaviour
{
    public static readonly List<MonsterSpawnPoint> RegisteredPoints = new();

    [SerializeField] private MonsterSpawnPoolSO _pool;
    [SerializeField] private int _maxPopulation = 1;
    [SerializeField] private float _respawnDelaySeconds = 8f;
    [SerializeField] private float _spawnRadius;
    [SerializeField] private bool _spawnOnEnable = true;
    [SerializeField] private string _spawnPointId;

    public MonsterSpawnPoolSO Pool => _pool;
    public int MaxPopulation => Mathf.Max(1, _maxPopulation);
    public float RespawnDelaySeconds => Mathf.Max(0f, _respawnDelaySeconds);
    public float SpawnRadius => Mathf.Max(0f, _spawnRadius);
    public bool SpawnOnEnable => _spawnOnEnable;
    public string SpawnPointId => _spawnPointId;

    private void OnEnable()
    {
        if (!RegisteredPoints.Contains(this))
            RegisteredPoints.Add(this);

        TryRegisterWithDirector();
    }

    private void Start()
    {
        TryRegisterWithDirector();
    }

    private void OnDisable()
    {
        RegisteredPoints.Remove(this);

        if (UnityServiceLocator.ServiceLocator.For(this).TryGet<IMonsterSpawnService>(out IMonsterSpawnService service))
            service.UnregisterPoint(this);
    }

    public Vector3 GetSpawnPosition()
    {
        Vector3 position = transform.position;
        if (SpawnRadius <= 0f)
            return position;

        Vector2 offset = Random.insideUnitCircle * SpawnRadius;
        return position + new Vector3(offset.x, 0f, offset.y);
    }

    public Quaternion GetSpawnRotation() => transform.rotation;

    public static IReadOnlyList<MonsterSpawnPoint> GetPointsInScene(Scene scene)
    {
        var result = new List<MonsterSpawnPoint>();
        for (int i = 0; i < RegisteredPoints.Count; i++)
        {
            MonsterSpawnPoint point = RegisteredPoints[i];
            if (point != null && point.gameObject.scene == scene)
                result.Add(point);
        }

        return result;
    }

    private void TryRegisterWithDirector()
    {
        if (!isActiveAndEnabled)
            return;

        if (UnityServiceLocator.ServiceLocator.For(this).TryGet<IMonsterSpawnService>(out IMonsterSpawnService service))
            service.RegisterPoint(this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.55f);
        Gizmos.DrawWireSphere(transform.position, SpawnRadius > 0f ? SpawnRadius : 0.5f);

        if (_pool != null)
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, $"{_pool.name} ({MaxPopulation})");
    }
#endif
}
