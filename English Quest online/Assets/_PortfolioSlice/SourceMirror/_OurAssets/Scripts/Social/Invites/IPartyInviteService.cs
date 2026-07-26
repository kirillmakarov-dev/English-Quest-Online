using System;
using Fusion;

public interface IPartyInviteService
{
    event Action OnInviteStateChanged;

    PartyInvite? PendingInvite { get; }

    void SendInvite(PlayerRef target);
    void AcceptInvite();
    void DeclineInvite();
}
