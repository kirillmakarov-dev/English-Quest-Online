using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 360-degree radial attack centered on the attacker.
/// </summary>
[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.CoreCombatAttackShapes + "/Radial", fileName = "AttackShape_Radial")]
public class RadialAttackShapeSO : AttackShapeSO
{
    [Min(0.1f)]
    public float radius = 4f;

    public override IReadOnlyList<Collider> DetectHits(AttackHitContext context)
    {
        IReadOnlyList<Collider> candidates = AttackShapeDetection.OverlapAt(
            context.Origin,
            radius,
            context.TargetLayers,
            context.ExcludeRoot);
        List<Collider> hits = new(candidates.Count);

        foreach (Collider collider in candidates)
        {
            if (AttackShapeDetection.IsExcluded(collider, context.ExcludeRoot)) continue;
            hits.Add(collider);
        }

        return hits;
    }
}
