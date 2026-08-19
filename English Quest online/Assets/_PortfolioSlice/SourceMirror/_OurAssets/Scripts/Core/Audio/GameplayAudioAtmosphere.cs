using UnityEngine;

/// <summary>
/// Owns the lightweight ambient loop and shared UI feedback used by the gameplay scene.
/// The sources are created at runtime so the scene only needs references to the clips.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameplayAudioAtmosphere : MonoBehaviour
{
    [Header("Ambient")]
    [SerializeField] private AudioClip ambientClip;
    [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.22f;

    [Header("Mini-game UI")]
    [SerializeField] private AudioClip miniGameCloseClip;
    [SerializeField, Range(0f, 1f)] private float miniGameCloseVolume = 0.28f;

    private static GameplayAudioAtmosphere _instance;
    private AudioSource _ambientSource;
    private AudioSource _uiSource;

    private void Awake()
    {
        _instance = this;
        _ambientSource = CreateSource(ambientVolume);
        _ambientSource.loop = true;

        _uiSource = CreateSource(1f);
    }

    private void Start()
    {
        if (ambientClip == null)
            return;

        _ambientSource.clip = ambientClip;
        _ambientSource.Play();
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    public void Configure(AudioClip ambient, AudioClip miniGameClose)
    {
        ambientClip = ambient;
        miniGameCloseClip = miniGameClose;
    }

    public static void PlayMiniGamePanelClosed()
    {
        if (_instance == null || _instance.miniGameCloseClip == null || _instance._uiSource == null)
            return;

        _instance._uiSource.PlayOneShot(
            _instance.miniGameCloseClip,
            _instance.miniGameCloseVolume);
    }

    private AudioSource CreateSource(float volume)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = volume;
        source.dopplerLevel = 0f;
        source.hideFlags = HideFlags.HideInInspector;
        return source;
    }
}
