/// <summary>Inverts another consideration's score.</summary>
public class InvertedConsideration : IUtilityConsideration
{
    private readonly IUtilityConsideration _inner;

    public InvertedConsideration(IUtilityConsideration inner)
    {
        _inner = inner;
    }

    public float Evaluate(CombatAIContext ctx)
    {
        if (_inner == null)
            return 1f;

        float score = _inner.Evaluate(ctx);
        return score >= 1f ? 0f : 1f;
    }
}
