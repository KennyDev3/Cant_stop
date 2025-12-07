using StarterAssets;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class GarbageItem : MonoBehaviour, IInteractable
{
    public event Action<GarbageItem> OnCollected;

    [Header("Data")]
    [SerializeField] private GarbageData garbageData;

    [Header("Pooling")]
    [Tooltip("TRUE for Enemies (Hidden until death). FALSE for Static Trash (Always visible).")]
    public bool isPooledObject = false;

    [SerializeField] private string interactableLayerName = "Interactable";

    [Header("References")]
    [SerializeField] private Outline targetOutline;
    [SerializeField] private Collider interactionCollider;
    [SerializeField] private GameObject destroyTarget;
    [SerializeField] private GameObject infoUIPrefab;

    [Tooltip("Multiplier for the Y-axis offset, based on the object's height (bounds.size.y).")]
    [SerializeField] private float uiYMultiplier = 0.6f;

    [Tooltip("Multiplier for the Z-axis offset, based on the object's depth (bounds.size.z).")]
    [SerializeField] private float uiZMultiplier = 0.5f;

    private int _originalLayer;
    private int _interactableLayerIndex;
    private GameObject _infoUIInstance;
    private TextMeshProUGUI _infoUIText;
    private Renderer _renderer;

    private void Awake()
    {
        _interactableLayerIndex = LayerMask.NameToLayer(interactableLayerName);
        _originalLayer = gameObject.layer;


        // UI Setup
        _renderer = GetComponentInChildren<Renderer>();
        // ----------------------------------

        if (infoUIPrefab != null && _renderer != null)
        {
           
            Bounds bounds = _renderer.bounds;

            
            float yOffset = bounds.center.y + (bounds.extents.y * uiYMultiplier);

            float zOffset = bounds.extents.z * uiZMultiplier;
            Vector3 spawnPosition = transform.position + new Vector3(0f, yOffset, zOffset);
            _infoUIInstance = Instantiate(infoUIPrefab, spawnPosition, Quaternion.identity, transform);


            _infoUIText = _infoUIInstance.GetComponentInChildren<TextMeshProUGUI>();
            _infoUIInstance.SetActive(false);
        }
        else
        {
            if (_renderer == null) Debug.LogError("GarbageItem needs a Renderer component (or child Renderer) to calculate bounds.");
        }

        if (isPooledObject)
        {
            
            if (interactionCollider != null) interactionCollider.enabled = false;
            if (targetOutline != null) targetOutline.enabled = false; 
            this.enabled = false;
        }
        else
        {
           
            if (interactionCollider != null) interactionCollider.enabled = true;
            if (targetOutline != null)
            {
                targetOutline.enabled = true; // Always on
                targetOutline.OutlineColor = Color.white;
            }
            this.enabled = true;
        }
    }

    // Called by EnemyHealth when dead
    public void ActivatePooledInteractable(GarbageData newData)
    {
        garbageData = newData;
        _originalLayer = gameObject.layer;

        // Change layer so Raycast can hit the Hips
        gameObject.layer = _interactableLayerIndex;

        if (interactionCollider != null) interactionCollider.enabled = true;

       

        this.enabled = true;
    }

    public void ResetPooledInteractable()
    {
        gameObject.layer = _originalLayer;

        if (interactionCollider != null) interactionCollider.enabled = false;

        if (targetOutline != null) targetOutline.enabled = false;
        if (_infoUIInstance != null) _infoUIInstance.SetActive(false);

        this.enabled = false;
    }

    public void Interact(PlayerInteractor interactor)
    {
        var garbageHandler = interactor.GetComponent<PlayerGarbageHandler>();

        if (garbageHandler != null && garbageHandler.StartPickupProcess(this))
        {
            if (interactionCollider != null) interactionCollider.enabled = false;
            if (targetOutline != null) targetOutline.enabled = false;
            if (_infoUIInstance != null) _infoUIInstance.SetActive(false);
        }
    }

    public void NotifyCollected()
    {
        if (isPooledObject)
        {
            OnCollected?.Invoke(this);
        }
        else
        {
            if (destroyTarget != null) Destroy(destroyTarget);
            else Destroy(gameObject);
        }
    }

    public void Highlight()
    {
        if (targetOutline != null)
        {
            targetOutline.enabled = true;
            targetOutline.OutlineColor = Color.yellow;
        }

        if (_infoUIInstance != null) _infoUIInstance.SetActive(true);
        if (garbageData != null && _infoUIText != null)
        {
            _infoUIText.text =
                $"Value: ${garbageData.value}\n" +
                $"Weight: {garbageData.capacityCost}\n" +
                $"Tier: {garbageData.garbageTier}";
        }
    }

    public void Unhighlight()
    {
        if (targetOutline != null)
        {
            if (isPooledObject)
            {
                targetOutline.enabled = false;
            }
            else
            {
                targetOutline.OutlineColor = Color.white;
                targetOutline.enabled = true;
            }
        }

        if (_infoUIInstance != null) _infoUIInstance.SetActive(false);
    }

    public string GetInteractionPrompt() => $"Pick up {garbageData?.itemName}";
    public GarbageData GetGarbageData() => garbageData;
}