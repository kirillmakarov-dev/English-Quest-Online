using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartySlotItemView : MonoBehaviour
{
    [SerializeField] private GameObject _occupiedRoot;
    [SerializeField] private GameObject _openSlotRoot;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _openSlotText;
    [SerializeField] private GameObject _leaderHighlight;
    [SerializeField] private GameObject _leaderBadge;
    [SerializeField] private Button _kickButton;

    public System.Action OnKickPressed;

    public void Bind(
        GameObject occupiedRoot,
        GameObject openSlotRoot,
        TextMeshProUGUI nameText,
        TextMeshProUGUI openSlotText,
        GameObject leaderHighlight,
        Button kickButton)
    {
        Bind(occupiedRoot, openSlotRoot, nameText, openSlotText, leaderHighlight, null, kickButton);
    }

    public void Bind(
        GameObject occupiedRoot,
        GameObject openSlotRoot,
        TextMeshProUGUI nameText,
        TextMeshProUGUI openSlotText,
        GameObject leaderHighlight,
        GameObject leaderBadge,
        Button kickButton)
    {
        _occupiedRoot = occupiedRoot;
        _openSlotRoot = openSlotRoot;
        _nameText = nameText;
        _openSlotText = openSlotText;
        _leaderHighlight = leaderHighlight;
        _leaderBadge = leaderBadge;
        _kickButton = kickButton;
    }

    private void Awake()
    {
        _kickButton?.onClick.AddListener(() => OnKickPressed?.Invoke());
    }

    public void ShowOpenSlot()
    {
        if (_occupiedRoot != null)
            _occupiedRoot.SetActive(false);

        if (_openSlotRoot != null)
            _openSlotRoot.SetActive(true);

        if (_openSlotText != null)
            _openSlotText.text = "OPEN SLOT";
    }

    public void ShowMember(PartyMemberInfo member, bool canKick)
    {
        if (_occupiedRoot != null)
            _occupiedRoot.SetActive(true);

        if (_openSlotRoot != null)
            _openSlotRoot.SetActive(false);

        if (_nameText != null)
            _nameText.text = member.DisplayName;

        if (_leaderHighlight != null)
            _leaderHighlight.SetActive(member.IsLeader);

        if (_leaderBadge != null)
            _leaderBadge.SetActive(member.IsLeader);

        if (_kickButton != null)
            _kickButton.gameObject.SetActive(canKick);
    }
}
