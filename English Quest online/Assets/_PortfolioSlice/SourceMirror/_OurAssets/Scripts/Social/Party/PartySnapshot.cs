using System.Collections.Generic;

public readonly struct PartySnapshot
{
    public IReadOnlyList<PartyMemberInfo> Members { get; }
    public int OpenSlotCount { get; }
    public bool IsLeader { get; }
    public bool IsInMultiMemberParty { get; }

    public PartySnapshot(
        IReadOnlyList<PartyMemberInfo> members,
        int openSlotCount,
        bool isLeader,
        bool isInMultiMemberParty)
    {
        Members = members;
        OpenSlotCount = openSlotCount;
        IsLeader = isLeader;
        IsInMultiMemberParty = isInMultiMemberParty;
    }
}
