using System;
using System.Collections.Generic;

public interface ISessionPlayerRegistry
{
    event Action OnPlayersChanged;

    IReadOnlyList<SessionPlayerInfo> Players { get; }

    void Refresh();
}
