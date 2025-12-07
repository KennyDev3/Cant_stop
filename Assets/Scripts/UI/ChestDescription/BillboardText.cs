using UnityEngine;
using TMPro;

public class BillboardText : MonoBehaviour
{
    private Transform mainCameraTransform;

    [Tooltip("The fixed tilt on the X-axis for the text, in degrees (e.g., 68 for looking up).")]
    [SerializeField] private float fixedXTilt = 68f;

    void Start()
    {
        // Get the Main Camera transform once
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("No Main Camera found! Ensure your camera is tagged 'MainCamera'.");
            enabled = false;
        }
    }

    void LateUpdate()
    {
        if (mainCameraTransform == null) return;

        float cameraYRotation = mainCameraTransform.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(fixedXTilt, cameraYRotation, 0f);

        transform.rotation = targetRotation;
    }
    
    public void SetText(string content)
    {
        TextMeshPro textComponent = GetComponentInChildren<TextMeshPro>();
        if (textComponent != null)
        {
            textComponent.text = content;
        }
    }
}