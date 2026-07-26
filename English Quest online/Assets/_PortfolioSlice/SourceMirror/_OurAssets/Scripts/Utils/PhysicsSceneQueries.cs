using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Physics queries scoped to a Unity scene. Required for Fusion Multi-Peer mode where each
/// <see cref="NetworkRunner"/> simulates in its own physics scene instead of the default one.
/// </summary>
public static class PhysicsSceneQueries
{
    private static readonly Collider[] s_overlapBuffer = new Collider[1];

    public static PhysicsScene Resolve(Scene scene)
    {
        return scene.IsValid() ? scene.GetPhysicsScene() : Physics.defaultPhysicsScene;
    }

    public static PhysicsScene Resolve(Component reference)
    {
        return reference != null ? Resolve(reference.gameObject.scene) : Physics.defaultPhysicsScene;
    }

    public static bool Raycast(
        Component reference,
        Vector3 origin,
        Vector3 direction,
        out RaycastHit hit,
        float maxDistance,
        int layerMask,
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
    {
        return Resolve(reference).Raycast(origin, direction, out hit, maxDistance, layerMask, queryTriggerInteraction);
    }

    public static bool Raycast(
        Component reference,
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        int layerMask,
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
    {
        return Resolve(reference).Raycast(origin, direction, maxDistance, layerMask, queryTriggerInteraction);
    }

    public static bool CheckSphere(
        Component reference,
        Vector3 position,
        float radius,
        int layerMask,
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
    {
        return Resolve(reference).OverlapSphere(
            position,
            radius,
            s_overlapBuffer,
            layerMask,
            queryTriggerInteraction) > 0;
    }

    public static int OverlapSphereNonAlloc(
        Component reference,
        Vector3 position,
        float radius,
        Collider[] results,
        int layerMask,
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide)
    {
        if (results == null || results.Length == 0)
            return 0;

        return Resolve(reference).OverlapSphere(
            position,
            radius,
            results,
            layerMask,
            queryTriggerInteraction);
    }
}
