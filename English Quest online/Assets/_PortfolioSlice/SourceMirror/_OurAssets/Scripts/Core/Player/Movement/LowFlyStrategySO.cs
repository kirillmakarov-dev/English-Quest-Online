using UnityEngine;

[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.GameplayAnimalsMovement + "/Low Fly", fileName = "LowFlyStrategy")]
public class LowFlyStrategySO : MovementStrategySO
{
    [Header("Ground Movement")]
    public float acceleration = 25f;
    public bool useAcceleration = true;
    public float sprintMultiplier = 1.5f;

    [Header("Jump")]
    public float maxJumpHeight = 1.5f;
    public float upwardDamping = 18f;

    [Header("Flight")]
    public float flySpeedMultiplier = 1.1f;
    public float hoverHeight = 1.2f;
    public float hoverForce = 25f;
    public float hoverDamping = 6f;
    public float autoLandDistance = 0.35f;

    public override bool CheckIsFlying(MovementContext ctx) => ctx != null && ctx.isFlying;

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

        if (ctx.flyTogglePressed)
        {
            SetFlying(ctx, !ctx.isFlying);
        }

        if (ctx.isFlying)
        {
            FlyMove(ctx);
        }
        else
        {
            GroundMove(ctx);
        }
    }

    private void GroundMove(MovementContext ctx)
    {
        // Reset gravity just in case it was disabled for flying
        // We'll rely on default gravity which might be set on the NCC component
        // Typically NCC has a public gravity field.
        // ctx.ncc.gravity = -20f; // Assuming standard value
        
        Vector3 direction = new Vector3(ctx.inputX, 0, ctx.inputZ).normalized;
        float speedMultiplier = (ctx.sprintHeld && ctx.ncc.Grounded) ? sprintMultiplier : 1f;

        if (direction.magnitude > 0.01f)
        {
            ctx.ncc.maxSpeed = ctx.moveSpeed * speedMultiplier;
            ctx.ncc.Move(direction);
        }
        else
        {
            ctx.ncc.Move(Vector3.zero);
        }

        if (ctx.jumpPressed && ctx.ncc.Grounded)
        {
            ctx.ncc.Jump(false, ctx.jumpImpulse);
        }
        
        // Update Context State
        ctx.verticalVel = ctx.ncc.Velocity.y;
        Vector3 vel = ctx.ncc.Velocity;
        float planarSpeed = new Vector3(vel.x, 0f, vel.z).magnitude;
        float maxSpeed = Mathf.Max(0.01f, ctx.moveSpeed * speedMultiplier);
        ctx.speed01 = Mathf.Clamp01(planarSpeed / maxSpeed);
        ctx.grounded = ctx.ncc.Grounded;
    }

    private void FlyMove(MovementContext ctx)
    {
        // For flying with NCC, we need to disable gravity logic usually or counteract it.
        // Since we can't easily change internal private state, we'll try to use Move() 
        // but we might need to manually set Velocity for full control if standard Move applies gravity.
        
        // However, standard Move() applies gravity: moveVelocity.y += gravity * Runner.DeltaTime;
        // To fly, we can set gravity to 0 on the component temporarily.
        
        float originalGravity = ctx.ncc.gravity;
        ctx.ncc.gravity = 0f; // Disable gravity for flight

        float flySpeed = ctx.moveSpeed * flySpeedMultiplier;
        Vector3 direction = new Vector3(ctx.inputX, 0, ctx.inputZ).normalized;
        
        // Calculate velocity manually for flight to get that "slide" feel or just set it
        // We'll use simple movement for now
        
        Vector3 targetVelocity = direction * flySpeed;
        
        // Apply acceleration manually since we are bypassing standard Move-with-gravity potentially
        // Actually, we can just use Move(direction) and since gravity is 0, it won't fall.
        // But Move() clamps to maxSpeed.
        
        ctx.ncc.maxSpeed = flySpeed;
        ctx.ncc.Move(direction);
        
        // Check for hover height
        ApplyHover(ctx);
        
        // Restore gravity? No, we are flying. We need to remember to restore it when landing/switching state.
        // This is tricky with SOs. Ideally `SetFlying` handles state changes on the NCC.
        
        // Let's reset it at the end of frame? No, that would apply gravity next frame.
        // We handled it in SetFlying
    }

    private void ApplyHover(MovementContext ctx)
    {
        // Simple hover logic: Raycast down, if close to ground, push up.
        // Since we are using NCC, we can just modifying Velocity directly.
        
        float castDistance = hoverHeight + 2f;
        Ray ray = new Ray(ctx.playerTransform.position + Vector3.up * 0.1f, Vector3.down);
        
        if (PhysicsSceneQueries.Raycast(ctx.playerTransform, ray.origin, ray.direction, out RaycastHit hit, castDistance, ctx.groundMask, QueryTriggerInteraction.Ignore))
        {
             // If we are too low, add upward velocity
             if (hit.distance < hoverHeight)
             {
                 float error = hoverHeight - hit.distance;
                 // Add upward velocity to correct
                 Vector3 vel = ctx.ncc.Velocity;
                 vel.y += error * hoverForce * ctx.deltaTime;
                 // Damping
                 vel.y -= vel.y * hoverDamping * ctx.deltaTime;
                 ctx.ncc.Velocity = vel;
             }
             
             // Auto landing logic
             if (hit.distance <= autoLandDistance && ctx.ncc.Velocity.y <= 0f)
             {
                 SetFlying(ctx, false);
             }
        }
    }

    private void SetFlying(MovementContext ctx, bool value)
    {
        ctx.isFlying = value;
        if (ctx.isFlying)
        {
             // Start flying
             ctx.ncc.gravity = 0f;
             Vector3 vel = ctx.ncc.Velocity;
             ctx.ncc.Velocity = new Vector3(vel.x, 0f, vel.z);
        }
        else
        {
            // Stop flying
            // Restore gravity to default (assuming -20 or whatever it was).
            // Hardcoding or we need to store it in Context.
            ctx.ncc.gravity = -20f; 
        }
    }
}
