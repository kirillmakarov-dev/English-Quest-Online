/// <summary>
/// Smooth distance curve for utility scoring. Returns higher values when closer to the ideal band.
/// </summary>
public class DistanceConsideration : IUtilityConsideration
{
    public enum DistanceMode
    {
        PreferCloserThan,
        PreferFartherThan
    }

    private readonly float _threshold;
    private readonly DistanceMode _mode;

    public DistanceConsideration(float threshold, DistanceMode mode)
    {
        _threshold = threshold;
        _mode = mode;
    }

    public float Evaluate(CombatAIContext ctx)
    {
        if (ctx == null || ctx.CurrentTarget == null)
            return 0f;

        float distance = ctx.DistanceToTarget;
        if (_mode == DistanceMode.PreferCloserThan)
            return distance <= _threshold ? 1f : 0f;

        return distance > _threshold ? 1f : 0f;
    }
}
