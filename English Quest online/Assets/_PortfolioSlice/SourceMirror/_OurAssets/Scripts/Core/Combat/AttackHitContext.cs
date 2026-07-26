using UnityEngine;

/// <summary>
/// Runtime data passed to attack shape strategies during hit detection.
/// </summary>
public struct AttackHitContext
{
    public Vector3 Origin;
    public Vector3 Forward;
    public Transform ExcludeRoot;
    public LayerMask TargetLayers;
    public GameObject Source;
    public float DamageMin;
    public float DamageMax;
    public int MaxTargets;

    public AttackHitContext(
        Vector3 origin,
        Vector3 forward,
        Transform excludeRoot,
        LayerMask targetLayers,
        GameObject source,
        float damageMin = 0f,
        float damageMax = 0f,
        int maxTargets = 0)
    {
        Origin = origin;
        Forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        ExcludeRoot = excludeRoot;
        TargetLayers = targetLayers;
        Source = source;
        DamageMin = damageMin;
        DamageMax = damageMax;
        MaxTargets = maxTargets;
    }
}
