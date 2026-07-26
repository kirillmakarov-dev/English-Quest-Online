/// <summary>
/// Multiplies consideration scores together. Empty list returns 1.
/// </summary>
public static class UtilityConsiderationMath
{
    public static float CombineMultiplicative(IUtilityConsideration[] considerations, CombatAIContext ctx)
    {
        if (considerations == null || considerations.Length == 0)
            return 1f;

        float product = 1f;
        for (int i = 0; i < considerations.Length; i++)
        {
            if (considerations[i] == null)
                continue;

            product *= considerations[i].Evaluate(ctx);
        }

        return product;
    }
}
