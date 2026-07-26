using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityServiceLocator;

public class SceneMusicController : MonoBehaviour
{
    [SerializeField] private List<AudioClip> playlist = new List<AudioClip>();
    [SerializeField] private bool shuffle = false;

    private void Start()
    {
        if (playlist.Count > 0 && ServiceLocator.For(this).TryGet<IAudioService>(out var audioService))
        {
            audioService.PlayMusicPlaylist(playlist, shuffle);
        }
    }
}
