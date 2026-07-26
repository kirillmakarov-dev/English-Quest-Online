using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SurfaceDefinition", menuName = ScriptableObjectMenuPaths.CoreAudio + "/Surface Definition")]
public class SurfaceDefinition : ScriptableObject
{
    [Serializable]
    public struct SurfaceEntry
    {
        public string Tag;
        public SurfaceType SurfaceType;
    }

    [SerializeField] private List<SurfaceEntry> _entries = new List<SurfaceEntry>();
    
    // Cache for faster lookups
    private Dictionary<string, SurfaceType> _tagToSurfaceMap;

    private void InitializeMap()
    {
        _tagToSurfaceMap = new Dictionary<string, SurfaceType>();
        foreach (var entry in _entries)
        {
            if (!_tagToSurfaceMap.ContainsKey(entry.Tag))
            {
                _tagToSurfaceMap.Add(entry.Tag, entry.SurfaceType);
            }
        }
    }

    public SurfaceType GetSurfaceType(string tag)
    {
        if (_tagToSurfaceMap == null)
        {
            InitializeMap();
        }

        if (_tagToSurfaceMap.TryGetValue(tag, out SurfaceType type))
        {
            return type;
        }

        return SurfaceType.Default;
    }
}
