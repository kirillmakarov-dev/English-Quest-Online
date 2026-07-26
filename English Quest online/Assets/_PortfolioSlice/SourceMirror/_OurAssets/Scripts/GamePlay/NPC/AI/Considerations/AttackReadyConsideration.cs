using System;

/// <summary>Returns 1 when the melee attack cooldown has elapsed.</summary>
public class AttackReadyConsideration : IUtilityConsideration
{
    private readonly Func<float> _getAttackTimer;

    public AttackReadyConsideration(Func<float> getAttackTimer)
    {
        _getAttackTimer = getAttackTimer;
    }

    public float Evaluate(CombatAIContext ctx)
    {
        if (ctx == null || ctx.Config == null)
            return 0f;

        float attackTimer = _getAttackTimer != null ? _getAttackTimer() : 0f;
        return attackTimer >= ctx.Config.attackCooldown || attackTimer <= 0.001f ? 1f : 0f;
    }
}

/// <summary>Returns 1 while an attack cycle is active (windup through cooldown).</summary>
public class AttackInProgressConsideration : IUtilityConsideration
{
    private readonly Func<float> _getAttackTimer;

    public AttackInProgressConsideration(Func<float> getAttackTimer)
    {
        _getAttackTimer = getAttackTimer;
    }

    public float Evaluate(CombatAIContext ctx)
    {
        if (ctx == null || ctx.Config == null)
            return 0f;

        float attackTimer = _getAttackTimer != null ? _getAttackTimer() : 0f;
        return attackTimer > 0f ? 1f : 0f;
    }
}
