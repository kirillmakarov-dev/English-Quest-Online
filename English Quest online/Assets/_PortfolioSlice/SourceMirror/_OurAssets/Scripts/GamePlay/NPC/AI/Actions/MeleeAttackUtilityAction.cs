/// <summary>Melee attack with windup, hit event, and cooldown sub-state.</summary>
public class MeleeAttackUtilityAction : IUtilityAction
{
    private const float ActiveAttackInertiaScore = 1.5f;
    private const float ChainAttackTimerSeed = 0.0001f;

    private readonly IUtilityConsideration[] _startConsiderations;
    private readonly AttackInProgressConsideration _inProgressConsideration;

    private float _attackTimer;
    private int _swingGeneration;
    private int _hitSwingGeneration = -1;

    public MeleeAttackUtilityAction()
    {
        _inProgressConsideration = new AttackInProgressConsideration(() => _attackTimer);
        _startConsiderations = new IUtilityConsideration[]
        {
            new TargetExistsConsideration(),
            new InRangeConsideration(CombatRangeType.Leash),
            new InRangeConsideration(CombatRangeType.Attack, useEffectiveAttackRange: true),
            new AttackReadyConsideration(() => _attackTimer)
        };
    }

    public CombatAIState State => CombatAIState.Attack;

    public float Evaluate(CombatAIContext ctx)
    {
        if (_inProgressConsideration.Evaluate(ctx) >= 1f)
            return ActiveAttackInertiaScore;

        return UtilityConsiderationMath.CombineMultiplicative(_startConsiderations, ctx);
    }

    public void OnEnter(CombatAIContext ctx)
    {
        BeginSwing();

        ctx?.Navigation?.Stop(ctx.NavAgent);
        ctx?.Navigation?.ResetPath(ctx.NavAgent);
        ctx?.AnimatorDriver?.PlayAttackAnimation();
        ctx?.OnAttackSwingStarted?.Invoke();
    }

    public void Execute(CombatAIContext ctx, float deltaTime)
    {
        if (ctx?.CurrentTarget == null)
            return;

        if (ctx.DistanceToTarget > ctx.Config.leashRange)
        {
            ClearTarget(ctx);
            return;
        }

        ctx.Navigation?.Stop(ctx.NavAgent);
        ctx.Navigation?.FaceTarget(ctx.Owner, ctx.CurrentTarget, deltaTime);
        ctx.AnimatorDriver?.ApplyLocomotion(ctx.Config.attackState);

        _attackTimer += deltaTime;

        if (_hitSwingGeneration != _swingGeneration &&
            _attackTimer >= ctx.Config.attackWindup)
        {
            _hitSwingGeneration = _swingGeneration;

            if (IsTargetInHitRange(ctx))
            {
                var info = new DamageInfo(ctx.Config.attackDamage, DamageType.Physical);
                ctx.OnAttackHit?.Invoke(ctx.CurrentTarget, ctx.Config.attackDamage, info);
            }
        }

        if (_attackTimer < ctx.Config.attackCooldown)
            return;

        if (_hitSwingGeneration != _swingGeneration)
            return;

        bool stillInMeleeRange = ctx.Navigation != null &&
            ctx.DistanceToTarget <= GetChainAttackRange(ctx);

        if (stillInMeleeRange)
        {
            BeginSwing();
            ctx.AnimatorDriver?.PlayAttackAnimation();
            ctx.OnAttackSwingStarted?.Invoke();
        }
        else
        {
            _attackTimer = 0f;
        }
    }

    public void OnExit(CombatAIContext ctx)
    {
        _attackTimer = 0f;
        _swingGeneration = 0;
        _hitSwingGeneration = -1;
    }

    /// <summary>
    /// Cancels damage for the current swing when the monster is hit during windup.
    /// Marks the swing as resolved without invoking <see cref="CombatAIContext.OnAttackHit"/>.
    /// </summary>
    public void CancelPendingHit(CombatAIContext ctx)
    {
        if (ctx?.Config == null || _attackTimer <= 0f)
            return;

        if (_hitSwingGeneration == _swingGeneration)
            return;

        _hitSwingGeneration = _swingGeneration;
    }

    private void BeginSwing()
    {
        _swingGeneration++;
        _attackTimer = ChainAttackTimerSeed;
    }

    private static void ClearTarget(CombatAIContext ctx)
    {
        ctx.CurrentTarget = null;
        ctx.OnTargetCleared?.Invoke();
    }

    private static bool IsTargetInHitRange(CombatAIContext ctx)
    {
        return ctx.Navigation != null &&
            ctx.Navigation.IsWithinAttackRange(ctx.DistanceToTarget, ctx.EffectiveAttackRange);
    }

    private static float GetChainAttackRange(CombatAIContext ctx)
    {
        return ctx.EffectiveAttackRange * 1.2f;
    }
}
