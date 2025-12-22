using UnityEngine;

public class TimedDestruction : MonoBehaviour
{
    [SerializeField] private float lifeTime = 1.0f;

    void Start()
    {
        // Destroy this object automatically after X seconds
        Destroy(gameObject, lifeTime);
    }
}