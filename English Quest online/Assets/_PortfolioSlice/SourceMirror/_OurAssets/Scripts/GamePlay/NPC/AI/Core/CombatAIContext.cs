using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Runtime bag passed to utility actions, considerations, and shared services.
/// </summary>
public class CombatAIContext
{
    public Transform Owner;
    public NavMeshAgent NavAgent;
    public ICombatAIConfig Config;
    public Transform CurrentTarget;
    public bool IsDead;

    public NPCAnimatorDriver AnimatorDriver;
    public NPCNavigationHelper Navigation;

    public float DistanceToTarget { get; private set; }
    public float EffectiveAttackRange { get; private set; }

    public CombatAIState CurrentState { get; set; } = CombatAIState.Idle;
    public IUtilityAction ActiveAction { get; set; }

    public Action<Transform, float, DamageInfo> OnAttackHit;
    public Action OnAttackSwingStarted;
    public Action OnTargetCleared;

    public void RefreshMetrics()
    {
        if (Config == null)
        {
            DistanceToTarget = float.MaxValue;
            EffectiveAttackRange = 0f;
            return;
        }

        DistanceToTarget = Navigation != null
            ? Navigation.DistanceToTarget(Owner, CurrentTarget)
            : float.MaxValue;

        EffectiveAttackRange = Navigation != null
            ? Navigation.GetEffectiveAttackRange(Config, NavAgent)
            : Config.attackRange;
    }
}
