using UnityEngine;

public class GarbageItem : MonoBehaviour, IInteractable
{

    [SerializeField] private GarbageData garbageData;


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


    public void Initialize(GarbageData data)
    {
        garbageData = data;
        InitializeRenderer();
    }

    private void InitializeRenderer()
    {
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
        {
            _originalColor = _renderer.material.color;
        }
    }

    public string GetInteractionPrompt()
    {
        return $"Press E to pick up {garbageData.itemName}";
    }

    public void Interact(PlayerInteractor interactor)
    {
        // Try to give this garbage to the player's garbage handler
        var garbageHandler = interactor.GetComponent<PlayerGarbageHandler>();
        if (garbageHandler != null)
        {
            if (garbageHandler.TryPickupGarbage(this))
            {
                // If pickup was successful, destroy this object
                Destroy(gameObject);
            }
        }
    }

    public void Highlight()
    {
        if (_renderer != null)
        {
            // A simple highlight: make it bright yellow
            _renderer.material.color = Color.yellow;
        }
    }

    public void Unhighlight()
    {
        if (_renderer != null)
        {
            _renderer.material.color = _originalColor;
        }
    }
    
    // Public getter for the data
    public GarbageData GetGarbageData()
    {
        return garbageData;
    }

    
}
