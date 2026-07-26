using Fusion;
using UnityEngine;

public enum NPCDistanceTier
{
    Full = 0,
    Reduced = 1,
    Minimal = 2,
    Dormant = 3
}

[System.Serializable]
public struct NPCDistanceLODSettings
{
    [Min(1f)]
    public float fullDistance;

    [Min(1f)]
    public float reducedDistance;

    [Min(1f)]
    public float minimalDistance;

    [Min(1)]
    public int reducedTickInterval;

    [Min(1)]
    public int minimalTickInterval;

    [Min(1)]
    public int recalcIntervalFrames;

    public static NPCDistanceLODSettings Default => new()
    {
        fullDistance = 25f,
        reducedDistance = 60f,
        minimalDistance = 100f,
        reducedTickInterval = 3,
        minimalTickInterval = 10,
        recalcIntervalFrames = 20
    };
}

/// <summary>
/// Shared distance-to-player helpers for NPC AI and visual LOD tiers.
/// LOD is always computed client-locally — never networked.
/// </summary>
public static class NPCDistanceLOD
{
    public static Transform FindNearestPlayerTransform(NetworkRunner runner, Vector3 from)
    {
        Transform nearest = null;
        float nearestSqDist = float.MaxValue;

        if (runner != null)
        {
            foreach (PlayerRef playerRef in runner.ActivePlayers)
            {
                NetworkObject playerObj = runner.GetPlayerObject(playerRef);
                if (playerObj == null)
                    continue;

                float sqDist = HorizontalSqrDistance(from, playerObj.transform.position);
                if (sqDist >= nearestSqDist)
                    continue;

                nearestSqDist = sqDist;
                nearest = playerObj.transform;
            }
        }

        if (nearest != null)
            return nearest;

        Transform localFallback = runner != null
            ? LocalPlayerRegistry.GetLocalTransform(runner)
            : FindOfflinePlayerTransform();

        if (localFallback == null)
            return null;

        float fallbackSqDist = HorizontalSqrDistance(from, localFallback.position);
        return fallbackSqDist < nearestSqDist ? localFallback : nearest;
    }

    public static float HorizontalDistance(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        delta.y = 0f;
        return delta.magnitude;
    }

    public static NPCDistanceTier EvaluateTier(float distance, NPCDistanceLODSettings settings)
    {
        if (distance < settings.fullDistance)
            return NPCDistanceTier.Full;

        if (distance < settings.reducedDistance)
            return NPCDistanceTier.Reduced;

        if (distance < settings.minimalDistance)
            return NPCDistanceTier.Minimal;

        return NPCDistanceTier.Dormant;
    }

    public static int TickIntervalForTier(NPCDistanceTier tier, NPCDistanceLODSettings settings)
    {
        return tier switch
        {
            NPCDistanceTier.Full => 1,
            NPCDistanceTier.Reduced => settings.reducedTickInterval,
            NPCDistanceTier.Minimal => settings.minimalTickInterval,
            _ => int.MaxValue
        };
    }

    private static float HorizontalSqrDistance(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        delta.y = 0f;
        return delta.sqrMagnitude;
    }

    private static Transform FindOfflinePlayerTransform()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform : null;
    }
}
