/// <summary>Returns the highest score from a set of considerations.</summary>
public class AnyConsideration : IUtilityConsideration
{
    private readonly IUtilityConsideration[] _options;

    public AnyConsideration(params IUtilityConsideration[] options)
    {
        _options = options;
    }

    public float Evaluate(CombatAIContext ctx)
    {
        if (_options == null || _options.Length == 0)
            return 0f;

        float best = 0f;
        for (int i = 0; i < _options.Length; i++)
        {
            if (_options[i] == null)
                continue;

            float score = _options[i].Evaluate(ctx);
            if (score > best)
                best = score;
        }

        return best;
    }
}
