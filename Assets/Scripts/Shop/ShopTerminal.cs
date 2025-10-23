using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class ShopTerminal : MonoBehaviour, IInteractable
{

    [Header("UI Reference")]
    [Tooltip("Reference to the ShopManager in the scene.")]
    [SerializeField] private ShopManager shopManager;

    private string _prompt = "Press E to open Shop";
    private Outline _outline;


    void Awake()
    {
        _outline = GetComponent<Outline>();
        _outline.OutlineColor = Color.white;
        _outline.enabled = true; // Keep enabled for it to function
    }

    void Start()
    {
        // Find the shop manager if not assigned (assumes one exists)
        if (shopManager == null)
        {
            shopManager = FindFirstObjectByType<ShopManager>();
            if (shopManager == null)
            {
                Debug.LogError("ShopTerminal needs a ShopManager reference or one must exist in the scene.");
                enabled = false;
                return;
            }
        }
    }

    public string GetInteractionPrompt()
    {
        return _prompt;
    }

    public void Interact(PlayerInteractor interactor)
    {
        // The shop manager handles the complex open/close logic
        shopManager.OpenShop(transform.position);
    }

    public void Highlight()
    {
        if (_outline != null)
        {
            // Set to yellow and make visible when in range
            _outline.OutlineColor = Color.green;
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
