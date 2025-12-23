using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    [Header("2.5D Positioning")]
    [Tooltip("How much to move the UI 'Up' relative to the screen/camera view")]
    [SerializeField] private float screenUpOffset = 1.5f;
    [Tooltip("Initial vertical jump to clear the floor/mesh")]
    [SerializeField] private float worldYPadding = 0.5f;

    [Header("Visuals")]
    [SerializeField] private Vector3 fixedRotation = new Vector3(68f, 0f, 0f);
    [SerializeField] private Vector3 targetWorldScale = new Vector3(0.01f, 0.01f, 0.01f);

    private Renderer parentRenderer;
    private Camera mainCam;

    void Start()
    {
        parentRenderer = transform.parent.GetComponentInChildren<Renderer>();
        mainCam = Camera.main;
    }

    void LateUpdate()
    {
        if (parentRenderer == null || mainCam == null) return;

        float topOfMesh = parentRenderer.bounds.max.y;
        Vector3 basePosition = new Vector3(parentRenderer.bounds.center.x, topOfMesh + worldYPadding, parentRenderer.bounds.center.z);

        
        Vector3 finalPosition = basePosition + (mainCam.transform.up * screenUpOffset);
        transform.position = finalPosition;

        transform.rotation = Quaternion.Euler(fixedRotation);

        if (transform.parent != null)
        {
            Vector3 pScale = transform.parent.lossyScale;
            transform.localScale = new Vector3(
                targetWorldScale.x / pScale.x,
                targetWorldScale.y / pScale.y,
                targetWorldScale.z / pScale.z
            );
        }
    }
}