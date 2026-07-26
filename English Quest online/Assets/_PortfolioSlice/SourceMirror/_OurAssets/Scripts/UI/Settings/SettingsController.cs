using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────
    [Header("Settings Service")]
    [Tooltip("Reference to the always-active SettingsService that applies settings to engine systems.")]
    [SerializeField] private SettingsService _settingsService;

    [Header("Quality Buttons")]
    [SerializeField] private Button _qualityLowBtn;
    [SerializeField] private Button _qualityMediumBtn;
    [SerializeField] private Button _qualityHighBtn;

    [Header("Display Mode")]
    [SerializeField] private GameObject _displayModeRow;
    [SerializeField] private Button _displayWindowedBtn;
    [SerializeField] private Button _displayBorderlessBtn;
    [SerializeField] private Button _displayFullscreenBtn;

    [Header("Brightness")]
    [SerializeField] private Slider            _brightnessSlider;
    [SerializeField] private TextMeshProUGUI   _brightnessValueLabel;

    [Header("VSync")]
    [SerializeField] private Toggle _vsyncToggle;

    [Header("Master Volume")]
    [SerializeField] private Slider          _masterSlider;
    [SerializeField] private TextMeshProUGUI _masterValueLabel;

    [Header("Music Volume")]
    [SerializeField] private Slider          _musicSlider;
    [SerializeField] private TextMeshProUGUI _musicValueLabel;

    [Header("SFX Volume")]
    [SerializeField] private Slider          _sfxSlider;
    [SerializeField] private TextMeshProUGUI _sfxValueLabel;

    [Header("Buttons")]
    [SerializeField] private Button _resetButton;
    [SerializeField] private Button _backButton;

    [Header("Quality Levels")]
    [SerializeField] private int _qualityLevelLow    = 0;
    [SerializeField] private int _qualityLevelMedium = 1;
    [SerializeField] private int _qualityLevelHigh   = 2;
    [SerializeField] private int _defaultQuality     = 1;
    [SerializeField] private int _defaultDisplayMode = (int)FullScreenMode.FullScreenWindow;

    [Header("Quality Button Colors")]
    [SerializeField] private Color _activeQualityColor   = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color _inactiveQualityColor = Color.white;

    private int    _currentQuality;
    private int    _currentDisplayMode;
    private Action _onBackRequested;

    // ─────────────────────────────────────────────────────────────
    #region Unity lifecycle

    private void OnEnable()
    {
        SetDisplayModeRowVisibility();
        LoadAndApplyAll();
        RegisterCallbacks();
    }

    private void OnDisable()
    {
        UnregisterCallbacks();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Public API

    public void Initialize(Action onBackRequested)
    {
        _onBackRequested = onBackRequested;
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Callbacks

    private void RegisterCallbacks()
    {
        _qualityLowBtn?.onClick.AddListener(()    => SetQuality(_qualityLevelLow));
        _qualityMediumBtn?.onClick.AddListener(() => SetQuality(_qualityLevelMedium));
        _qualityHighBtn?.onClick.AddListener(()   => SetQuality(_qualityLevelHigh));
        _displayWindowedBtn?.onClick.AddListener(()   => SetDisplayMode((int)FullScreenMode.Windowed));
        _displayBorderlessBtn?.onClick.AddListener(() => SetDisplayMode((int)FullScreenMode.FullScreenWindow));
        _displayFullscreenBtn?.onClick.AddListener(() => SetDisplayMode((int)FullScreenMode.ExclusiveFullScreen));
        _brightnessSlider?.onValueChanged.AddListener(OnBrightnessChanged);
        _vsyncToggle?.onValueChanged.AddListener(OnVSyncChanged);
        _masterSlider?.onValueChanged.AddListener(OnMasterChanged);
        _musicSlider?.onValueChanged.AddListener(OnMusicChanged);
        _sfxSlider?.onValueChanged.AddListener(OnSFXChanged);
        _resetButton?.onClick.AddListener(ResetToDefaults);
        _backButton?.onClick.AddListener(() => _onBackRequested?.Invoke());
    }

    private void UnregisterCallbacks()
    {
        _qualityLowBtn?.onClick.RemoveAllListeners();
        _qualityMediumBtn?.onClick.RemoveAllListeners();
        _qualityHighBtn?.onClick.RemoveAllListeners();
        _displayWindowedBtn?.onClick.RemoveAllListeners();
        _displayBorderlessBtn?.onClick.RemoveAllListeners();
        _displayFullscreenBtn?.onClick.RemoveAllListeners();
        _brightnessSlider?.onValueChanged.RemoveAllListeners();
        _vsyncToggle?.onValueChanged.RemoveAllListeners();
        _masterSlider?.onValueChanged.RemoveAllListeners();
        _musicSlider?.onValueChanged.RemoveAllListeners();
        _sfxSlider?.onValueChanged.RemoveAllListeners();
        _resetButton?.onClick.RemoveAllListeners();
        _backButton?.onClick.RemoveAllListeners();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Load & apply all settings

    private void LoadAndApplyAll()
    {
        int   quality    = PlayerPrefs.GetInt(SettingsService.PrefQuality,    _defaultQuality);
        int   displayMode = PlayerPrefs.GetInt(SettingsService.PrefDisplayMode, _defaultDisplayMode);
        float brightness = PlayerPrefs.GetFloat(SettingsService.PrefBrightness, SettingsService.DefaultBrightness);
        bool  vsync      = PlayerPrefs.GetInt(SettingsService.PrefVSync,      SettingsService.DefaultVSync) == 1;
        float master     = PlayerPrefs.GetFloat(SettingsService.PrefMaster,   SettingsService.DefaultMaster);
        float music      = PlayerPrefs.GetFloat(SettingsService.PrefMusic,    SettingsService.DefaultMusic);
        float sfx        = PlayerPrefs.GetFloat(SettingsService.PrefSFX,      SettingsService.DefaultSFX);

        // Update UI without re-triggering callbacks
        _currentQuality = quality;
        _currentDisplayMode = displayMode;
        ApplyQualityButtonVisuals(quality);
        ApplyDisplayModeButtonVisuals(displayMode);

        if (_brightnessSlider != null) { _brightnessSlider.SetValueWithoutNotify(brightness); UpdateLabel(_brightnessValueLabel, brightness); }
        if (_vsyncToggle      != null)   _vsyncToggle.SetIsOnWithoutNotify(vsync);
        if (_masterSlider     != null) { _masterSlider.SetValueWithoutNotify(master);         UpdateLabel(_masterValueLabel, master); }
        if (_musicSlider      != null) { _musicSlider.SetValueWithoutNotify(music);           UpdateLabel(_musicValueLabel, music); }
        if (_sfxSlider        != null) { _sfxSlider.SetValueWithoutNotify(sfx);               UpdateLabel(_sfxValueLabel, sfx); }

        // Apply to engine systems via service (service also handles startup apply via Awake)
        _settingsService?.ApplyAll();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Change event handlers

    private void OnBrightnessChanged(float value)
    {
        _settingsService?.ApplyBrightness(value);
        UpdateLabel(_brightnessValueLabel, value);
        PlayerPrefs.SetFloat(SettingsService.PrefBrightness, value);
    }

    private void OnVSyncChanged(bool value)
    {
        _settingsService?.ApplyVSync(value);
        PlayerPrefs.SetInt(SettingsService.PrefVSync, value ? 1 : 0);
    }

    private void OnMasterChanged(float value)
    {
        _settingsService?.SetMasterVolume(value);
        UpdateLabel(_masterValueLabel, value);
        PlayerPrefs.SetFloat(SettingsService.PrefMaster, value);
    }

    private void OnMusicChanged(float value)
    {
        _settingsService?.SetMusicVolume(value);
        UpdateLabel(_musicValueLabel, value);
        PlayerPrefs.SetFloat(SettingsService.PrefMusic, value);
    }

    private void OnSFXChanged(float value)
    {
        _settingsService?.SetSFXVolume(value);
        UpdateLabel(_sfxValueLabel, value);
        PlayerPrefs.SetFloat(SettingsService.PrefSFX, value);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Quality & display mode

    private void SetQuality(int index)
    {
        _currentQuality = index;
        ApplyQualityButtonVisuals(index);
        _settingsService?.ApplyQuality(index);
        PlayerPrefs.SetInt(SettingsService.PrefQuality, index);
    }

    private void ApplyQualityButtonVisuals(int index)
    {
        SetButtonColor(_qualityLowBtn,    index == _qualityLevelLow);
        SetButtonColor(_qualityMediumBtn, index == _qualityLevelMedium);
        SetButtonColor(_qualityHighBtn,   index == _qualityLevelHigh);
    }

    private void SetDisplayMode(int mode)
    {
        if (!IsDisplayModeSupportedPlatform())
            return;

        _currentDisplayMode = mode;
        ApplyDisplayModeButtonVisuals(mode);
        _settingsService?.ApplyDisplayMode(mode);
        PlayerPrefs.SetInt(SettingsService.PrefDisplayMode, mode);
    }

    private void ApplyDisplayModeButtonVisuals(int mode)
    {
        SetButtonColor(_displayWindowedBtn, mode == (int)FullScreenMode.Windowed);
        SetButtonColor(_displayBorderlessBtn, mode == (int)FullScreenMode.FullScreenWindow);
        SetButtonColor(_displayFullscreenBtn, mode == (int)FullScreenMode.ExclusiveFullScreen);
    }

    private static bool IsDisplayModeSupportedPlatform()
    {
        return Application.isEditor
               || Application.platform == RuntimePlatform.WindowsPlayer
               || Application.platform == RuntimePlatform.OSXPlayer
               || Application.platform == RuntimePlatform.LinuxPlayer;
    }

    private void SetDisplayModeRowVisibility()
    {
        if (_displayModeRow != null)
            _displayModeRow.SetActive(IsDisplayModeSupportedPlatform());
    }

    private void SetButtonColor(Button btn, bool active)
    {
        if (btn == null) return;
        var colors = btn.colors;
        colors.normalColor = active ? _activeQualityColor : _inactiveQualityColor;
        btn.colors = colors;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Reset

    private void ResetToDefaults()
    {
        SetQuality(_defaultQuality);
        SetDisplayMode(_defaultDisplayMode);
        if (_brightnessSlider != null) _brightnessSlider.value = SettingsService.DefaultBrightness;
        if (_vsyncToggle      != null) _vsyncToggle.isOn       = SettingsService.DefaultVSync == 1;
        if (_masterSlider     != null) _masterSlider.value     = SettingsService.DefaultMaster;
        if (_musicSlider      != null) _musicSlider.value      = SettingsService.DefaultMusic;
        if (_sfxSlider        != null) _sfxSlider.value        = SettingsService.DefaultSFX;
        // Registered callbacks fire automatically, applying & saving each value.
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Helpers

    private static void UpdateLabel(TextMeshProUGUI label, float value)
    {
        if (label != null)
            label.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }

    #endregion
}
