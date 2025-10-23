using UnityEngine;

public class GarbageItem : MonoBehaviour, IInteractable
{

    [SerializeField] private GarbageData garbageData;


    private Outline _outline;



    void Awake()
    {
        _outline = GetComponent<Outline>();
        _outline.OutlineColor = Color.white;
        _outline.enabled = true; // Keep enabled for it to function
    }


    public void Initialize(GarbageData data)
    {
        garbageData = data;
       
    }
   

    public string GetInteractionPrompt()
    {
        return $"Press E to pick up {garbageData.itemName}";
    }

    public void Interact(PlayerInteractor interactor)
    {
        var garbageHandler = interactor.GetComponent<PlayerGarbageHandler>();
        if (garbageHandler != null)
        {
            // Call the PickupGarbage method. Since it always succeeds, we don't need to check for a return value.
            garbageHandler.PickupGarbage(this);
            
            // After picking it up, destroy the game object.
            Destroy(gameObject);
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

    // Public getter for the data
    public GarbageData GetGarbageData()
    {
        return garbageData;
    }
}