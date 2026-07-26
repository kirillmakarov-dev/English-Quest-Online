/// <summary>Stand still when there is no valid aggro target.</summary>
public class IdleUtilityAction : IUtilityAction
{
    private readonly IUtilityConsideration[] _considerations =
    {
        new AnyConsideration(
            new InvertedConsideration(new TargetExistsConsideration()),
            new OutsideRangeConsideration(CombatRangeType.Aggro))
    };

    public CombatAIState State => CombatAIState.Idle;

    public float Evaluate(CombatAIContext ctx)
    {
        return UtilityConsiderationMath.CombineMultiplicative(_considerations, ctx);
    }

    public void OnEnter(CombatAIContext ctx)
    {
        if (ctx?.Navigation == null)
            return;

        ctx.Navigation.Stop(ctx.NavAgent);
        ctx.Navigation.ResetPath(ctx.NavAgent);

        if (ctx.Config == null)
            return;

        ctx.AnimatorDriver?.ApplyLocomotion(ctx.Config.idleState);
    }

    public void Execute(CombatAIContext ctx, float deltaTime)
    {
    }

    public void OnExit(CombatAIContext ctx)
    {
    }
}
