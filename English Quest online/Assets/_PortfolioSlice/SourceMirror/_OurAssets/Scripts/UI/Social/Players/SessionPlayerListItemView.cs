using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionPlayerListItemView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private GameObject _onlineIndicator;
    [SerializeField] private Button _inviteButton;
    [SerializeField] private TextMeshProUGUI _inviteButtonText;
    [SerializeField] private Button _questButton;
    [SerializeField] private TextMeshProUGUI _questButtonText;

    public System.Action OnInvitePressed;
    public System.Action OnQuestEditorPressed;

    private bool _wired;
    private SessionPlayerInfo? _player;

    public void Bind(
        TextMeshProUGUI nameText,
        GameObject onlineIndicator,
        Button inviteButton,
        TextMeshProUGUI inviteButtonText,
        Button questButton = null,
        TextMeshProUGUI questButtonText = null)
    {
        _nameText = nameText;
        _onlineIndicator = onlineIndicator;
        _inviteButton = inviteButton;
        _inviteButtonText = inviteButtonText;
        _questButton = questButton;
        _questButtonText = questButtonText;
        _wired = false;
        EnsureWired();
    }

    private void Awake()
    {
        EnsureWired();
    }

    private void OnEnable()
    {
        EnsureWired();
    }

    private void EnsureWired()
    {
        if (_wired)
            return;

        _wired = true;

        if (_inviteButton != null)
            _inviteButton.onClick.AddListener(HandleInvitePressed);

        if (_questButton != null)
            _questButton.onClick.AddListener(HandleQuestEditorPressed);
    }

    private void HandleInvitePressed()
    {
        string playerName = _player?.DisplayName ?? "(unknown)";
        PlayerRef target = _player?.PlayerRef ?? default;
        Debug.Log(
            $"[SocialPanel] Invite button clicked for '{playerName}' ({target}). " +
            $"handler={(OnInvitePressed != null ? "set" : "null")}, " +
            $"interactable={(_inviteButton != null && _inviteButton.interactable)}, " +
            $"active={gameObject.activeInHierarchy}",
            this);

        if (OnInvitePressed == null)
        {
            Debug.LogWarning("[SocialPanel] Invite click ignored — no OnInvitePressed handler is wired.", this);
            return;
        }

        OnInvitePressed.Invoke();
    }

    private void HandleQuestEditorPressed()
    {
        string playerName = _player?.DisplayName ?? "(unknown)";
        PlayerRef target = _player?.PlayerRef ?? default;
        Debug.Log(
            $"[SocialPanel] Quest button clicked for '{playerName}' ({target}). " +
            $"handler={(OnQuestEditorPressed != null ? "set" : "null")}",
            this);

        if (OnQuestEditorPressed == null)
        {
            Debug.LogWarning("[SocialPanel] Quest editor click ignored — no handler is wired.", this);
            return;
        }

        OnQuestEditorPressed.Invoke();
    }

    public void Bind(SessionPlayerInfo player, bool canInvite, bool showQuestButton)
    {
        _player = player;
        EnsureWired();

        if (_nameText != null)
            _nameText.text = player.DisplayName;

        if (_onlineIndicator != null)
            _onlineIndicator.SetActive(true);

        if (_inviteButton != null)
            _inviteButton.gameObject.SetActive(canInvite);

        if (_inviteButtonText != null)
        {
            _inviteButtonText.text = player.IsInMyParty ? "IN PARTY" : "INVITE";
            _inviteButtonText.color = player.IsInMyParty
                ? new Color(0.72f, 0.78f, 0.86f, 1f)
                : Color.white;
        }

        if (_questButton != null)
            _questButton.gameObject.SetActive(showQuestButton);

        if (_questButtonText != null && showQuestButton)
            _questButtonText.text = "QUESTS";
    }
}
