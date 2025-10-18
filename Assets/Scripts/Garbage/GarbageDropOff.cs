using UnityEngine;

public class GarbageDropOff : MonoBehaviour, IInteractable
{
    // Simple highlight effect
    private Renderer _renderer;
    private Color _originalColor;

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
        {
            _originalColor = _renderer.material.color;
        }
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
        if (_renderer != null) _renderer.material.color = Color.green;
    }

    public void Unhighlight()
    {
        if (_renderer != null) _renderer.material.color = _originalColor;
    }
}
