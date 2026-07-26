using UnityEngine;

/// <summary>
/// Ensures world travel services and map UI exist after PreLoad boots.
/// Attach to a GameObject in PreLoad alongside other DDOL managers.
/// </summary>
public class WorldTravelBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject _worldMapCanvasPrefab;
    [SerializeField] private WorldMapDefinitionSO _openWorldMap;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (_openWorldMap == null)
            _openWorldMap = WorldMapDefinitionLoader.LoadOpenWorldMap();

        if (_openWorldMap != null)
            WorldMapDefinitionLoader.Initialize(_openWorldMap);
        else
            AppLog.Error("[WorldTravelBootstrap] OpenWorldMap asset is not assigned.");

        if (FindFirstObjectByType<WorldMapUI>(FindObjectsInactive.Include) == null)
        {
            if (_worldMapCanvasPrefab != null)
            {
                GameObject canvas = Instantiate(_worldMapCanvasPrefab, transform);
                canvas.name = "WorldMapCanvas";
            }
            else
            {
                AppLog.Error("[WorldTravelBootstrap] WorldMapCanvas prefab is not assigned.");
            }
        }

        if (GetComponent<NetworkTravelCoordinator>() == null)
            gameObject.AddComponent<NetworkTravelCoordinator>();

        if (GetComponent<WorldTravelService>() == null)
            gameObject.AddComponent<WorldTravelService>();
    }
}
