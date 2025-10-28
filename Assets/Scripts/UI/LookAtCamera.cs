using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {
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
        if (mainCameraTransform == null)
        {
            return;
        }

        Vector3 lookDirection = transform.position - mainCameraTransform.position;
        transform.rotation = Quaternion.LookRotation(lookDirection);
    }
}