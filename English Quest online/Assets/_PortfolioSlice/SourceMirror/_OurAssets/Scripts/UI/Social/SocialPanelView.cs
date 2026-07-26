using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SocialPanelView : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject _panelRoot;

    [Header("Columns")]
    [SerializeField] private Transform _playersColumnRoot;
    [SerializeField] private Transform _partyColumnRoot;

    [Header("Invite Banner")]
    [SerializeField] private GameObject _inviteBannerRoot;
    [SerializeField] private TextMeshProUGUI _inviteBannerText;
    [SerializeField] private Button _acceptInviteButton;
    [SerializeField] private Button _declineInviteButton;

    [Header("Controls")]
    [SerializeField] private Button _closeButton;

    public Transform PlayersColumnRoot => _playersColumnRoot;
    public Transform PartyColumnRoot => _partyColumnRoot;

    public System.Action OnClosePressed;
    public System.Action OnAcceptInvitePressed;
    public System.Action OnDeclineInvitePressed;

    private bool _wired;

    private void Awake()
    {
        EnsureWired();

        // Prefab starts with PanelRoot inactive. Do not SetActive(false) here:
        // _panelRoot is this GameObject, so Show()'s first activate would run Awake
        // and immediately undo the open (dimmer only, need several G presses).
        if (_inviteBannerRoot != null && _inviteBannerRoot != gameObject)
            HideInviteBanner();
    }

    private void OnEnable()
    {
        EnsureWired();
    }

    public void Bind(
        GameObject panelRoot,
        Transform playersColumnRoot,
        Transform partyColumnRoot,
        GameObject inviteBannerRoot,
        TextMeshProUGUI inviteBannerText,
        Button acceptInviteButton,
        Button declineInviteButton,
        Button closeButton)
    {
        _panelRoot = panelRoot;
        _playersColumnRoot = playersColumnRoot;
        _partyColumnRoot = partyColumnRoot;
        _inviteBannerRoot = inviteBannerRoot;
        _inviteBannerText = inviteBannerText;
        _acceptInviteButton = acceptInviteButton;
        _declineInviteButton = declineInviteButton;
        _closeButton = closeButton;

        _wired = false;
        EnsureWired();

        // Only force-close during editor/runtime wiring when the root is a separate object.
        if (_panelRoot != null && _panelRoot != gameObject)
            _panelRoot.SetActive(false);

        if (_inviteBannerRoot != null && _inviteBannerRoot != gameObject)
            HideInviteBanner();
    }

    private void EnsureWired()
    {
        if (_wired)
            return;

        _wired = true;
        _closeButton?.onClick.AddListener(() => OnClosePressed?.Invoke());
        _acceptInviteButton?.onClick.AddListener(() => OnAcceptInvitePressed?.Invoke());
        _declineInviteButton?.onClick.AddListener(() => OnDeclineInvitePressed?.Invoke());
    }

    public void Show()
    {
        EnsureWired();
        if (_panelRoot != null)
            _panelRoot.SetActive(true);
    }

    public void PrepareForDisplay()
    {
        if (_panelRoot == null)
            return;

        Canvas.ForceUpdateCanvases();

        if (_panelRoot.transform is RectTransform panelRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);

        RectTransform layoutRoot = ResolveLayoutRebuildRoot();
        if (layoutRoot != null && layoutRoot != _panelRoot.transform)
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);

        Canvas.ForceUpdateCanvases();
    }

    private RectTransform ResolveLayoutRebuildRoot()
    {
        if (_playersColumnRoot != null)
        {
            Transform columns = _playersColumnRoot.parent != null
                ? _playersColumnRoot.parent.parent
                : null;
            if (columns is RectTransform columnsRect)
                return columnsRect;
        }

        if (_panelRoot.transform.childCount == 0)
            return _panelRoot.transform as RectTransform;

        return _panelRoot.transform.GetChild(0) as RectTransform;
    }

    public void Hide()
    {
        if (_panelRoot != null)
            _panelRoot.SetActive(false);
    }

    public void ShowInviteBanner(string message)
    {
        if (_inviteBannerRoot != null)
            _inviteBannerRoot.SetActive(true);

        if (_inviteBannerText != null)
            _inviteBannerText.text = message;
    }

    public void HideInviteBanner()
    {
        if (_inviteBannerRoot != null)
            _inviteBannerRoot.SetActive(false);
    }
}
