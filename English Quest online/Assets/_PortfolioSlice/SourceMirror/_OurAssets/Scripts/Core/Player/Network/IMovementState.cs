/// <summary>
/// Exposes the movement state properties that the animator needs.
/// Decouples PlayerAnimator from the concrete PlayerMovement class.
/// </summary>
public interface IMovementState
{
    bool IsGrounded { get; }
    float VerticalVelocity { get; }
}
