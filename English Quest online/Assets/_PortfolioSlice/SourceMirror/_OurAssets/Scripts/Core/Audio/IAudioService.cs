using System.Collections.Generic;
using UnityEngine;

public interface IAudioService
{
    void LoadAudioSettings();
    void PlayVoice(AudioClip clip);
    void StopVoice();
    void PlaySFX(AudioClip clip);
    void PlayMusic(AudioClip clip);
    void PlayMusicPlaylist(List<AudioClip> playlist, bool shuffle = false);
}