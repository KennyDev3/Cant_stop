using UnityEngine;

public class GarbageDropOff : MonoBehaviour, IInteractable
{
    // Simple highlight effect

    private Outline _outline;

    void Awake()
    {
        _outline = GetComponent<Outline>();
        _outline.OutlineColor = Color.white;
        _outline.enabled = true; // Keep enabled for it to function
    }
    
    public string GetInteractionPrompt()
    {
        return "Press E to drop off garbage";
    }

    public void Interact(PlayerInteractor interactor)
    {
        var garbageHandler = interactor.GetComponent<PlayerGarbageHandler>();
        if (garbageHandler != null)
        {
            int cashEarned = garbageHandler.DropOffGarbage();
            // In the future, you would add cashEarned to a Game Manager script
        }
    }

    public void Highlight()
    {
        if (_outline != null)
        {
            // Set to yellow and make visible when in range
            _outline.OutlineColor = Color.yellow;
        }
    }

    public void Unhighlight()
    {
        if (_outline != null)
        {
            // Reset to white and hide when out of range
            _outline.OutlineColor = Color.white;
            
        }
    }
}
