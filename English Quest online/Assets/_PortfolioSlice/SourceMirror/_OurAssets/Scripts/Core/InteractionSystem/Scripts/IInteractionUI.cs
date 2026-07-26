/// <summary>
/// Abstraction over the interaction prompt UI panel.
/// Decouples <see cref="PlayerInteractionController"/> from the concrete MonoBehaviour,
/// allowing the controller logic to be tested without a scene.
/// </summary>
public interface IInteractionUI
{
    void Show(string message, string button = "E");
    void Hide();
}
