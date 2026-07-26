using System.Collections;
using UnityEngine;

/// <summary>
/// Schedules deferred attack damage. At impact time, hit detection runs from the caster's
/// current pose — targets must still be inside the attack shape to take damage.
///
/// VFX is no longer scheduled here; it is replicated to all clients via
/// <see cref="PlayerAbilityPresentationSync"/> and played by <see cref="AttackPresentationPlayer"/>.
/// </summary>
[DisallowMultipleComponent]
public class AttackHitScheduler : MonoBehaviour
{
    private int _castGeneration;
    private int _resolvedCastGeneration = -1;

    public static int TestResolveHitCount { get; private set; }

    public static void ResetTestCounters() => TestResolveHitCount = 0;

    public void ScheduleDelayedHit(
        Transform caster,
        AttackShapeSO shape,
        float damageMin,
        float damageMax,
        int maxTargets,
        DamageType damageType,
        Vector3 originOffset,
        LayerMask targetLayers,
        float delay)
    {
        if (caster == null || shape == null || damageMax <= 0f) return;

        int castGeneration = ++_castGeneration;

        if (delay <= 0f)
        {
            TryResolveHit(
                castGeneration,
                caster,
                shape,
                damageMin,
                damageMax,
                maxTargets,
                damageType,
                originOffset,
                targetLayers);
            return;
        }

        StartCoroutine(DelayedHit(
            castGeneration,
            caster,
            shape,
            damageMin,
            damageMax,
            maxTargets,
            damageType,
            originOffset,
            targetLayers,
            delay));
    }

    /// <summary>
    /// Cancels pending damage for the current cast when the player is hit during windup.
    /// Mirrors <see cref="MeleeAttackUtilityAction.CancelPendingHit"/>.
    /// </summary>
    public void CancelPendingHit()
    {
        if (_castGeneration <= 0 || _resolvedCastGeneration >= _castGeneration)
            return;

        _resolvedCastGeneration = _castGeneration;
    }

    private IEnumerator DelayedHit(
        int castGeneration,
        Transform caster,
        AttackShapeSO shape,
        float damageMin,
        float damageMax,
        int maxTargets,
        DamageType damageType,
        Vector3 originOffset,
        LayerMask targetLayers,
        float delay)
    {
        yield return new WaitForSeconds(delay);

        if (caster == null || !caster.gameObject.activeInHierarchy)
            yield break;

        TryResolveHit(
            castGeneration,
            caster,
            shape,
            damageMin,
            damageMax,
            maxTargets,
            damageType,
            originOffset,
            targetLayers);
    }

    private void TryResolveHit(
        int castGeneration,
        Transform caster,
        AttackShapeSO shape,
        float damageMin,
        float damageMax,
        int maxTargets,
        DamageType damageType,
        Vector3 originOffset,
        LayerMask targetLayers)
    {
        if (_resolvedCastGeneration >= castGeneration)
            return;

        _resolvedCastGeneration = castGeneration;
        ResolveHit(
            caster,
            shape,
            damageMin,
            damageMax,
            maxTargets,
            damageType,
            originOffset,
            targetLayers);
    }

    private static void ResolveHit(
        Transform caster,
        AttackShapeSO shape,
        float damageMin,
        float damageMax,
        int maxTargets,
        DamageType damageType,
        Vector3 originOffset,
        LayerMask targetLayers)
    {
        TestResolveHitCount++;

        Vector3 forward = caster.forward;
        Vector3 origin = caster.position + caster.TransformVector(originOffset);
        LayerMask layers = targetLayers.value != 0 ? targetLayers : CombatLayers.DefaultTargetMask;

        var context = new AttackHitContext(
            origin,
            forward,
            caster,
            layers,
            caster.gameObject,
            damageMin,
            damageMax,
            maxTargets);

        AttackExecutor.ApplyDamage(shape, context, damageType);
    }
}
