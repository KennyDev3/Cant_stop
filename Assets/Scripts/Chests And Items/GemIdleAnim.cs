using UnityEngine;

public class GemIdleAnim : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 90f;

    [Header("Floating")]
    [SerializeField] private float floatAmplitude = 0.1f;
    [SerializeField] private float floatFrequency = 1f;

    private Vector3 _startLocalPos;

    private void OnEnable()
    {
        
        _startLocalPos = transform.localPosition;
    }

    private void Update()
    {
        // Rotate around Y
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.Self);

        // Gentle up/down float
        float yOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.localPosition = _startLocalPos + Vector3.up * yOffset;
    }
}
