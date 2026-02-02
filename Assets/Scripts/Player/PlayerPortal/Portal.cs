using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Portal : MonoBehaviour
{
    [Header("Destination")]
    [Tooltip("Assign in prefab, or set at runtime via SetDestination() (e.g. when LevelObjectiveManager spawns portals).")]
    [SerializeField] private PortalDestinationSO destination;

    [Header("Portal placement")]
    [Tooltip("World Y position when the portal spawns. Text animation uses the label's current transform as baseline.")]
    [SerializeField] private float spawnPositionY = 3.45f;

    [Header("Label (TextMeshPro 3D)")]
    [SerializeField] private TextMeshPro portalLabel;
    [Tooltip("Label's local position relative to portal. Set once to match the text child in the prefab (e.g. Y = -0.271).")]
    [SerializeField] private Vector3 labelLocalPosition = new Vector3(0f, -0.271f, 0f);
    [Tooltip("Label's local scale in the prefab. Ensures relationship is consistent regardless of spawn order.")]
    [SerializeField] private Vector3 labelLocalScale = Vector3.one;
    [Tooltip("Label's local rotation (Euler) in the prefab.")]
    [SerializeField] private Vector3 labelLocalRotationEuler = Vector3.zero;

    [Header("Audio")]
    [SerializeField] SoundDef playerPortalOpeningSound;
    [SerializeField] SoundDef playerEnteringPortalSound;

    private bool _triggered = false;

    /// <summary>Call from the Portal prefab with the label child set correctly; copies its local transform into the serialized fields so the relationship stays consistent at runtime.</summary>
    [ContextMenu("Copy label transform from hierarchy")]
    private void CopyLabelTransformFromHierarchy()
    {
        if (portalLabel == null) return;
        Transform tr = portalLabel.transform;
        labelLocalPosition = tr.localPosition;
        labelLocalScale = tr.localScale;
        labelLocalRotationEuler = tr.localEulerAngles;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private void Start()
    {
        // Spawn at desired height so all portals share same Y (and Z is set by spawner)
        Vector3 p = transform.position;
        transform.position = new Vector3(p.x, spawnPositionY, p.z);

        ApplyDestinationLabel();

        SoundManager.Instance.Play(playerPortalOpeningSound,transform.position); 

    }

    /// <summary>Assign destination at runtime (e.g. when LevelObjectiveManager spawns multiple portals from one prefab).</summary>
    public void SetDestination(PortalDestinationSO dest)
    {
        destination = dest;
        ApplyDestinationLabel();
    }

    private void ApplyDestinationLabel()
    {
        if (destination == null || portalLabel == null) return;
        if (string.IsNullOrEmpty(destination.LabelText)) return;
        portalLabel.text = destination.LabelText;
    }

    private void Update()
    {
        if (portalLabel == null) return;

        Transform t = portalLabel.transform;
        float time = Time.time;

        // Always apply prefab relationship (serialized) so it's consistent regardless of spawn/order
        t.localPosition = labelLocalPosition;

        // Scale: prefab relationship + pulse animation
        float scaleMul = 1f + 0.06f * Mathf.Sin(time * 2.2f);
        t.localScale = labelLocalScale * scaleMul;

        // Rotation: prefab relationship + wobble (portal spawns at 0,0,0 so no flip needed)
        Quaternion prefabLocalRot = Quaternion.Euler(labelLocalRotationEuler);
        float wobbleDeg = 1.5f * Mathf.Sin(time * 1.2f);
        t.localRotation = prefabLocalRot * Quaternion.Euler(0f, wobbleDeg, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;

        if (other.CompareTag("Player"))
        {
            _triggered = true;
            ActivatePortal(other.gameObject);

            SoundManager.Instance.Play(playerEnteringPortalSound, transform.position);
        }
    }

    // Call this if you need to trigger the portal via code/event instead of collision
    public void HandleExternalTrigger(Collider other)
    {
        OnTriggerEnter(other);
    }

    private void ActivatePortal(GameObject playerObj)
    {
        if (destination == null)
        {
            Debug.LogWarning("[Portal] No destination assigned. Cannot activate.");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[Portal] No GameManager found. Reloading current scene as fallback.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        Debug.Log("<color=cyan>[Portal]</color> Activating: " + destination.name);
        GameManager.Instance.RequestScene(destination.GetRequest());
    }
}