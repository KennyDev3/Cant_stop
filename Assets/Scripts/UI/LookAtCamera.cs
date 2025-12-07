using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    [Tooltip("The fixed rotation in Euler angles (degrees) to set for the object.")]
    [SerializeField] private Vector3 fixedRotation = new Vector3(68f, 0f, 0f);

    private Vector3 initialScale;

    void Start()
    {
        
        initialScale = transform.localScale;

        transform.localRotation = Quaternion.Euler(fixedRotation);
    }

    void LateUpdate()
    {
       
        Vector3 parentWorldScale = transform.parent.lossyScale;

        float inverseX = 1f / parentWorldScale.x;
        float inverseY = 1f / parentWorldScale.y;
        float inverseZ = 1f / parentWorldScale.z;

       
        transform.localScale = new Vector3(
            initialScale.x * inverseX,
            initialScale.y * inverseY,
            initialScale.z * inverseZ
        );
    }
}