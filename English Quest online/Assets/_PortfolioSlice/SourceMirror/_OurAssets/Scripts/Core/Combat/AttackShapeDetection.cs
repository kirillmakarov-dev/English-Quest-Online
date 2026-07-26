using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared helpers for non-alloc overlap queries and target filtering.
/// </summary>
internal static class AttackShapeDetection
{
    private const int MaxOverlapResults = 32;
    private static readonly Collider[] OverlapBuffer = new Collider[MaxOverlapResults];
    private static readonly List<Collider> ResultBuffer = new(MaxOverlapResults);

    public static IReadOnlyList<Collider> OverlapAt(
        Vector3 origin,
        float radius,
        LayerMask layers,
        Component sceneReference = null)
    {
        ResultBuffer.Clear();

        int count = sceneReference != null
            ? PhysicsSceneQueries.OverlapSphereNonAlloc(
                sceneReference,
                origin,
                radius,
                OverlapBuffer,
                layers,
                QueryTriggerInteraction.Collide)
            : Physics.OverlapSphereNonAlloc(origin, radius, OverlapBuffer, layers, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
            ResultBuffer.Add(OverlapBuffer[i]);

        return ResultBuffer;
    }

    public static bool IsExcluded(Collider collider, Transform excludeRoot)
    {
        if (collider == null) return true;
        if (excludeRoot == null) return false;

        Transform hitTransform = collider.transform;
        return hitTransform == excludeRoot || hitTransform.IsChildOf(excludeRoot);
    }

    public static bool TryGetDamageReceiver(Collider collider, out DamageReceiver receiver)
    {
        receiver = collider != null
            ? collider.GetComponentInParent<DamageReceiver>()
            : null;
        return receiver != null;
    }

    public static bool PassesForwardCone(
        Vector3 origin,
        Vector3 forward,
        Collider collider,
        float maxRange,
        float halfAngleDegrees)
    {
        Vector3 closest = collider.ClosestPoint(origin);
        Vector3 toTarget = closest - origin;
        float distance = toTarget.magnitude;
        if (distance <= 0.001f || distance > maxRange) return false;

        float angle = Vector3.Angle(forward, toTarget / distance);
        return angle <= halfAngleDegrees;
    }
}
