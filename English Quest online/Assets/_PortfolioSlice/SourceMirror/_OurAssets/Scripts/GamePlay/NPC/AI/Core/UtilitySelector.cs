using System.Collections.Generic;

/// <summary>
/// Picks the utility action with the highest score each tick.
/// </summary>
public class UtilitySelector
{
    public IUtilityAction Select(IReadOnlyList<IUtilityAction> actions, CombatAIContext ctx)
    {
        if (actions == null || actions.Count == 0)
            return null;

        IUtilityAction bestAction = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < actions.Count; i++)
        {
            IUtilityAction action = actions[i];
            if (action == null)
                continue;

            float score = action.Evaluate(ctx);
            if (score > bestScore)
            {
                bestScore = score;
                bestAction = action;
            }
        }

        IUtilityAction combatFallback = ResolveCombatFallback(actions, ctx, bestScore);
        return combatFallback ?? bestAction;
    }

    /// <summary>
    /// Prevents a 0-score tie from defaulting to Idle while a valid target is still in leash range.
    /// </summary>
    private static IUtilityAction ResolveCombatFallback(
        IReadOnlyList<IUtilityAction> actions,
        CombatAIContext ctx,
        float bestScore)
    {
        if (bestScore > 0f || ctx?.CurrentTarget == null || ctx.Config == null)
            return null;

        if (ctx.DistanceToTarget > ctx.Config.leashRange)
            return null;

        if (ctx.DistanceToTarget <= ctx.EffectiveAttackRange)
            return FindAction(actions, CombatAIState.Attack);

        if (ctx.DistanceToTarget <= ctx.Config.aggroRange)
            return FindAction(actions, CombatAIState.Chase);

        return null;
    }

    private static IUtilityAction FindAction(IReadOnlyList<IUtilityAction> actions, CombatAIState state)
    {
        for (int i = 0; i < actions.Count; i++)
        {
            if (actions[i] != null && actions[i].State == state)
                return actions[i];
        }

        return null;
    }
}
