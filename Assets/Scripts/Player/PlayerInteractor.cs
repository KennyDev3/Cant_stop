using UnityEngine;
using StarterAssets;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private LayerMask interactableLayer;

    private StarterAssetsInputs _input;
    private Camera _mainCamera;
    private IInteractable _currentInteractable;
    private ShopManager _shopManager;

    void Awake()
    {
        _input = GetComponent<StarterAssetsInputs>();
        _mainCamera = Camera.main;

        _shopManager = FindFirstObjectByType<ShopManager>();
        if (_shopManager == null)
        {
            Debug.LogError("PlayerInteractor cannot find a ShopManager in the scene.");
        }
    }

    void Update()
    {
        CheckForInteractable();

        if (_shopManager != null && _shopManager.IsShopOpen)
        {
            if (_currentInteractable == null)
            {
                Debug.LogWarning("[PI Update] Shop Open. No interactable in range. Closing shop.");
                _shopManager.CloseShop();
            }

            
            if (_input.interact)
            {
                _input.interact = false;
            }

            // We return here to skip the normal interaction logic below.
            return;
        }

        // 3. NORMAL INTERACTION LOGIC (Only runs if the shop is closed)

        if (_input.interact)
        {
            if (_currentInteractable != null)
            {
                _currentInteractable.Interact(this);
            }
            // Consume Input for single press
            _input.interact = false;
        }
    }

    // The rest of the methods remain unchanged.
    private void CheckForInteractable()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactionDistance, interactableLayer);

        IInteractable closestInteractable = null;
        float minDistance = float.MaxValue;

        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent(out IInteractable interactable))
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestInteractable = interactable;
                }
            }
        }

        if (closestInteractable != _currentInteractable)
        {
            if (_currentInteractable != null) _currentInteractable.Unhighlight();

            _currentInteractable = closestInteractable;

            if (_currentInteractable != null)
            {
                _currentInteractable.Highlight();
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}