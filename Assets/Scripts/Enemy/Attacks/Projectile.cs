using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float damage;
    private float moveSpeed; 

    
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("Projectile is missing a Rigidbody component!");
            return;
        }

        rb.useGravity = false;
        // The key setting for manual movement:
        rb.isKinematic = true;

        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        // Ensure Collider exists
        if (sphereCollider != null)
        {
            sphereCollider.isTrigger = true;
        }
    }

    public void Initialize(float damageAmount, float speed)
    {
        this.damage = damageAmount;
        this.moveSpeed = speed;

        Destroy(gameObject, 10f);
    }

   
    void Update()
    {
        
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            Destroy(gameObject);
        }

    }
}