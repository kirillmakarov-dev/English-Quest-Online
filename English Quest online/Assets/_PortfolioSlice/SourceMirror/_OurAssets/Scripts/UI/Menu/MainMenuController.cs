using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityServiceLocator;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject _mainPanel;
    [SerializeField] private GameObject _hostPanel;
    [SerializeField] private GameObject _joinPanel;

    [SerializeField] private SettingsController _settingsController;

    [Header("Host Inputs")]
    [SerializeField] private TMP_InputField _hostSessionNameInput;
    [SerializeField] private TMP_InputField _hostPasswordInput;
    [SerializeField] private TMP_Dropdown _levelDropdown;
    [SerializeField] private Button _startHostButton;

    [Header("Join Inputs")]
    [SerializeField] private TMP_InputField _joinSessionNameInput;
    [SerializeField] private TMP_InputField _joinPasswordInput;
    [SerializeField] private Button _startJoinButton;

    [Header("Open World")]
    [SerializeField] private NetworkSessionProfile _openWorldProfile;
    [SerializeField] private string _openWorldSceneName = "OpenWorld";
    [SerializeField] private Button _openWorldButton;

    [Header("Level Configuration")]
    [SerializeField] private MenuHostLevelCatalogSO _levelCatalog;

    [Header("Version")]
    [SerializeField] private TextMeshProUGUI _versionLabel;

    [Header("Profile")]
    [FormerlySerializedAs("_teacherRoleLabel")]
    [SerializeField] private TextMeshProUGUI _guiderRoleLabel;

    [Header("Session Errors")]
    [SerializeField] private TextMeshProUGUI _sessionErrorLabel;
    [SerializeField] private float _sessionErrorDisplaySeconds = 5f;

    private const string HostPassword = "ER26";
    private Coroutine _sessionErrorCoroutine;

    private void OnEnable()
    {
        NetworkSessionErrorService.ErrorRaised += HandleSessionError;
    }

    private void OnDisable()
    {
        NetworkSessionErrorService.ErrorRaised -= HandleSessionError;
    }

    private void Start()
    {
        ShowPanel(_mainPanel);

        if (_settingsController != null)
        {
            _settingsController.Initialize(OnBackFromSettings);
            _settingsController.Hide();
        }

        PopulateLevelDropdown();
        ApplyVersionLabel();
        ApplyGuiderRoleLabel();
    }

    private void ApplyGuiderRoleLabel()
    {
        if (_guiderRoleLabel == null)
            return;

        if (PlayerRoleProfile.IsGuider)
        {
            _guiderRoleLabel.text = "Guider";
            _guiderRoleLabel.gameObject.SetActive(true);
        }
        else
        {
            _guiderRoleLabel.gameObject.SetActive(false);
        }
    }

    private void ApplyVersionLabel()
    {
        if (_versionLabel == null)
            return;

        string version = PlayerPrefs.GetString("gameVersion", "");
        if (string.IsNullOrEmpty(version))
            version = Application.version;

        _versionLabel.text = version;
    }

    private void PopulateLevelDropdown()
    {
        if (_levelDropdown == null)
            return;

        _levelDropdown.ClearOptions();

        if (_levelCatalog == null || _levelCatalog.LevelCount == 0)
        {
            AppLog.Error("MainMenuController: MenuHostLevelCatalog is missing or has no hostable levels.");
            return;
        }

        var options = new List<string>(_levelCatalog.LevelCount);
        for (int i = 0; i < _levelCatalog.LevelCount; i++)
            options.Add(_levelCatalog.GetDisplayName(i));

        _levelDropdown.AddOptions(options);
    }

    public void OnJoinClicked()
    {
        ShowPanel(_joinPanel);
    }

    public void OnHostClicked()
    {
        ShowPanel(_hostPanel);
    }

    public void OnSettingsClicked()
    {
        ShowPanel(null);
        _settingsController?.Show();
    }

    private void OnBackFromSettings()
    {
        _settingsController?.Hide();
        ShowPanel(_mainPanel);
    }

    public void OnBackClicked()
    {
        ShowPanel(_mainPanel);
    }

    public void OnExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public async void OnStartHostGameClicked()
    {
        string sessionName = _hostSessionNameInput.text;
        if (string.IsNullOrEmpty(sessionName))
        {
            AppLog.Error("Session Name cannot be empty");
            return;
        }

        if (_hostPasswordInput != null)
        {
            string enteredPassword = _hostPasswordInput.text;
            if (!string.Equals(enteredPassword, HostPassword))
            {
                AppLog.Error("Invalid host password");
                return;
            }
        }

        if (_levelCatalog == null || _levelCatalog.LevelCount == 0)
        {
            AppLog.Error("MainMenuController: MenuHostLevelCatalog is missing or has no hostable levels.");
            return;
        }

        int levelIndex = _levelDropdown.value;
        if (!_levelCatalog.TryGetLevel(levelIndex, out int sceneBuildIndex, out string displayName))
        {
            AppLog.Error($"Invalid level selection at index {levelIndex}.");
            return;
        }

        if (_startHostButton != null)
            _startHostButton.interactable = false;

        try
        {
            if (ServiceLocator.For(this).TryGet<INetworkSessionService>(out var network))
                await network.StartSharedSession(sessionName, sceneBuildIndex);
            else
                AppLog.Error("INetworkSessionService not found via ServiceLocator.");
        }
        catch (Exception e)
        {
            HandleSessionError(e.Message);
        }
        finally
        {
            if (_startHostButton != null)
                _startHostButton.interactable = true;
        }
    }

    public async void OnOpenWorldClicked()
    {
        if (_openWorldButton != null)
            _openWorldButton.interactable = false;

        try
        {
            if (ServiceLocator.For(this).TryGet<INetworkSessionService>(out var network))
            {
                if (_openWorldProfile != null)
                    await network.JoinOpenWorldAsync(_openWorldProfile);
                else
                    await network.JoinOpenWorldAsync(_openWorldSceneName);
            }
            else
                AppLog.Error("INetworkSessionService not found via ServiceLocator.");
        }
        catch (Exception e)
        {
            HandleSessionError(e.Message);
        }
        finally
        {
            if (_openWorldButton != null)
                _openWorldButton.interactable = true;
        }
    }

    public async void OnStartJoinGameClicked()
    {
        string sessionName = _joinSessionNameInput.text;
        if (string.IsNullOrEmpty(sessionName))
        {
            AppLog.Error("Session Name cannot be empty");
            return;
        }

        if (_joinPasswordInput != null)
        {
            string expectedPassword = $"0{System.DateTime.Now.Day:D2}0";
            if (_joinPasswordInput.text != expectedPassword)
            {
                AppLog.Error("Invalid join password");
                return;
            }
        }

        if (_startJoinButton != null)
            _startJoinButton.interactable = false;

        try
        {
            if (ServiceLocator.For(this).TryGet<INetworkSessionService>(out var network))
                await network.JoinSharedSession(sessionName);
            else
                AppLog.Error("INetworkSessionService not found via ServiceLocator.");
        }
        catch (Exception e)
        {
            HandleSessionError(e.Message);
        }
        finally
        {
            if (_startJoinButton != null)
                _startJoinButton.interactable = true;
        }
    }

    private void ShowPanel(GameObject panelToShow)
    {
        _mainPanel.SetActive(false);
        _hostPanel.SetActive(false);
        _joinPanel.SetActive(false);
        _settingsController?.Hide();

        if (panelToShow != null)
            panelToShow.SetActive(true);
    }

    private void HandleSessionError(string message)
    {
        AppLog.Error($"[MainMenuController] {message}");

        if (_sessionErrorLabel == null)
            return;

        _sessionErrorLabel.text = message;
        _sessionErrorLabel.gameObject.SetActive(true);

        if (_sessionErrorCoroutine != null)
            StopCoroutine(_sessionErrorCoroutine);

        _sessionErrorCoroutine = StartCoroutine(HideSessionErrorAfterDelay());
    }

    private System.Collections.IEnumerator HideSessionErrorAfterDelay()
    {
        yield return new WaitForSeconds(_sessionErrorDisplaySeconds);

        if (_sessionErrorLabel != null)
        {
            _sessionErrorLabel.text = string.Empty;
            _sessionErrorLabel.gameObject.SetActive(false);
        }

        _sessionErrorCoroutine = null;
    }
}
