/// <summary>
/// Returns a utility score in [0, 1]. Actions combine consideration scores multiplicatively.
/// </summary>
public interface IUtilityConsideration
{
    float Evaluate(CombatAIContext ctx);
}
