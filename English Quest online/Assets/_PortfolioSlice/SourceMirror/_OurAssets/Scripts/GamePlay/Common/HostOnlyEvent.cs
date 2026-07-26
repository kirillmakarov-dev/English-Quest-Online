using Fusion;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Adapter that gates a UnityEvent behind a guider check.
/// Call Trigger() from any source (UnityEvent, script, etc.).
/// The events will only invoke if the local player is the session guider.
/// </summary>
public class HostOnlyEvent : NetworkBehaviour
{
    [SerializeField] private UnityEvent _onHostTriggered;

    /// <summary>
    /// Invoke this from a UnityEvent wire-up or any other script.
    /// Silently does nothing if the local player is not the guider.
    /// </summary>
    public void Trigger()
    {
        if (Runner == null || !GuiderService.IsLocalPlayerGuiderFor(Runner))
            return;

        _onHostTriggered.Invoke();
    }
}
