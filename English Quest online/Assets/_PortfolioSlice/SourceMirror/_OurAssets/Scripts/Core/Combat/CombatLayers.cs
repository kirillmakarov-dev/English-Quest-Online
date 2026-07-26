using UnityEngine;

/// <summary>
/// Shared layer names and masks for combat hit detection.
/// </summary>
public static class CombatLayers
{
    public const string CombatTargetLayerName = "CombatTarget";

    public static int CombatTarget => LayerMask.NameToLayer(CombatTargetLayerName);

    public static LayerMask DefaultTargetMask
    {
        get
        {
            int layer = CombatTarget;
            return layer >= 0 ? 1 << layer : Physics.DefaultRaycastLayers;
        }
    }
}
