using StarterAssets;
using System;
using System.Collections;
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

    [Header("Pickup Animation")]
    [SerializeField] private float animationDuration = 0.5f; 
    [SerializeField] private float maxScaleMultiplier = 1.15f;


    private bool _isBeingCollected = false;

    [Tooltip("Multiplier for the Y-axis offset, based on the object's height (bounds.size.y).")]
    [SerializeField] private float uiYMultiplier = 0.6f;

    [Tooltip("Multiplier for the Z-axis offset, based on the object's depth (bounds.size.z).")]
    [SerializeField] private float uiZMultiplier = 0.5f;

    [Tooltip("Used if NO Renderer is found.")]
    [SerializeField] private Vector3 fallbackOffset = new Vector3(0, 1.5f, 0);



    private int _originalLayer;
    private int _interactableLayerIndex;
    private GameObject _infoUIInstance;
    private TextMeshProUGUI _infoUIText;
    private Renderer _renderer;

    private void Awake()
    {
        _interactableLayerIndex = LayerMask.NameToLayer(interactableLayerName);
        _originalLayer = gameObject.layer;

        _renderer = GetComponentInChildren<Renderer>();

        if (infoUIPrefab != null)
        {
            Vector3 spawnPosition;

            if (_renderer != null)
            {
                Bounds bounds = _renderer.bounds;
                float yOffset = bounds.center.y + (bounds.extents.y * uiYMultiplier);
                float zOffset = bounds.extents.z * uiZMultiplier;
                spawnPosition = transform.position + new Vector3(0f, yOffset, zOffset);
            }
            else
            {
                spawnPosition = transform.position + fallbackOffset;
            }

            _infoUIInstance = Instantiate(infoUIPrefab, spawnPosition, Quaternion.identity, transform);
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
                targetOutline.enabled = true;
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
        // If it's an enemy/pooled object, bypass the animation as requested
        if (isPooledObject)
        {
            OnCollected?.Invoke(this);
        }
        else
        {
            // Prevent double-triggering if NotifyCollected is called multiple times
            if (_isBeingCollected) return;

            StartCoroutine(PickupAnimationRoutine());
        }
    }

    private IEnumerator PickupAnimationRoutine()
    {
        _isBeingCollected = true;

        if (interactionCollider != null) interactionCollider.enabled = false;
        if (targetOutline != null) targetOutline.enabled = false;
        if (_infoUIInstance != null) _infoUIInstance.SetActive(false);

        Vector3 originalScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / animationDuration;
            float curve;

            if (percent < 0.2f)
            {
                curve = Mathf.Lerp(1f, maxScaleMultiplier, percent / 0.2f);
            }
            else
            {
                curve = Mathf.Lerp(maxScaleMultiplier, 0f, (percent - 0.2f) / 0.8f);
            }

            transform.localScale = originalScale * curve;
            yield return null;
        }

        if (destroyTarget != null) Destroy(destroyTarget);
        else Destroy(gameObject);
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