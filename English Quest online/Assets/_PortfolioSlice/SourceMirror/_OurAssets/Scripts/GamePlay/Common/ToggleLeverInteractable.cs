using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Toggle lever: alternates between on/off each pull and fires a UnityEvent&lt;bool&gt;
/// with the new state (true = on, false = off).
/// </summary>
public class ToggleLeverInteractable : LeverInteractableBase
{
    [Space]
    [SerializeField] private UnityEvent<bool> _leverToggled;

    public UnityEvent<bool> LeverToggled => _leverToggled;

    private bool _isOn;
    public bool IsOn => _isOn;

    protected override void HandleActivation()
    {
        _isOn = !_isOn;
        _leverToggled.Invoke(_isOn);
    }
}
