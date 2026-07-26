using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject strategy for detecting targets in an attack area.
/// Subclass to add new attack shapes without changing ability behaviours.
/// </summary>
public abstract class AttackShapeSO : ScriptableObject
{
    public abstract IReadOnlyList<Collider> DetectHits(AttackHitContext context);
}
