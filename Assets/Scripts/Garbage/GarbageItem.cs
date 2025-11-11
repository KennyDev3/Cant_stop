using UnityEngine;
using TMPro;
using StarterAssets; 



public class GarbageItem : MonoBehaviour, IInteractable
{
    [Header("Data")]
    [SerializeField] private GarbageData garbageData; // Assign your ScriptableObject here

    [Header("Animation")]
    [Tooltip("The duration of the PLAYER'S pickup animation")]
    public float playerAnimationDuration = 1.0f;

    [Header("UI Feedback")]
    [SerializeField] private GameObject infoUIPrefab;
    [SerializeField] private float uiAnimationDuration = 0.15f;
    [SerializeField] private Vector3 desiredWorldScale = new Vector3(0.01f, 0.01f, 0.01f);
    [SerializeField] private Vector2 uiOffset = new Vector2(0f, 1f);
    [SerializeField] private float randomOffsetRange = 1f;

    [Header("Cleanup")]
    [Tooltip("If set, this object will be destroyed instead of the object this script is on. (Used for Enemy Corpses)")]
    public GameObject destroyTarget; // 

    private GameObject _infoUIInstance;
    private TextMeshProUGUI _infoUIText;
    private Vector3 _targetLocalScale = Vector3.zero;
    private Outline _outline; 

    void Awake()
    {
        _outline = GetComponent<Outline>();
        if (_outline == null)
        {
            Debug.LogWarning("Outline component missing from GarbageItem.", this);
        }
        else
        {
            _outline.OutlineColor = Color.white;
            _outline.enabled = true;
        }

        if (infoUIPrefab != null)
        {
            _infoUIInstance = Instantiate(infoUIPrefab, transform.position, Quaternion.identity, transform);
            float randomX = Random.Range(-randomOffsetRange, randomOffsetRange);
            _infoUIInstance.transform.localPosition = new Vector3(uiOffset.x + randomX, uiOffset.y, 0f);

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
        if (garbageHandler == null)
        {
            Debug.LogError("Player is missing PlayerGarbageHandler component!");
            return;
        }

        // 1. Tell the coordinator (Handler) to start the pickup process.
        if (garbageHandler.StartPickupProcess(this))
        {
            // 2. If the handler confirmed the process started (e.g., player is strong enough),
            //    disable this item's visuals and collider so it can't be interacted with again.
            if (_outline != null) _outline.enabled = false;
            GetComponent<Collider>().enabled = false;

            // Hide the floating UI
            if (_infoUIInstance != null)
            {
                // Assumes LeanTween is in the project. If not, just disable the object.
                LeanTween.cancel(_infoUIInstance);
                LeanTween.scale(_infoUIInstance, Vector3.zero, uiAnimationDuration).setEaseInBack();
                _infoUIInstance.SetActive(false);
            }
        }
    }

    public void Highlight()
    {
        if (_outline != null)
        {
            _outline.OutlineColor = Color.yellow;
        }

        if (_infoUIInstance != null)
        {
            _infoUIText.text = $"Weight: {garbageData.capacityCost}\nWorth: ${garbageData.value}";
            _infoUIInstance.SetActive(true);
            LeanTween.cancel(_infoUIInstance);
            LeanTween.scale(_infoUIInstance, _targetLocalScale, uiAnimationDuration).setEaseOutBack();
        }
    }

    public void Unhighlight()
    {
        if (_outline != null)
        {
            _outline.OutlineColor = Color.white;
        }

        if (_infoUIInstance != null)
        {
            _infoUIInstance.SetActive(false);
            LeanTween.cancel(_infoUIInstance);
            LeanTween.scale(_infoUIInstance, Vector3.zero, uiAnimationDuration).setEaseInBack();
        }
    }


    public void NotifyCollected()
    {
        if (destroyTarget != null)
        {
            // If a special target is set (like the Enemy root), destroy that.
            Destroy(destroyTarget);
        }
        else
        {
            // Otherwise, do the default behavior (for all your other items).
            Destroy(gameObject);
        }
    }

    public GarbageData GetGarbageData()
    {
        return garbageData;
    }
}