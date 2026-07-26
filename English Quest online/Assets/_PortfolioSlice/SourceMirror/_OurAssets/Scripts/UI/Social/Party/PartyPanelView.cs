using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class PartyPanelView : MonoBehaviour
{
    [SerializeField] private List<PartySlotItemView> _slots = new();
    [SerializeField] private Button _leavePartyButton;

    public System.Action OnLeavePartyPressed;
    public System.Action<PlayerRef> OnKickRequested;

    private bool _wired;

    public void Bind(List<PartySlotItemView> slots, Button leavePartyButton)
    {
        _slots = slots;
        _leavePartyButton = leavePartyButton;
    }

    private void EnsureWired()
    {
        if (_wired)
            return;

        _wired = true;
        _leavePartyButton?.onClick.AddListener(() => OnLeavePartyPressed?.Invoke());
    }

    public void Render(PartySnapshot snapshot)
    {
        EnsureWired();
        for (int i = 0; i < _slots.Count; i++)
        {
            PartySlotItemView slot = _slots[i];
            if (slot == null)
                continue;

            if (i < snapshot.Members.Count)
            {
                PartyMemberInfo member = snapshot.Members[i];
                bool canKick = snapshot.IsLeader && !member.IsLocal && !member.IsLeader;
                slot.ShowMember(member, canKick);

                PlayerRef capturedPlayer = member.PlayerRef;
                slot.OnKickPressed = () => OnKickRequested?.Invoke(capturedPlayer);
            }
            else
            {
                slot.ShowOpenSlot();
                slot.OnKickPressed = null;
            }
        }

        if (_leavePartyButton != null)
            _leavePartyButton.gameObject.SetActive(snapshot.IsInMultiMemberParty);
    }
}
