using UnityEngine;

[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.GameplayAnimalsMovement + "/Ground", fileName = "GroundMoveStrategy")]
public class GroundMoveStrategySO : MovementStrategySO
{
    [Header("Tuning")]
    public float acceleration = 25f;
    public bool useAcceleration = true;
    public float sprintMultiplier = 1.5f;

    [Header("Jump")]
    public float maxJumpHeight = 1.5f;

    public override bool CheckIsFlying(MovementContext ctx) => false;

    public override void TickInput(MovementContext ctx)
    {
        if (ctx == null)
        {
            return;
        }

        ctx.inputX = Input.GetAxisRaw("Horizontal");
        ctx.inputZ = Input.GetAxisRaw("Vertical");
        ctx.jumpPressed = Input.GetKeyDown(KeyCode.Space);
        ctx.flyTogglePressed = Input.GetKeyDown(KeyCode.F);
        ctx.sprintHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    public override void FixedMove(MovementContext ctx)
    {
        if (ctx == null || ctx.ncc == null)
        {
            return;
        }

        ctx.isFlying = false;

        // Apply settings to NCC
        // Note: NCC usually has its own settings, but we can override or use Move(dir) which handles it.
        // For simplicity with standard Fusion NCC, we'll direct the move.
        
        Vector3 direction = new Vector3(ctx.inputX, 0, ctx.inputZ).normalized;
        // Use the context grounded check (from GroundDetector) instead of ncc.Grounded directly, as ncc.Grounded can be unstable on slopes
        float speedMultiplier = ctx.sprintHeld ? sprintMultiplier : 1f;

        if (direction.magnitude > 0.01f)
        {
            // Calculate move direction with speed
            // NCC Move() often takes a direction vector relative to speed * time, OR just a direction to move in this frame.
            // Standard Fusion NetworkCharacterController.Move(direction) takes a direction and handles delta internally for velocity application
            // But we want to apply speed.
            
            // Re-reading NCC source snippet: 
            // "if (direction == default) ... else { horizontalVel = ... + direction * acceleration * deltaTime ... }"
            // params: public float maxSpeed;
            
            // We should update the maxSpeed of the controller to match our form
            ctx.ncc.maxSpeed = ctx.moveSpeed * speedMultiplier;
            ctx.ncc.Move(direction);
        }
        else
        {
            ctx.ncc.Move(Vector3.zero);
        }

        if (ctx.jumpPressed && ctx.grounded)
        {
            ctx.ncc.Jump(true, ctx.jumpImpulse); // Try to use jumpImpulse from context/form
        }

        // Update Context State for Animation
        ctx.verticalVel = ctx.ncc.Velocity.y;
        Vector3 vel = ctx.ncc.Velocity;
        float planarSpeed = new Vector3(vel.x, 0f, vel.z).magnitude;
        float maxSpeed = Mathf.Max(0.01f, ctx.moveSpeed * speedMultiplier);
        ctx.speed01 = Mathf.Clamp01(planarSpeed / maxSpeed);
    }
}
