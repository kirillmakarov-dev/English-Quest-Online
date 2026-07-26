using UnityEngine;

public enum CombatRangeType
{
    Aggro,
    Attack,
    Leash
}

/// <summary>Returns 1 when horizontal distance to target is within the configured range.</summary>
public class InRangeConsideration : IUtilityConsideration
{
    private readonly CombatRangeType _rangeType;
    private readonly bool _useEffectiveAttackRange;

    public InRangeConsideration(CombatRangeType rangeType, bool useEffectiveAttackRange = false)
    {
        _rangeType = rangeType;
        _useEffectiveAttackRange = useEffectiveAttackRange;
    }

    public float Evaluate(CombatAIContext ctx)
    {
        if (ctx == null || ctx.Config == null || ctx.CurrentTarget == null)
            return 0f;

        float range = GetRange(ctx);
        return ctx.DistanceToTarget <= range ? 1f : 0f;
    }

    private float GetRange(CombatAIContext ctx)
    {
        return _rangeType switch
        {
            CombatRangeType.Aggro => ctx.Config.aggroRange,
            CombatRangeType.Attack => _useEffectiveAttackRange ? ctx.EffectiveAttackRange : ctx.Config.attackRange,
            CombatRangeType.Leash => ctx.Config.leashRange,
            _ => 0f
        };
    }
}

/// <summary>Returns 1 when horizontal distance to target is outside the configured range.</summary>
public class OutsideRangeConsideration : IUtilityConsideration
{
    private readonly CombatRangeType _rangeType;
    private readonly bool _useEffectiveAttackRange;

    public OutsideRangeConsideration(CombatRangeType rangeType, bool useEffectiveAttackRange = false)
    {
        _rangeType = rangeType;
        _useEffectiveAttackRange = useEffectiveAttackRange;
    }

    public float Evaluate(CombatAIContext ctx)
    {
        if (ctx == null || ctx.Config == null || ctx.CurrentTarget == null)
            return 1f;

        float range = GetRange(ctx);
        return ctx.DistanceToTarget > range ? 1f : 0f;
    }

    private float GetRange(CombatAIContext ctx)
    {
        return _rangeType switch
        {
            CombatRangeType.Aggro => ctx.Config.aggroRange,
            CombatRangeType.Attack => _useEffectiveAttackRange ? ctx.EffectiveAttackRange : ctx.Config.attackRange,
            CombatRangeType.Leash => ctx.Config.leashRange,
            _ => 0f
        };
    }
}
