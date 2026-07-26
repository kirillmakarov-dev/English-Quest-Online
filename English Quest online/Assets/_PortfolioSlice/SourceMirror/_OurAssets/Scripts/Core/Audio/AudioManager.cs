using UnityEngine;
using System.Collections;
using UnityEngine.Audio;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityServiceLocator;

public class AudioManager : StaticInstance<AudioManager>, IAudioService
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource voiceSource;

    private Coroutine _playlistCoroutine;
    private List<AudioClip> _currentPlaylist;
    private int _playlistIndex;
    private bool _isPlaylistPlaying;
    private bool _shufflePlaylist; // Added missing field
    private ServiceLocator _serviceLocator;

    protected override void Awake()
    {
        if (HasInstance && Instance != this)
        {
            if (IsBootstrapScene(gameObject.scene))
            {
                Destroy(gameObject);
                return;
            }

            Destroy(Instance.gameObject);
        }

        base.Awake();
        _serviceLocator = ServiceLocator.For(this);
        _serviceLocator.Register<IAudioService>(this);
    }

    static bool IsBootstrapScene(Scene scene)
    {
        if (!scene.IsValid())
            return false;

        return scene.name is "PreLoad" or "Menu";
    }

    protected override void OnDestroy()
    {
        if (_serviceLocator != null)
        {
            _serviceLocator.Deregister<IAudioService>();
        }

        base.OnDestroy();
    }

    private void Start()
    {
        LoadAudioSettings();
    }

    public void LoadAudioSettings()
    {
        if (audioMixer == null) return;
        
        float master = PlayerPrefs.GetFloat("EK_MasterVolume", 0.8f);
        float music = PlayerPrefs.GetFloat("EK_MusicVolume", 0.8f);
        float sfx = PlayerPrefs.GetFloat("EK_SFXVolume", 0.8f);

        ApplyMixerVolume("MasterVolume", master);
        ApplyMixerVolume("MusicVolume", music);
        ApplyMixerVolume("SFXVolume", sfx);
    }

    private void ApplyMixerVolume(string parameterName, float linear)
    {
        float db = linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
        audioMixer.SetFloat(parameterName, db);
    }

    public void PlayVoice(AudioClip clip)
    {
        if (voiceSource == null) return;
        
        voiceSource.Stop(); // Stop previous line to avoid overlap
        
        if (clip != null)
        {
            voiceSource.clip = clip;
            voiceSource.Play();
        }
    }

    public void StopVoice()
    {
        if (voiceSource != null)
        {
            voiceSource.Stop();
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
    
    public void PlayMusic(AudioClip clip)
    {
        StopPlaylistCoroutine(); // significant change: ensure playlist stops if single clip requested

        if (musicSource == null || clip == null) return;
        
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.loop = true; // Ensure looping for single track
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void PlayMusicPlaylist(List<AudioClip> playlist, bool shuffle = false)
    {
        if (playlist == null || playlist.Count == 0) return;

        // If only one track, use standard loop logic
        if (playlist.Count == 1)
        {
            PlayMusic(playlist[0]);
            return;
        }

        StopPlaylistCoroutine();
        
        _currentPlaylist = new List<AudioClip>(playlist);
        _playlistIndex = 0;
        _shufflePlaylist = shuffle;
        _isPlaylistPlaying = true;

        if (shuffle)
        {
            ShufflePlaylist();
        }

        PlayNextTrackInPlaylist();
    }

    private void PlayNextTrackInPlaylist()
    {
        if (_currentPlaylist == null || _currentPlaylist.Count == 0) return;

        if (_playlistIndex >= _currentPlaylist.Count)
        {
            _playlistIndex = 0; 
            // Optional: Reshuffle here if desired
        }

        AudioClip clip = _currentPlaylist[_playlistIndex];
        
        musicSource.loop = false; // Disable loop so we can detect end
        musicSource.clip = clip;
        musicSource.Play();

        _playlistCoroutine = StartCoroutine(WaitForTrackEnd(clip.length));
    }

    private IEnumerator WaitForTrackEnd(float duration)
    {
        // Wait for the clip duration
        yield return new WaitForSeconds(duration);

        // Advance index
        _playlistIndex++;
        
        // Loop back if finished
        if (_playlistIndex >= _currentPlaylist.Count)
        {
            _playlistIndex = 0;
            if (_shufflePlaylist) ShufflePlaylist();
        }

        PlayNextTrackInPlaylist();
    }

    private void StopPlaylistCoroutine()
    {
        if (_playlistCoroutine != null)
        {
            StopCoroutine(_playlistCoroutine);
            _playlistCoroutine = null;
        }
        _isPlaylistPlaying = false;
    }

    private void ShufflePlaylist()
    {
        for (int i = 0; i < _currentPlaylist.Count; i++)
        {
            AudioClip temp = _currentPlaylist[i];
            int randomIndex = Random.Range(i, _currentPlaylist.Count);
            _currentPlaylist[i] = _currentPlaylist[randomIndex];
            _currentPlaylist[randomIndex] = temp;
        }
    }
}
