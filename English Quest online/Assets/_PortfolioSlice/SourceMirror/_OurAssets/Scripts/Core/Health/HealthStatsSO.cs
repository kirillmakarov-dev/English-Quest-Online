using UnityEngine;

/// <summary>
/// Defines the static health parameters for any entity.
/// Create an asset via Assets → Create → English Kingdom → Core → Combat → Health Stats and assign it to <see cref="HealthComponent"/>.
/// </summary>
[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.CoreCombat + "/Health Stats", fileName = "HealthStats_Default")]
public class HealthStatsSO : ScriptableObject
{
    [Header("HP")]
    [Min(1f)]
    public float maxHP = 100f;

    [Header("Regen")]
    [Tooltip("HP restored per second while alive. 0 disables passive regen.")]
    [Min(0f)]
    public float regenPerSecond = 0f;

    [Header("Invincibility")]
    [Tooltip("Seconds of invincibility frames granted after taking a hit. 0 disables i-frames.")]
    [Min(0f)]
    public float invincibilityDurationOnHit = 0.5f;
}
