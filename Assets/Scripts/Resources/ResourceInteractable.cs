using UnityEngine;

/// <summary>
/// Resource pickup in the world. Implements IInteractable; player presses interact to collect.
/// Requires Outline on this GameObject or a child. Place on same layer as other interactables (PlayerInteractor's interactableLayer).
/// </summary>
public class ResourceInteractable : MonoBehaviour, IInteractable
{
    [Header("Resource")]
    [SerializeField] private ResourceSO resourceType;

    [Tooltip("If &gt; 0, use this instead of ResourceSO.amountPerPickup.")]
    [SerializeField] private int amountOverride;

    [Header("Audio")]
    [SerializeField] private SoundDef pickupSound;

    private Outline _outline;

    private void Awake()
    {
        _outline = GetComponentInChildren<Outline>(true);
        if (_outline == null)
            _outline = GetComponent<Outline>();
        if (_outline != null)
        {
            _outline.OutlineColor = Color.green;
            _outline.enabled = true;
        }
        else
            Debug.LogWarning("[ResourceInteractable] No Outline found on self or children. Highlight will do nothing.", this);
    }

    public string GetInteractionPrompt()
    {
        if (resourceType != null && !string.IsNullOrEmpty(resourceType.displayName))
            return $"Press E to collect {resourceType.displayName}";
        return "Press E to collect";
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (resourceType == null)
        {
            Debug.LogWarning("[ResourceInteractable] No ResourceSO assigned.", this);
            return;
        }

        var holder = interactor.GetComponent<PlayerResourceHolder>();
        if (holder == null)
        {
            Debug.LogWarning("[ResourceInteractable] Player has no PlayerResourceHolder.", this);
            return;
        }

        int amount = amountOverride > 0 ? amountOverride : resourceType.amountPerPickup;
        holder.Add(resourceType, amount);

        if (pickupSound != null && SoundManager.Instance != null)
            SoundManager.Instance.Play(pickupSound, transform.position);

        Destroy(gameObject);
    }

    public void Highlight()
    {
        if (_outline != null)
            _outline.OutlineColor = Color.yellow;
    }

    public void Unhighlight()
    {
        if (_outline != null)
            _outline.OutlineColor = Color.green;
    }
}
