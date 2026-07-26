using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runs hit detection, applies damage, and spawns attack VFX.
/// </summary>
public static class AttackExecutor
{
    public static GameObject SpawnVfx(GameObject vfxPrefab, Vector3 vfxPosition, Quaternion vfxRotation)
    {
        return AttackVfxSpawner.Spawn(vfxPrefab, vfxPosition, vfxRotation);
    }

    public static int ApplyDamage(
        AttackShapeSO shape,
        AttackHitContext context,
        DamageType damageType)
    {
        if (shape == null || context.DamageMax <= 0f) return 0;

        IReadOnlyList<Collider> hits = shape.DetectHits(context);
        HashSet<DamageReceiver> damaged = new();
        int appliedCount = 0;
        int maxTargets = context.MaxTargets;

        foreach (Collider collider in hits)
        {
            if (maxTargets > 0 && appliedCount >= maxTargets) break;

            if (!AttackShapeDetection.TryGetDamageReceiver(collider, out DamageReceiver receiver)) continue;
            if (!damaged.Add(receiver)) continue;

            float rolledDamage = RollDamage(context.DamageMin, context.DamageMax);
            receiver.ApplyDamage(rolledDamage, new DamageInfo(rolledDamage, damageType), context.Source);
            appliedCount++;
        }

        return appliedCount;
    }

    public static int Execute(
        AttackShapeSO shape,
        AttackHitContext context,
        DamageType damageType,
        GameObject vfxPrefab,
        Vector3 vfxPosition,
        Quaternion vfxRotation)
    {
        SpawnVfx(vfxPrefab, vfxPosition, vfxRotation);
        return ApplyDamage(shape, context, damageType);
    }

    private static float RollDamage(float min, float max)
    {
        if (max <= min) return min;
        return Random.Range(min, max);
    }
}
