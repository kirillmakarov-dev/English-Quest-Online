using System;
using Fusion;

public interface IPartyService
{
    event Action OnPartyChanged;

    PartySnapshot GetMyParty();
    bool CanInvite(PlayerRef target);
    void LeaveParty();
    void Kick(PlayerRef target);
    int GetPartySize(PlayerRef leaderRef);
    PlayerRef GetPartyLeader(PlayerRef member);
    void NotifyChanged();
}
