using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class Action_TriggerImpulse : GameAction
{
    [Tooltip("The Cinemachine Impulse Source to trigger.")]
    [SerializeField] private CinemachineImpulseSource _impulseSource;

    [Tooltip("Force multiplier for the impulse.")]
    [SerializeField] private float _force = 1.0f;

    public override IEnumerator Execute()
    {
        if (_impulseSource != null)
        {
            AppLog.Info($"[Action_TriggerImpulse] Generating impulse with force {_force}");
            _impulseSource.GenerateImpulse(_force);
        }
        else
        {
            AppLog.Warning("[Action_TriggerImpulse] Impulse Source is not assigned.");
        }
        yield break;
    }
}
