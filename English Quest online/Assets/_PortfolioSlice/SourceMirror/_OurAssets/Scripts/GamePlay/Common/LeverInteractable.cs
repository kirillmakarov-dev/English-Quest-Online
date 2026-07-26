using UnityEngine;
using UnityEngine.Events;

/// <summary>One-shot lever: fires a UnityEvent once per pull.</summary>
public class LeverInteractable : LeverInteractableBase
{
    [Space]
    [SerializeField] private UnityEvent _leverActivated;

    public UnityEvent LeverActivated => _leverActivated;

    protected override void HandleActivation()
    {
        _leverActivated.Invoke();
    }
}
