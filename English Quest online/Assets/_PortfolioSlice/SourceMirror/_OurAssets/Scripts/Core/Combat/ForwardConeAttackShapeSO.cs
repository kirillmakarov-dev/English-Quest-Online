using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Medium-range cone attack in front of the attacker.
/// </summary>
[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.CoreCombatAttackShapes + "/Forward Cone", fileName = "AttackShape_ForwardCone")]
public class ForwardConeAttackShapeSO : AttackShapeSO
{
    [Min(0.1f)]
    public float range = 8f;

    [Tooltip("Total cone angle in degrees.")]
    [Range(1f, 180f)]
    public float coneAngle = 30f;

    public override IReadOnlyList<Collider> DetectHits(AttackHitContext context)
    {
        float halfAngle = coneAngle * 0.5f;
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
