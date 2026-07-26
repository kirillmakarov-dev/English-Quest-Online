using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FootstepCollection", menuName = ScriptableObjectMenuPaths.CoreAudio + "/Footstep Collection")]
public class FootstepCollection : ScriptableObject
{
    [Serializable]
    public struct FootstepEntry
    {
        public SurfaceType SurfaceType;
        public List<AudioClip> Clips;
    }

    [SerializeField] private List<FootstepEntry> _entries = new List<FootstepEntry>();

    public AudioClip GetRandomClip(SurfaceType surfaceType)
    {
        foreach (var entry in _entries)
        {
            if (entry.SurfaceType == surfaceType)
            {
                if (entry.Clips != null && entry.Clips.Count > 0)
                {
                    return entry.Clips[UnityEngine.Random.Range(0, entry.Clips.Count)];
                }
            }
        }
        
        // Fallback to Default if specific surface not found
        if (surfaceType != SurfaceType.Default)
        {
            return GetRandomClip(SurfaceType.Default);
        }

        return null;
    }
}
