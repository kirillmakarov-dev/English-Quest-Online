using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Fires a list of local UnityEvents when a qualifying object enters the trigger collider.
/// No networking — use NetworkEventRelay when all players must respond.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TriggerEventRelay : MonoBehaviour
{
    [Header("Filter")]
    [Tooltip("Only objects with this tag will activate the trigger. Leave empty to accept any object.")]
    [SerializeField] private string _requiredTag = "Player";

    [Space]
    [Tooltip("If enabled, the events fire only the first time the trigger is entered.")]
    [SerializeField] private bool _runOnce = false;
    private bool _hasRun = false;

    [Space]
    [Tooltip("Invoked locally when a qualifying object enters the trigger.")]
    [SerializeField] private List<UnityEvent> _onTriggerEntered = new();

    private void Awake()
    {
        var col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            AppLog.Warning($"[TriggerEventRelay] Collider on '{gameObject.name}' is not set to Is Trigger. Setting it now.", this);
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_runOnce && _hasRun) return;
        if (!string.IsNullOrEmpty(_requiredTag) && !other.CompareTag(_requiredTag)) return;

        _hasRun = true;

        foreach (UnityEvent ev in _onTriggerEntered)
            ev?.Invoke();
    }

    /// <summary>Resets the run-once guard so the trigger can fire again.</summary>
    public void ResetTrigger() => _hasRun = false;
}
