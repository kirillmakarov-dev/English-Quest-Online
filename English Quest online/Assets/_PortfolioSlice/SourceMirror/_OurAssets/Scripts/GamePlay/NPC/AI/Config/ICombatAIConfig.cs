/// <summary>
/// Combat tuning shared by utility actions and navigation helpers.
/// Implemented by per-archetype ScriptableObject configs.
/// </summary>
public interface ICombatAIConfig
{
    string animatorStateName { get; }
    string attackAnimStateName { get; }
    int idleState { get; }
    int runState { get; }
    int attackState { get; }
    int hitState { get; }
    int deathState { get; }
    float hitAnimDuration { get; }
    float aggroRange { get; }
    float attackRange { get; }
    float leashRange { get; }
    float chaseSpeed { get; }
    float attackDamage { get; }
    float attackCooldown { get; }
    float attackWindup { get; }
}
