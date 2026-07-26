using UnityEngine;

/// <summary>
/// Describes the nature of a single damage event.
/// Has zero Fusion imports — <see cref="NetworkedHealthSync"/> converts to/from primitives for RPCs.
/// </summary>
public enum DamageType
{
    Physical,
    Magic,
    True    // Bypasses all resistances and modifiers
}

/// <summary>
/// Lightweight value type passed through the health pipeline alongside a damage amount.
/// </summary>
public struct DamageInfo
{
    public float RawAmount;
    public DamageType Type;

    /// <summary>When true, <see cref="HitDirection"/> is a normalized horizontal push-away vector.</summary>
    public bool HasHitDirection;

    /// <summary>Normalized horizontal direction away from the damage source.</summary>
    public Vector3 HitDirection;

    public DamageInfo(float rawAmount, DamageType type = DamageType.Physical)
    {
        RawAmount = rawAmount;
        Type = type;
        HasHitDirection = false;
        HitDirection = Vector3.zero;
    }

    /// <summary>
    /// Returns a copy with <see cref="HitDirection"/> computed from source to target (Y flattened).
    /// </summary>
    public static DamageInfo WithHitDirection(DamageInfo info, GameObject source, Vector3 targetWorldPosition)
    {
        if (source == null)
            return info;

        Vector3 delta = targetWorldPosition - source.transform.position;
        delta.y = 0f;
        if (delta.sqrMagnitude < 0.001f)
            return info;

        info.HasHitDirection = true;
        info.HitDirection = delta.normalized;
        return info;
    }
}
