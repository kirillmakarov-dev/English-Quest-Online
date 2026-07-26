using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WorldMapDefinition", menuName = ScriptableObjectMenuPaths.GameplayWorldTravel + "/Map Definition")]
public class WorldMapDefinitionSO : ScriptableObject
{
    [SerializeField] private string _mapId = "open_world";
    [SerializeField] private Sprite _mapImage;
    [SerializeField] private Sprite _vehicleIcon;
    [SerializeField] private List<WorldMapNodeData> _nodes = new();
    [SerializeField] private List<WorldMapRouteData> _routes = new();

    public string MapId => _mapId;
    public Sprite MapImage => _mapImage;
    public Sprite VehicleIcon => _vehicleIcon;
    public IReadOnlyList<WorldMapNodeData> Nodes => _nodes;
    public IReadOnlyList<WorldMapRouteData> Routes => _routes;

    public bool TryGetNode(string nodeId, out WorldMapNodeData node)
    {
        for (int i = 0; i < _nodes.Count; i++)
        {
            if (_nodes[i].id == nodeId)
            {
                node = _nodes[i];
                return true;
            }
        }

        node = default;
        return false;
    }

    public bool TryGetRoute(string fromNodeId, string toNodeId, out WorldMapRouteData route)
    {
        for (int i = 0; i < _routes.Count; i++)
        {
            WorldMapRouteData candidate = _routes[i];
            if (candidate.fromNodeId == fromNodeId && candidate.toNodeId == toNodeId)
            {
                route = candidate;
                return true;
            }
        }

        route = default;
        return false;
    }

    public IEnumerable<string> GetReachableNodeIds(string fromNodeId)
    {
        for (int i = 0; i < _routes.Count; i++)
        {
            if (_routes[i].fromNodeId == fromNodeId)
                yield return _routes[i].toNodeId;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        for (int i = 0; i < _nodes.Count; i++)
        {
            WorldMapNodeData node = _nodes[i];
            node.SyncSceneReference();
            _nodes[i] = node;
        }
    }
#endif
}

[Serializable]
public struct WorldMapNodeData
{
    public string id;
    public string displayName;
    [Range(0f, 1f)] public float normalizedX;
    [Range(0f, 1f)] public float normalizedY;
    public Sprite locationIcon;
    public WorldMapDestinationType destinationType;
    public SceneReference scene;
    public string spawnPointId;
    public Vector3 worldPosition;
    public Vector3 worldRotationEuler;
    public bool unlockedByDefault;
    public WorldTravelSessionMode sessionMode;
    public string sharedSessionName;
    public int sharedSessionMaxPlayers;

    public Vector2 NormalizedPosition => new(normalizedX, normalizedY);

#if UNITY_EDITOR
    public void SyncSceneReference()
    {
        scene.SyncFromAsset();
    }
#endif
}

[Serializable]
public struct WorldMapRouteData
{
    public string fromNodeId;
    public string toNodeId;
    public Vector2[] waypoints;
    public float travelDuration;

    public float DurationOrDefault => travelDuration > 0f ? travelDuration : 2f;
}
