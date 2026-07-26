using UnityEngine;

/// <summary>
/// Contract for entities that can receive combat damage.
/// </summary>
public interface IDamageable
{
    void ApplyDamage(float amount, DamageInfo info, GameObject source);
}
