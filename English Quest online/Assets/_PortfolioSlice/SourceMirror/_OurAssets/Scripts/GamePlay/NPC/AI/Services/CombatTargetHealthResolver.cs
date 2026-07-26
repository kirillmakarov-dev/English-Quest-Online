using UnityEngine;

/// <summary>
/// Resolves health components on combat targets. Player health lives on nested modules,
/// so callers must search children as well as parents.
/// </summary>
public static class CombatTargetHealthResolver
{
    public static NetworkedHealthSync FindHealthSync(Transform target)
    {
        if (target == null)
            return null;

        return target.GetComponent<NetworkedHealthSync>()
            ?? target.GetComponentInChildren<NetworkedHealthSync>()
            ?? target.GetComponentInParent<NetworkedHealthSync>();
    }

    public static HealthComponent FindHealth(Transform target)
    {
        if (target == null)
            return null;

        return target.GetComponent<HealthComponent>()
            ?? target.GetComponentInChildren<HealthComponent>()
            ?? target.GetComponentInParent<HealthComponent>();
    }
}
