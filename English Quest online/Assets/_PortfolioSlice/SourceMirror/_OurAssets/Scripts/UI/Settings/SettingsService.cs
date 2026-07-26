using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Engine-facing settings service. Lives on a persistent (always-active) GameObject.
/// Applies all saved settings on Awake so they take effect at startup,
/// even if the Settings panel is never opened.
/// </summary>
public class SettingsService : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────
    [Header("Audio Mixer")]
    [Tooltip("Assign SoundMixer.mixer. Expose 'MasterVolume', 'MusicVolume', 'SFXVolume' parameters.")]
    [SerializeField] private AudioMixer _audioMixer;

    [Header("Post-Processing (optional – for brightness)")]
    [Tooltip("URP Volume with a ColorAdjustments override. Leave empty to skip.")]
    [SerializeField] private Volume _postProcessVolume;

    // ── AudioMixer parameter names ────────────────────────────────
    private const string MixerMaster = "MasterVolume";
    private const string MixerMusic  = "MusicVolume";
    private const string MixerSFX    = "SFXVolume";

    // ── PlayerPrefs keys (public so SettingsController can share them) ─
    public const string PrefQuality    = "EK_Quality";
    public const string PrefBrightness = "EK_Brightness";
    public const string PrefVSync      = "EK_VSync";
    public const string PrefDisplayMode = "EK_DisplayMode";
    public const string PrefMaster     = "EK_MasterVolume";
    public const string PrefMusic      = "EK_MusicVolume";
    public const string PrefSFX        = "EK_SFXVolume";

    // ── Default values (public so SettingsController can share them) ──
    public const float DefaultBrightness = 0.75f;
    public const int   DefaultVSync      = 1;
    public const float DefaultMaster     = 0.8f;
    public const float DefaultMusic      = 0.8f;
    public const float DefaultSFX        = 0.8f;
    public const int   DefaultQuality    = 1;
    public const int   DefaultDisplayMode = (int)FullScreenMode.FullScreenWindow;

    private void Start()
    {
        // Covers the case where this component activates after the static bootstrap ran
        // (e.g. the Settings panel is opened for the first time).
        ApplyAll();
    }

    // ─────────────────────────────────────────────────────────────
    #region Public API

    /// <summary>Reads all settings from PlayerPrefs and applies them to engine systems.</summary>
    public void ApplyAll()
    {
        ApplyQuality(PlayerPrefs.GetInt(PrefQuality, DefaultQuality));
        ApplyBrightness(PlayerPrefs.GetFloat(PrefBrightness, DefaultBrightness));
        ApplyVSync(PlayerPrefs.GetInt(PrefVSync, DefaultVSync) == 1);
        ApplyDisplayMode(PlayerPrefs.GetInt(PrefDisplayMode, DefaultDisplayMode));
        ApplicationSettings.Apply();
        ApplyMixerVolume(MixerMaster, PlayerPrefs.GetFloat(PrefMaster, DefaultMaster));
        ApplyMixerVolume(MixerMusic,  PlayerPrefs.GetFloat(PrefMusic,  DefaultMusic));
        ApplyMixerVolume(MixerSFX,    PlayerPrefs.GetFloat(PrefSFX,    DefaultSFX));
    }

    public void ApplyQuality(int index)
    {
        int clamped = Mathf.Clamp(index, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(clamped, applyExpensiveChanges: true);
    }

    public void ApplyBrightness(float normalised)
    {
        Screen.brightness = normalised;

        if (_postProcessVolume != null &&
            _postProcessVolume.profile.TryGet<ColorAdjustments>(out var ca))
        {
            ca.postExposure.Override(Mathf.Lerp(-2f, 2f, normalised));
        }
    }

    public void ApplyVSync(bool enabled)
    {
        QualitySettings.vSyncCount = enabled ? 1 : 0;
        ApplicationSettings.Apply();
    }

    public void ApplyDisplayMode(int displayMode)
    {
#if UNITY_STANDALONE
        var mode = displayMode switch
        {
            (int)FullScreenMode.Windowed => FullScreenMode.Windowed,
            (int)FullScreenMode.ExclusiveFullScreen => FullScreenMode.ExclusiveFullScreen,
            _ => FullScreenMode.FullScreenWindow
        };

        if (mode == FullScreenMode.Windowed)
        {
            Screen.SetResolution(1920, 1080, mode);
            return;
        }

        var desktop = Screen.currentResolution;
        Screen.SetResolution(desktop.width, desktop.height, mode);
#endif
    }

    public void SetMasterVolume(float linear) => ApplyMixerVolume(MixerMaster, linear);
    public void SetMusicVolume(float linear)  => ApplyMixerVolume(MixerMusic,  linear);
    public void SetSFXVolume(float linear)    => ApplyMixerVolume(MixerSFX,    linear);

    #endregion

    // ─────────────────────────────────────────────────────────────
    private void ApplyMixerVolume(string parameterName, float linear)
    {
        if (_audioMixer == null) return;
        float db = linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
        _audioMixer.SetFloat(parameterName, db);
    }
}
