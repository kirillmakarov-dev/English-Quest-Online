/// <summary>Move toward a target that is in aggro range but outside melee range.</summary>
public class ChaseUtilityAction : IUtilityAction
{
    private readonly IUtilityConsideration[] _considerations =
    {
        new TargetExistsConsideration(),
        new InRangeConsideration(CombatRangeType.Aggro),
        new InRangeConsideration(CombatRangeType.Leash),
        new OutsideRangeConsideration(CombatRangeType.Attack, useEffectiveAttackRange: true)
    };

    public CombatAIState State => CombatAIState.Chase;

    public float Evaluate(CombatAIContext ctx)
    {
        return UtilityConsiderationMath.CombineMultiplicative(_considerations, ctx);
    }

    public void OnEnter(CombatAIContext ctx)
    {
        ctx?.Navigation?.Resume(ctx.NavAgent);

        if (ctx?.Config == null)
            return;

        ctx.AnimatorDriver?.ApplyLocomotion(ctx.Config.runState);
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

        ctx.Navigation?.Resume(ctx.NavAgent);
        ctx.Navigation?.Chase(ctx.NavAgent, ctx.CurrentTarget, ctx.Config.chaseSpeed);
        ctx.AnimatorDriver?.ApplyLocomotion(ctx.Config.runState);
        ctx.Navigation?.FaceTarget(ctx.Owner, ctx.CurrentTarget, deltaTime);
    }

    public void OnExit(CombatAIContext ctx)
    {
    }

    private static void ClearTarget(CombatAIContext ctx)
    {
        ctx.CurrentTarget = null;
        ctx.OnTargetCleared?.Invoke();
    }
}
