using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class PlayerSpawnPoint : MonoBehaviour
{
    // Static list allows the GameManager or Spawner to find all points without FindObjectsOfType
    public static List<PlayerSpawnPoint> SpawnPoints = new List<PlayerSpawnPoint>();

    [SerializeField] private string _travelNodeId;

    public string TravelNodeId => _travelNodeId;

    public static void CollectSceneIssues(Scene scene, List<string> errors, List<string> warnings)
    {
        errors ??= new List<string>();
        warnings ??= new List<string>();

        List<PlayerSpawnPoint> scenePoints = GetPointsInScene(scene);
        if (scenePoints.Count == 0)
        {
            errors.Add($"Scene '{scene.name}' has no PlayerSpawnPoint components.");
            return;
        }

        if (scenePoints.Count < 2)
            warnings.Add($"Scene '{scene.name}' only has {scenePoints.Count} PlayerSpawnPoint. Two-player spawn coverage may be limited.");

        var duplicateNames = scenePoints
            .Where(point => point != null && !string.IsNullOrWhiteSpace(point.gameObject.name))
            .GroupBy(point => point.gameObject.name)
            .Where(group => group.Count() > 1);

        foreach (var duplicate in duplicateNames)
            warnings.Add($"Scene '{scene.name}' has duplicate PlayerSpawnPoint name '{duplicate.Key}'. Player ordering may become ambiguous.");

        var duplicateTravelIds = scenePoints
            .Where(point => point != null && !string.IsNullOrWhiteSpace(point._travelNodeId))
            .GroupBy(point => point._travelNodeId)
            .Where(group => group.Count() > 1);

        foreach (var duplicate in duplicateTravelIds)
            warnings.Add($"Scene '{scene.name}' has duplicate travel spawn id '{duplicate.Key}'.");
    }

    private void Awake()
    {
        SpawnPoints.Add(this);
    }

    private void OnDestroy()
    {
        SpawnPoints.Remove(this);
    }

    public static bool TryGetRandomSpawnPoint(out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (SpawnPoints.Count == 0)
        {
            AppLog.Warning("No PlayerSpawnPoints found in scene. Spawning at (0,0,0).");
            return false;
        }

        int index = Random.Range(0, SpawnPoints.Count);
        SpawnPoints[index].transform.GetPositionAndRotation(out position, out rotation);
        return true;
    }

    public static bool TryGetNearestSpawnPoint(Vector3 fromPosition, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        PlayerSpawnPoint nearest = null;
        float bestSqrDist = float.MaxValue;

        for (int i = 0; i < SpawnPoints.Count; i++)
        {
            PlayerSpawnPoint point = SpawnPoints[i];
            if (point == null) continue;

            float sqrDist = (point.transform.position - fromPosition).sqrMagnitude;
            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                nearest = point;
            }
        }

        if (nearest == null)
        {
            AppLog.Warning("No PlayerSpawnPoints found in scene. Spawning at (0,0,0).");
            return false;
        }

        nearest.transform.GetPositionAndRotation(out position, out rotation);
        return true;
    }

    public static bool TryGetSpawnForNode(string nodeId, out Vector3 position, out Quaternion rotation)
    {
        return TryGetSpawnForNode(nodeId, SceneManager.GetActiveScene(), out position, out rotation);
    }

    public static bool TryGetSpawnForNode(string nodeId, Scene scene, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (string.IsNullOrEmpty(nodeId))
            return false;

        for (int i = 0; i < SpawnPoints.Count; i++)
        {
            PlayerSpawnPoint point = SpawnPoints[i];
            if (!IsPointInScene(point, scene) || string.IsNullOrEmpty(point._travelNodeId))
                continue;

            if (point._travelNodeId == nodeId)
            {
                point.transform.GetPositionAndRotation(out position, out rotation);
                return true;
            }
        }

        return false;
    }

    public static bool TryGetRandomSpawnPoint(Scene scene, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        List<PlayerSpawnPoint> scenePoints = GetPointsInScene(scene);
        if (scenePoints.Count == 0)
            return false;

        int index = Random.Range(0, scenePoints.Count);
        scenePoints[index].transform.GetPositionAndRotation(out position, out rotation);
        return true;
    }

    public static bool TryGetSpawnPointForPlayer(
        Scene scene,
        int playerId,
        out Vector3 position,
        out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        List<PlayerSpawnPoint> scenePoints = GetPointsInScene(scene);
        if (scenePoints.Count == 0)
            return false;

        scenePoints.Sort((left, right) =>
            string.CompareOrdinal(left.gameObject.name, right.gameObject.name));

        int zeroBasedPlayerIndex = Mathf.Max(0, playerId - 1);
        PlayerSpawnPoint point = scenePoints[zeroBasedPlayerIndex % scenePoints.Count];
        point.transform.GetPositionAndRotation(out position, out rotation);
        return true;
    }

    /// <summary>
    /// Resolves a travel arrival position: tagged travel spawn first, then any spawn in the scene.
    /// </summary>
    public static bool TryResolveTravelSpawn(string nodeId, Scene scene, out Vector3 position, out Quaternion rotation)
    {
        if (TryGetSpawnForNode(nodeId, scene, out position, out rotation))
            return true;

        return TryGetRandomSpawnPoint(scene, out position, out rotation);
    }

    public static bool TryResolveTravelSpawn(string nodeId, out Vector3 position, out Quaternion rotation)
    {
        return TryResolveTravelSpawn(nodeId, SceneManager.GetActiveScene(), out position, out rotation);
    }

    public static async UniTask WaitForSpawnPointsInSceneAsync(Scene scene, int maxFrames = 120)
    {
        for (int frame = 0; frame < maxFrames; frame++)
        {
            if (HasSpawnPointsInScene(scene))
                return;

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        }

        AppLog.Warning($"[PlayerSpawnPoint] Timed out waiting for spawn points in scene '{scene.name}'.");
    }

    public static bool HasSpawnPointsInScene(Scene scene) => GetPointsInScene(scene).Count > 0;

    private static List<PlayerSpawnPoint> GetPointsInScene(Scene scene)
    {
        var scenePoints = new List<PlayerSpawnPoint>();

        for (int i = 0; i < SpawnPoints.Count; i++)
        {
            PlayerSpawnPoint point = SpawnPoints[i];
            if (IsPointInScene(point, scene))
                scenePoints.Add(point);
        }

        return scenePoints;
    }

    private static bool IsPointInScene(PlayerSpawnPoint point, Scene scene)
    {
        return point != null && point.gameObject.scene == scene;
    }

    // Visualize the spawn point in the editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.5f);

        // Draw forward direction
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
    }
}
