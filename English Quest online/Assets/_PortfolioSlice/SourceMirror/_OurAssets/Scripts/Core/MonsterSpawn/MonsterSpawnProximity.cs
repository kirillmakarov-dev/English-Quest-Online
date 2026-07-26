using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure proximity helpers for open-world monster spawn activation (XZ plane).
/// </summary>
public static class MonsterSpawnProximity
{
    public static bool IsWithinRadiusXZ(Vector3 point, Vector3 player, float radius)
    {
        float dx = point.x - player.x;
        float dz = point.z - player.z;
        return (dx * dx) + (dz * dz) <= radius * radius;
    }

    public static bool IsNearAnyPlayer(Vector3 point, IReadOnlyList<Vector3> players, float radius)
    {
        if (players == null || players.Count == 0 || radius <= 0f)
            return false;

        for (int i = 0; i < players.Count; i++)
        {
            if (IsWithinRadiusXZ(point, players[i], radius))
                return true;
        }

        return false;
    }
}
