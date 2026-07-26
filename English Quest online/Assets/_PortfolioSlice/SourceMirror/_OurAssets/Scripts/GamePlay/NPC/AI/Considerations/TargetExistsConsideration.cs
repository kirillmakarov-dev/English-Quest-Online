/// <summary>Returns 1 when a target exists, otherwise 0.</summary>
public class TargetExistsConsideration : IUtilityConsideration
{
    public float Evaluate(CombatAIContext ctx)
    {
        return ctx != null && ctx.CurrentTarget != null ? 1f : 0f;
    }
}
