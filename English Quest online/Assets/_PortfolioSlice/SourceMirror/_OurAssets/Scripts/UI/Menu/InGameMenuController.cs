using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Fusion;
using MoreMountains.Feedbacks;
using UnityServiceLocator;

public class InGameMenuController : GameplayUIBase
{
    [Header("UI Elements")]
    [Tooltip("The panel containing the menu UI — hidden directly on close")]
    [SerializeField] private GameObject _menuPanel;

    [Tooltip("Button to resume the game")]
    [SerializeField] private Button _resumeButton;

    [Tooltip("Button to leave the session and return to main menu")]
    [SerializeField] private Button _leaveButton;

    [Tooltip("Button to close the game application")]
    [SerializeField] private Button _exitButton;

    [SerializeField] private SettingsController _settingsController;
    [SerializeField] private Button _settingsButton;


    [Header("Feedbacks")]
    [Tooltip("MoreMountains feedback that plays when the menu opens (should activate the menu panel inside)")]
    [SerializeField] private MMF_Player _openFeedback;

    private bool _isMenuOpen = false;
    private bool _isSettingsOpen = false;

    private void Start()
    {
        if (_resumeButton != null) _resumeButton.onClick.AddListener(OnResumeClicked);
        if (_leaveButton != null)  _leaveButton.onClick.AddListener(OnLeaveClicked);
        if (_exitButton != null)   _exitButton.onClick.AddListener(OnExitClicked);
        if (_settingsButton != null) _settingsButton.onClick.AddListener(OnSettingsClicked);
        if (_settingsController != null) _settingsController.Initialize(OnBackFromSettings);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isSettingsOpen)
            {
                OnBackFromSettings();
                return;
            }

            if (!_isMenuOpen && IsBlockedByAnotherSystem())
                return;

            ToggleMenu();
        }
    }

    private void ToggleMenu()
    {
        _isMenuOpen = !_isMenuOpen;

        if (_isMenuOpen)
        {
            if (_menuPanel != null) _menuPanel.SetActive(true);
            UIDimmer.Instance.Show();
            AudioListener.pause = true;
            _openFeedback?.PlayFeedbacks();
            BeginInteraction();
        }
        else
        {
            AudioListener.pause = false;
            if (_menuPanel != null) _menuPanel.SetActive(false);
            UIDimmer.Instance.Hide();
            EndInteraction();
        }
    }

    public void OnResumeClicked()
    {
        if (_isMenuOpen) ToggleMenu();
    }

    public void OnLeaveClicked()
    {
        if (ServiceLocator.For(this).TryGet<ILevelManager>(out var levelManager))
        {
            levelManager.ReturnToMenu();
        }
        else
        {
            AppLog.Error("ILevelManager not found via ServiceLocator!");
            SceneManager.LoadScene("Menu");
        }
    }

    public void OnLeaveDemoClicked()
    {
        SceneManager.LoadScene("DemoMenu");
    }

    public void OnExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDisable()
    {
        if (_isMenuOpen)
        {
            AudioListener.pause = false;
            EndInteraction();
            _isMenuOpen = false;
        }
    }

    private bool IsBlockedByAnotherSystem()
    {
        if (!ServiceLocator.For(this).TryGet(out IPlayerLockSystem lockSystem)) return false;

        return GameplayInputGate.IsBlockedByAnother(this, lockSystem);
    }

    private void OnSettingsClicked()
    {
        _isSettingsOpen = true;
        if (_menuPanel != null) _menuPanel.SetActive(false);
        _settingsController?.Show();
    }

    private void OnBackFromSettings()
    {
        _isSettingsOpen = false;
        _settingsController?.Hide();
        if (_menuPanel != null) _menuPanel.SetActive(true);
    }
}
