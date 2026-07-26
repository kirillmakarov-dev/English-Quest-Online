using UnityEngine;

public interface IInteractable
{
    // The prompt to show in UI (e.g., "Press E to Pick Up")
    string InteractionPrompt { get; }

    // If false, interaction should not be shown or triggered.
    bool CanInteract { get; }

    // Called when the player presses the interaction key
    // Returns true if the interaction was successful/started
    bool Interact(PlayerInteraction interactor);
}
