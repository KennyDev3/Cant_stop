using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Portal : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Leave EMPTY to let GameManager decide the next level automatically. Only fill this if you want to force a specific level (like a secret level).")]
    [SerializeField] private string nextSceneNameOverride;

    [SerializeField] private bool returnToHub = false;
    [SerializeField] private bool increaseDifficulty = true;

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
        // Spawn at desired height (prefab scale 2.5,2.5,2.5 is unchanged)
        Vector3 p = transform.position;
        transform.position = new Vector3(p.x, spawnPositionY, p.z);
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
        }
    }

    // Call this if you need to trigger the portal via code/event instead of collision
    public void HandleExternalTrigger(Collider other)
    {
        OnTriggerEnter(other);
    }

    private void ActivatePortal(GameObject playerObj)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[Portal] No GameManager found. Reloading current scene as fallback.");
            SceneManager.LoadScene(string.IsNullOrEmpty(nextSceneNameOverride) ? SceneManager.GetActiveScene().name : nextSceneNameOverride);
            return;
        }

        Debug.Log("<color=cyan>[Portal]</color> Activating... Collecting run state.");

        GameManager.Instance.CollectRunState();

        if (returnToHub)
        {
            GameManager.Instance.LoadSpecificLevel(GameManager.Instance.HubSceneName);
        }
        else if (!string.IsNullOrEmpty(nextSceneNameOverride))
        {
            GameManager.Instance.LoadSpecificLevel(nextSceneNameOverride);
        }
        else
        {
            GameManager.Instance.LoadNextLevelInSequence();
        }
    }
}