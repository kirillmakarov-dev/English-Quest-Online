using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class WorldMapDefinitionLoader
{
    public const string OpenWorldMapAssetPath = "Assets/_OurAssets/Data/WorldMap/OpenWorldMap.asset";

    private static readonly Dictionary<string, WorldMapDefinitionSO> _mapsById = new();
    private static WorldMapDefinitionSO _openWorldMap;

    public static void Register(WorldMapDefinitionSO map)
    {
        if (map == null || string.IsNullOrEmpty(map.MapId))
            return;

        _mapsById[map.MapId] = map;
    }

    public static void Initialize(WorldMapDefinitionSO openWorldMap)
    {
        _mapsById.Clear();
        _openWorldMap = openWorldMap;
        Register(openWorldMap);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Clears cached map registry between Play Mode tests so harness maps do not
    /// replace the production OpenWorld map for integration assertions.
    /// </summary>
    public static void ResetForPlayModeTests()
    {
        _mapsById.Clear();
        _openWorldMap = null;
    }
#endif

    public static WorldMapDefinitionSO ResolveByMapId(string mapId)
    {
        if (string.IsNullOrEmpty(mapId))
            return null;

        if (_mapsById.TryGetValue(mapId, out WorldMapDefinitionSO registered))
            return registered;

        WorldMapDefinitionSO map = LoadOpenWorldMap();
        if (map != null && map.MapId == mapId)
        {
            Register(map);
            return map;
        }

        return null;
    }

    public static WorldMapDefinitionSO LoadOpenWorldMap()
    {
        if (_openWorldMap != null)
            return _openWorldMap;

#if UNITY_EDITOR
        WorldMapDefinitionSO editorMap =
            AssetDatabase.LoadAssetAtPath<WorldMapDefinitionSO>(OpenWorldMapAssetPath);
        if (editorMap != null)
            Register(editorMap);
        return editorMap;
#else
        AppLog.Error(
            "[WorldMapDefinitionLoader] OpenWorldMap is not registered. " +
            "Assign it on WorldTravelBootstrap in the PreLoad scene.");
        return null;
#endif
    }
}
