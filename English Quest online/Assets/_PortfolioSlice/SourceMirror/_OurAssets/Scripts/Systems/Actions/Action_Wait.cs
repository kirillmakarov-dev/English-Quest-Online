using System.Collections;
using UnityEngine;

public class Action_Wait : GameAction
{
    [Tooltip("Time in seconds to wait.")]
    [SerializeField] private float _duration = 1.0f;

    public override IEnumerator Execute()
    {
        if (_duration > 0)
        {
            AppLog.Info($"[Action_Wait] Waiting for {_duration} seconds...");
            yield return new WaitForSeconds(_duration);
        }
    }
}
