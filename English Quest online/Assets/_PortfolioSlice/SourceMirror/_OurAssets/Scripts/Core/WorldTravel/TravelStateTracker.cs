using System.Collections.Generic;
using Fusion;

/// <summary>
/// Tracks in-flight travel per Fusion runner (or a single offline slot).
/// </summary>
internal sealed class TravelStateTracker
{
    private readonly HashSet<NetworkRunner> _travelingRunners = new();
    private bool _offlineTraveling;

    public bool IsAnyoneTraveling => _travelingRunners.Count > 0 || _offlineTraveling;

    public bool TryBegin(PlayerInteraction interactor)
    {
        NetworkRunner runner = TravelInteractorResolver.GetRunner(interactor);
        if (runner != null)
            return _travelingRunners.Add(runner);

        if (_offlineTraveling)
            return false;

        _offlineTraveling = true;
        return true;
    }

    public void End(PlayerInteraction interactor)
    {
        NetworkRunner runner = TravelInteractorResolver.GetRunner(interactor);
        if (runner != null)
            _travelingRunners.Remove(runner);
        else
            _offlineTraveling = false;
    }

    public bool IsTraveling(PlayerInteraction interactor)
    {
        NetworkRunner runner = TravelInteractorResolver.GetRunner(interactor);
        if (runner != null)
            return _travelingRunners.Contains(runner);

        return _offlineTraveling;
    }

    public void ClearRunner(NetworkRunner runner)
    {
        if (runner != null)
            _travelingRunners.Remove(runner);
        else
            _offlineTraveling = false;
    }
}
