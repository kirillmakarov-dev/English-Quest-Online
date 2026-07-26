using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Short-range forward arc attack in front of the attacker.
/// </summary>
[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.CoreCombatAttackShapes + "/Melee Forward", fileName = "AttackShape_MeleeForward")]
public class MeleeForwardAttackShapeSO : AttackShapeSO
{
    [Min(0.1f)]
    public float range = 1.8f;

    [Tooltip("Total arc angle in degrees.")]
    [Range(1f, 180f)]
    public float arcAngle = 90f;

    public override IReadOnlyList<Collider> DetectHits(AttackHitContext context)
    {
        float halfAngle = arcAngle * 0.5f;
        IReadOnlyList<Collider> candidates = AttackShapeDetection.OverlapAt(
            context.Origin,
            range,
            context.TargetLayers,
            context.ExcludeRoot);
        List<Collider> hits = new(candidates.Count);

        foreach (Collider collider in candidates)
        {
            if (AttackShapeDetection.IsExcluded(collider, context.ExcludeRoot)) continue;
            if (!AttackShapeDetection.PassesForwardCone(context.Origin, context.Forward, collider, range, halfAngle))
                continue;

            hits.Add(collider);
        }

        return hits;
    }
}
