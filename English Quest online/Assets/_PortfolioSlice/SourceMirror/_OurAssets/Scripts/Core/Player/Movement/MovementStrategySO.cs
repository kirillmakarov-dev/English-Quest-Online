using UnityEngine;

public abstract class MovementStrategySO : ScriptableObject
{
    public virtual void Initialize(MovementContext context)
    {
        // Stateless, no initialization needed
    }

    public abstract void TickInput(MovementContext ctx);
    public abstract void FixedMove(MovementContext ctx);
    public abstract bool CheckIsFlying(MovementContext ctx);

    public virtual void Dispose()
    {
    }
}
