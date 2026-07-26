/// <summary>
/// A competing behavior evaluated and executed by <see cref="CombatAIBrain"/>.
/// </summary>
public interface IUtilityAction
{
    CombatAIState State { get; }

    /// <summary>Utility score used by <see cref="UtilitySelector"/>; may exceed 1 for action inertia.</summary>
    float Evaluate(CombatAIContext ctx);

    void OnEnter(CombatAIContext ctx);
    void Execute(CombatAIContext ctx, float deltaTime);
    void OnExit(CombatAIContext ctx);
}
