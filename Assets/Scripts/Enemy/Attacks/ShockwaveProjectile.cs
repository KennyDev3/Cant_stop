using UnityEngine;

public class ShockwaveProjectile : MonoBehaviour
{
    [Header("Visuals")]
    // 
    public GameObject hitEffect; // Optional: Spawn particle on hit

    private Vector3 moveDirection;

    private float speed;
    private float damage;
    public float lifeTime = 5f;

    public void Initialize(float attackDamage, float projectileSpeed)
    {
        this.damage = attackDamage;
        this.speed = projectileSpeed;

        // Auto-destroy after lifetime to prevent memory leaks
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {

        // Using Space.World ensures we move along the global axes defined at launch
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);

                // Optional: Spawn hit VFX
                if (hitEffect != null) Instantiate(hitEffect, transform.position, Quaternion.identity);

                // Destroy the wave on impact? 
                Destroy(gameObject);
            }
        }

    }


}
