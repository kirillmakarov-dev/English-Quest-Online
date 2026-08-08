using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public sealed class SceneCameraObstacleColliders : MonoBehaviour
{
    private static readonly string[] s_obstacleTokens =
    {
        "house",
        "building",
        "wall",
        "roof",
        "border",
        "boundary",
        "siding",
        "structure",
        "tower",
        "floor",
        "sidewalk",
        "door",
        "window",
        "pillar",
        "fence",
        "tent"
    };

    private const string CameraObstacleLayerName = "CameraObstacle";

    [SerializeField] private bool _includeInactiveObjects;
    [SerializeField, Min(0.01f)] private float _minimumMeshExtent = 0.15f;
    [SerializeField] private bool _logSummary;

    private bool _hasBuilt;

    private void Awake()
    {
        BuildOnce();
    }

    private void OnEnable()
    {
        BuildOnce();
    }

    public void BuildOnce()
    {
        if (_hasBuilt || !gameObject.scene.IsValid())
            return;

        _hasBuilt = true;
        int addedBoxColliders = 0;
        int cameraObstacleLayer = LayerMask.NameToLayer(CameraObstacleLayerName);
        if (cameraObstacleLayer < 0)
        {
            Debug.LogError(
                $"[SceneCameraObstacleColliders] Required layer '{CameraObstacleLayerName}' is missing.",
                this);
            return;
        }

        IgnorePhysicalCollisions(cameraObstacleLayer);

        GameObject[] roots = gameObject.scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            MeshFilter[] meshFilters = roots[i].GetComponentsInChildren<MeshFilter>(_includeInactiveObjects);
            for (int m = 0; m < meshFilters.Length; m++)
                AddCameraBlockerIfNeeded(meshFilters[m], cameraObstacleLayer, ref addedBoxColliders);
        }

        // Fusion can run client physics with transform synchronization disabled.
        Physics.SyncTransforms();

        if (_logSummary || Debug.isDebugBuild)
        {
            PhysicsScene physicsScene = gameObject.scene.GetPhysicsScene();
            Debug.Log(
                $"[SceneCameraObstacleColliders] Added {addedBoxColliders} camera blockers on layer " +
                $"'{CameraObstacleLayerName}' ({cameraObstacleLayer}) in scene '{gameObject.scene.name}' " +
                $"(handle {gameObject.scene.handle}, physics scene valid: {physicsScene.IsValid()}).",
                this);
        }
    }

    private void AddCameraBlockerIfNeeded(
        MeshFilter meshFilter,
        int cameraObstacleLayer,
        ref int addedBoxColliders)
    {
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;

        if (!IsCameraObstacle(meshFilter.transform))
            return;

        if (!IsPrimaryLod(meshFilter.transform))
            return;

        Renderer renderer = meshFilter.GetComponent<Renderer>();
        if (renderer == null || (!renderer.gameObject.activeInHierarchy && !_includeInactiveObjects))
            return;

        Bounds worldBounds = renderer.bounds;
        if (worldBounds.extents.magnitude < _minimumMeshExtent)
            return;

        GameObject blockerObject = new($"Camera Blocker {addedBoxColliders:0000} - {meshFilter.name}");
        blockerObject.layer = cameraObstacleLayer;
        blockerObject.transform.SetParent(transform, false);
        blockerObject.transform.SetPositionAndRotation(worldBounds.center, Quaternion.identity);
        blockerObject.transform.localScale = Vector3.one;

        BoxCollider boxCollider = blockerObject.AddComponent<BoxCollider>();
        boxCollider.center = Vector3.zero;
        Vector3 blockerSize = worldBounds.size;
        blockerSize.x = Mathf.Max(blockerSize.x, 0.05f);
        blockerSize.y = Mathf.Max(blockerSize.y, 0.05f);
        blockerSize.z = Mathf.Max(blockerSize.z, 0.05f);
        boxCollider.size = blockerSize;
        boxCollider.isTrigger = false;
        addedBoxColliders++;
    }

    private static bool IsPrimaryLod(Transform transform)
    {
        for (Transform current = transform; current != null; current = current.parent)
        {
            string objectName = current.name.ToLowerInvariant();
            int lodMarker = objectName.IndexOf("lod", System.StringComparison.Ordinal);
            if (lodMarker < 0)
                continue;

            int lodNumberIndex = lodMarker + 3;
            if (lodNumberIndex < objectName.Length && char.IsDigit(objectName[lodNumberIndex]))
                return objectName[lodNumberIndex] == '0';
        }

        return true;
    }

    private static bool IsCameraObstacle(Transform transform)
    {
        for (Transform current = transform; current != null; current = current.parent)
        {
            string objectName = current.name.ToLowerInvariant();
            for (int i = 0; i < s_obstacleTokens.Length; i++)
            {
                if (objectName.Contains(s_obstacleTokens[i]))
                    return true;
            }
        }

        return false;
    }

    private static void IgnorePhysicalCollisions(int cameraObstacleLayer)
    {
        for (int layer = 0; layer < 32; layer++)
            Physics.IgnoreLayerCollision(cameraObstacleLayer, layer, true);
    }
}
