using System.Collections;
using EnglishKingdom.StatsSystem;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Applies an amount to a <see cref="StatDefinitionSO"/> via <see cref="IStatsService"/>.
/// The primary way to wire simple stats (enemy defeated, object picked up, etc.) into scenes
/// without changing the combat/pickup code itself — drop this on a prefab and call
/// <see cref="Record"/> from an existing UnityEvent (e.g. an on-death or on-pickup callback).
/// </summary>
public class Action_RecordStat : GameAction
{
    [Tooltip("Which stat to update.")]
    [SerializeField] private StatDefinitionSO _stat;

    [Tooltip("Value passed to IStatsService.Apply. For Sum stats this is the increment (usually 1).")]
    [SerializeField] private int _amount = 1;

    public override IEnumerator Execute()
    {
        Record();
        yield break;
    }

    /// <summary>Convenience entry point for UnityEvent wiring (inspector 'On Click ()' style lists).</summary>
    public void Record()
    {
        if (_stat == null)
        {
            AppLog.Warning($"[Action_RecordStat] No StatDefinitionSO assigned on {gameObject.name}.");
            return;
        }

        if (ServiceLocator.Global.TryGet(out IStatsService stats))
        {
            stats.Apply(_stat, _amount);
        }
        else
        {
            AppLog.Error("[Action_RecordStat] IStatsService is not registered!");
        }
    }
}
