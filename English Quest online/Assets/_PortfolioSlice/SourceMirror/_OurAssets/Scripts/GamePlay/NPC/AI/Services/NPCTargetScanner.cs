using UnityEngine;

/// <summary>
/// Offline aggro scan — nearest valid hostile within range.
/// By default only players are valid targets; monsters never target themselves.
/// </summary>
public static class NPCTargetScanner
{
    private const int OverlapBufferSize = 64;
    private const float PlayerCacheRefreshSeconds = 0.5f;

    private static readonly Collider[] s_overlapBuffer = new Collider[OverlapBufferSize];
    private static PlayerRoot[] s_playerRoots = System.Array.Empty<PlayerRoot>();
    private static float s_playerRootsRefreshAt;

    public static Transform FindNearestAggroTarget(Transform owner, float radius, bool playersOnly = true)
    {
        if (owner == null || radius <= 0f)
            return null;

        if (playersOnly)
            return FindNearestPlayerTarget(owner, radius);

        int hitCount = Physics.OverlapSphereNonAlloc(
            owner.position,
            radius,
            s_overlapBuffer,
            ~0,
            QueryTriggerInteraction.Collide);

        Transform nearest = null;
        float nearestSq = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = s_overlapBuffer[i];
            if (hit == null)
                continue;

            if (IsSameEntity(owner, hit.transform))
                continue;

            if (!TryResolveCandidate(hit, out Transform candidate))
                continue;

            if (!IsValidAggroTarget(owner, candidate, playersOnly: false))
                continue;

            Vector3 delta = candidate.position - owner.position;
            delta.y = 0f;
            float sq = delta.sqrMagnitude;
            if (sq < nearestSq)
            {
                nearestSq = sq;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private static Transform FindNearestPlayerTarget(Transform owner, float radius)
    {
        RefreshPlayerRootsIfNeeded();

        float radiusSq = radius * radius;
        Transform nearest = null;
        float nearestSq = float.MaxValue;

        for (int i = 0; i < s_playerRoots.Length; i++)
        {
            PlayerRoot root = s_playerRoots[i];
            if (root == null)
                continue;

            Transform candidate = root.transform;
            if (!IsValidAggroTarget(owner, candidate, playersOnly: true))
                continue;

            Vector3 delta = candidate.position - owner.position;
            delta.y = 0f;
            float sq = delta.sqrMagnitude;
            if (sq > radiusSq || sq >= nearestSq)
                continue;

            nearestSq = sq;
            nearest = candidate;
        }

        return nearest;
    }

    private static void RefreshPlayerRootsIfNeeded()
    {
        if (Time.unscaledTime < s_playerRootsRefreshAt)
            return;

        s_playerRootsRefreshAt = Time.unscaledTime + PlayerCacheRefreshSeconds;
        s_playerRoots = Object.FindObjectsByType<PlayerRoot>(FindObjectsSortMode.None);
    }

    public static bool IsSameEntity(Transform a, Transform b)
    {
        if (a == null || b == null)
            return false;

        return a == b || a.IsChildOf(b) || b.IsChildOf(a);
    }

    public static bool IsPlayerTarget(Transform candidate)
    {
        if (candidate == null)
            return false;

        if (candidate.CompareTag("Player"))
            return true;

        return candidate.GetComponentInParent<PlayerRoot>() != null;
    }

    public static bool IsValidAggroTarget(Transform owner, Transform candidate, bool playersOnly)
    {
        if (candidate == null || IsSameEntity(owner, candidate))
            return false;

        if (playersOnly)
            return IsPlayerTarget(candidate);

        if (IsPlayerTarget(candidate))
            return true;

        HealthComponent health = CombatTargetHealthResolver.FindHealth(candidate);
        return health != null && health.IsAlive;
    }

    private static bool TryResolveCandidate(Collider hit, out Transform candidate)
    {
        candidate = null;
        if (hit == null)
            return false;

        if (IsPlayerTarget(hit.transform))
        {
            candidate = hit.transform.root;
            return true;
        }

        HealthComponent health = hit.GetComponentInParent<HealthComponent>();
        if (health == null || !health.IsAlive)
            return false;

        candidate = health.transform;
        return true;
    }
}
