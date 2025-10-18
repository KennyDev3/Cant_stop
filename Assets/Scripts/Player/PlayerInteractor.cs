using UnityEngine;
using StarterAssets;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private LayerMask interactableLayer;

    private StarterAssetsInputs _input;
    private Camera _mainCamera;
    private IInteractable _currentInteractable;

    void Awake()
    {
        _input = GetComponent<StarterAssetsInputs>();
        _mainCamera = Camera.main;
    }

    void Update()
    {
        CheckForInteractable();

        if (_input.interact)
        {
            if (_currentInteractable != null)
            {
                _currentInteractable.Interact(this);
            }
            // Set interact to false to act as a single-press button
            _input.interact = false;
        }
    }

    private void CheckForInteractable()
{
    Collider[] colliders = Physics.OverlapSphere(transform.position, interactionDistance, interactableLayer);

    // DEBUG: Is the sphere finding anything at all?
    if (colliders.Length > 0)
    {
        // Debug.Log($"Found {colliders.Length} colliders in the interaction zone.");
    }

    IInteractable closestInteractable = null;
    float minDistance = float.MaxValue;

    foreach (var collider in colliders)
    {
        // DEBUG: Print the name and layer of every object found
        // Debug.Log($"Checking object: {collider.name} on layer {LayerMask.LayerToName(collider.gameObject.layer)}");
        
        if (collider.TryGetComponent(out IInteractable interactable))
        {
            // DEBUG: Did we find a script that implements IInteractable?
            // Debug.Log($"{collider.name} has an IInteractable script!");

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
                // DEBUG: We are about to highlight something!
                // Debug.Log($"Highlighting new target: {(_currentInteractable as MonoBehaviour).name}");
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
