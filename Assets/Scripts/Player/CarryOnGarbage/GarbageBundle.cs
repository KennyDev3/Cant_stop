using UnityEngine;
using System.Collections.Generic;
using StarterAssets;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]

public class GarbageBundle : MonoBehaviour, IInteractable
{
    private List<GarbageData> _contents = new List<GarbageData>();
    private Rigidbody _rb;
    private bool _isConsumed = false;

    [SerializeField] private Outline targetOutline;

    [Header("Physics Settings")]
    [Tooltip("How much random spin is added on throw.")]
    [SerializeField] private float rotationSpeed = 2f;

    [Tooltip("Linear Damping (Air resistance/Sliding friction). Higher = Stops sliding faster.")]
    [SerializeField] private float groundDamping = 1f;

    [Tooltip("Angular Damping (Air resistance for rotation). Higher = Stops spinning faster.")]
    [SerializeField] private float rotationDamping = 2f;

    [Header("Scaling Settings")]
    [SerializeField] private float minScaleXYZ = 1.3f; // Prefab default
    [SerializeField] private float maxScaleXYZ = 2.5f; // Max growth


    private Vector3 _originalScale;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        _rb.linearDamping = groundDamping;
        _rb.angularDamping = rotationDamping;

    }

    public void InitializeBundle(List<GarbageData> data, Vector3 direction, float force, float fullnessRatio)
    {
        _contents = new List<GarbageData>(data);

       
        fullnessRatio = Mathf.Clamp01(fullnessRatio);

        float targetScale = Mathf.Lerp(minScaleXYZ, maxScaleXYZ, fullnessRatio);
        transform.localScale = Vector3.one * targetScale;

        _originalScale = transform.localScale;

        // --- PHYSICS ---
        _rb.isKinematic = false;
        _rb.AddForce(direction * force, ForceMode.VelocityChange);
        _rb.AddTorque(Random.insideUnitSphere * rotationSpeed, ForceMode.Impulse);
    }


    public List<GarbageData> GetContents()
    {
        return _contents;
    }

    public int GetTotalValue()
    {
        int total = 0;
        foreach (var item in _contents) total += item.value;
        return total;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (_isConsumed) return;

        var handler = interactor.GetComponent<PlayerGarbageHandler>();
        if (handler != null)
        {
            // Try to pick up the whole bundle
            if (handler.TryCollectBundle(this))
            {
                Destroy(gameObject);
            }
        }
    }

    public string GetInteractionPrompt()
    {
        return $"Pick up Trash Bundle (${GetTotalValue()})";
    }


    public void ShrinkToPercentage(float percentage)
    {

        transform.localScale = _originalScale * percentage;
    }



    // Outline logic for IInteractable
    public void Highlight()
    {
        if (targetOutline)
        {
            targetOutline.OutlineColor = Color.yellow;
        }
    }

    public void Unhighlight()
    {
        targetOutline.OutlineColor = Color.white;
    }























}