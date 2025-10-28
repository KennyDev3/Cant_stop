using UnityEngine;
using TMPro;

public class GarbageItem : MonoBehaviour, IInteractable
{
    [Header("UI Feedback")]
    [Tooltip("The World Space Canvas Prefab to show item info.")]
    [SerializeField] private GameObject infoUIPrefab;

    [Tooltip("The time (in seconds) the scale animation takes.")]
    [SerializeField] private float animationDuration = 0.15f;

    [Tooltip("The desired final WORLD scale for the UI (e.g., 0.01, 0.01, 0.01)")]
    [SerializeField] private Vector3 desiredWorldScale = new Vector3(0.01f, 0.01f, 0.01f);

    [Tooltip("X/Y offset from the GarbageItem's pivot point (in local space).")]
    [SerializeField] private Vector2 uiOffset = new Vector2(0f, 1f);

    [Tooltip("The magnitude of the random positional offset added to the UI on instantiation.")]
    [SerializeField] private float randomOffsetRange = 1f; // <<< NEW RANDOM RANGE FIELD

    private GameObject _infoUIInstance;
    private TextMeshProUGUI _infoUIText;
    private Vector3 _targetLocalScale = Vector3.zero;

    [SerializeField] private GarbageData garbageData;


    private Outline _outline;



    void Awake()
    {
        _outline = GetComponent<Outline>();
        _outline.OutlineColor = Color.white;
        _outline.enabled = true; // Keep enabled for it to function

        if (infoUIPrefab != null)
        {
            _infoUIInstance = Instantiate(infoUIPrefab, transform.position, Quaternion.identity, transform);

            float randomX = Random.Range(-randomOffsetRange, randomOffsetRange);

            _infoUIInstance.transform.localPosition = new Vector3(
                uiOffset.x + randomX,          
                uiOffset.y,                    
                0f                             
            );

            Vector3 parentScale = transform.localScale;

            _targetLocalScale.x = desiredWorldScale.x / parentScale.x;
            _targetLocalScale.y = desiredWorldScale.y / parentScale.y;
            _targetLocalScale.z = desiredWorldScale.z / parentScale.z;

            _infoUIText = _infoUIInstance.GetComponentInChildren<TextMeshProUGUI>();

            _infoUIInstance.transform.localScale = Vector3.zero;
        }
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
          if (garbageHandler.PickupGarbage(this))
            {
                Destroy(gameObject);
            }
           
        }
    }

    public void Highlight()
    {
        if (_outline != null)
        {
            // Set to yellow and make visible when in range
            _outline.OutlineColor = Color.yellow;
        }

        if (_infoUIInstance != null)
        {
            // 1. Format the text with the garbage data
            _infoUIText.text = $"Weight: {garbageData.capacityCost}\nWorth: ${garbageData.value}";

            // 2. Stop any previous animation and start the scale-up animation
            LeanTween.cancel(_infoUIInstance);
            // Use setEaseOutBack for a nice 'pop' effect
            LeanTween.scale(_infoUIInstance, _targetLocalScale, animationDuration).setEaseOutBack();
        }
    }

    public void Unhighlight()
    {
        if (_outline != null)
        {
            // Reset to white and hide when out of range
            _outline.OutlineColor = Color.white;

        }

        if (_infoUIInstance != null)
        {
            // Stop any previous animation and start the scale-down animation
            LeanTween.cancel(_infoUIInstance);
            // Use setEaseInBack to quickly disappear
            LeanTween.scale(_infoUIInstance, Vector3.zero, animationDuration).setEaseInBack();
        }
    }

    // Public getter for the data
    public GarbageData GetGarbageData()
    {
        return garbageData;
    }
}