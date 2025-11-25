using UnityEngine;
using TMPro;
using System;
using StarterAssets;

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

    private int _originalLayer;
    private int _interactableLayerIndex;
    private GameObject _infoUIInstance;
    private TextMeshProUGUI _infoUIText;

    private void Awake()
    {
        _interactableLayerIndex = LayerMask.NameToLayer(interactableLayerName);
        _originalLayer = gameObject.layer;

        // UI Setup
        if (infoUIPrefab != null)
        {
            _infoUIInstance = Instantiate(infoUIPrefab, transform.position, Quaternion.identity, transform);
            _infoUIText = _infoUIInstance.GetComponentInChildren<TextMeshProUGUI>();
            _infoUIInstance.SetActive(false);
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
        if (garbageData != null && _infoUIText != null) _infoUIText.text = $"{garbageData.itemName}";
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