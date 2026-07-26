using UnityEngine;

/// <summary>
/// Per-monster configuration for <see cref="MonsterAIComponent"/>.
/// Create one asset per monster type and assign it on the prefab.
/// </summary>
[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.GameplayMonsters + "/Monster Behaviour Config", fileName = "MonsterBehaviourConfig")]
public class MonsterBehaviorConfigSO : ScriptableObject, ICombatAIConfig
{
    [Header("Animator Parameter")]
    [Tooltip("Integer parameter on the Animator Controller that drives locomotion / attack states.")]
    public string animatorStateName = "State";

    [Tooltip("Exact name of the Attack state inside the Animator Controller (used to force-replay the animation on each swing).")]
    public string attackAnimStateName = "Attack";

    [Header("Animator States")]
    public int idleState = 0;
    public int walkState = 1;
    public int runState = 2;
    public int attackState = 3;
    public int hitState = 4;
    public int deathState = 5;

    [Tooltip("How long the hit animation plays before resuming the previous state. TZ_aggresive_damage_B is 25 frames @ 30fps = 0.83s.")]
    [Min(0f)]
    public float hitAnimDuration = 0.83f;

    [Tooltip("How long the death animation plays before the monster is removed. TZ_death_A is 50 frames @ 30fps = 1.67s.")]
    [Min(0f)]
    public float deathAnimDuration = 1.67f;

    [Header("Detection")]
    [Tooltip("When enabled, monsters only chase and attack players. When disabled, any living HealthComponent can be targeted (never self).")]
    public bool aggroPlayersOnly = true;

    [Min(0.1f)]
    public float aggroRange = 12f;

    [Min(0.1f)]
    public float attackRange = 2f;

    [Min(0.1f)]
    public float leashRange = 20f;

    [Header("Movement")]
    [Min(0.1f)]
    public float chaseSpeed = 4f;

    [Header("Combat")]
    [Min(0f)]
    public float attackDamage = 10f;

    [Min(0.1f)]
    public float attackCooldown = 1.5f;

    [Min(0f)]
    public float attackWindup = 0.4f;

    [Header("Audio")]
    [Tooltip("One-shot clips played when the monster starts an attack swing. A random clip is chosen when multiple are assigned.")]
    public AudioClip[] attackSounds;

    [Range(0f, 1f)]
    public float attackSoundVolume = 1f;

    [Header("Performance LOD")]
    [Tooltip("Full AI tick within this horizontal distance to the nearest player.")]
    [Min(1f)]
    public float lodFullDistance = 25f;

    [Tooltip("Reduced AI tick rate within this distance.")]
    [Min(1f)]
    public float lodReducedDistance = 60f;

    [Tooltip("Minimal AI tick rate within this distance. Beyond = dormant.")]
    [Min(1f)]
    public float lodMinimalDistance = 100f;

    [Min(1)]
    public int lodReducedTickInterval = 3;

    [Min(1)]
    public int lodMinimalTickInterval = 10;

    [Min(1)]
    public int lodRecalcIntervalFrames = 20;

    [Header("Visual LOD")]
    [Tooltip("Full animator updates within this distance (client-local).")]
    [Min(1f)]
    public float visualFullDistance = 30f;

    [Tooltip("Animator culled within this distance. Beyond = renderers disabled.")]
    [Min(1f)]
    public float visualHiddenDistance = 50f;

    [Min(1)]
    public int visualRecalcIntervalFrames = 20;

    public AudioClip GetRandomAttackSound()
    {
        if (attackSounds == null || attackSounds.Length == 0)
            return null;

        return attackSounds[Random.Range(0, attackSounds.Length)];
    }

    public NPCDistanceLODSettings GetAILodSettings() => new()
    {
        fullDistance = lodFullDistance,
        reducedDistance = lodReducedDistance,
        minimalDistance = lodMinimalDistance,
        reducedTickInterval = lodReducedTickInterval,
        minimalTickInterval = lodMinimalTickInterval,
        recalcIntervalFrames = lodRecalcIntervalFrames
    };

    string ICombatAIConfig.animatorStateName => animatorStateName;
    string ICombatAIConfig.attackAnimStateName => attackAnimStateName;
    int ICombatAIConfig.idleState => idleState;
    int ICombatAIConfig.runState => runState;
    int ICombatAIConfig.attackState => attackState;
    int ICombatAIConfig.hitState => hitState;
    int ICombatAIConfig.deathState => deathState;
    float ICombatAIConfig.hitAnimDuration => hitAnimDuration;
    float ICombatAIConfig.aggroRange => aggroRange;
    float ICombatAIConfig.attackRange => attackRange;
    float ICombatAIConfig.leashRange => leashRange;
    float ICombatAIConfig.chaseSpeed => chaseSpeed;
    float ICombatAIConfig.attackDamage => attackDamage;
    float ICombatAIConfig.attackCooldown => attackCooldown;
    float ICombatAIConfig.attackWindup => attackWindup;
}
