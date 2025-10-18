using UnityEngine;

public interface IInteractable
{
    // A method to get a prompt string, e.g., "Press E to pick up"
    string GetInteractionPrompt();

    // The main interaction method
    void Interact(PlayerInteractor interactor);

    // Methods for highlighting the object
    void Highlight();
    void Unhighlight();
}
