using StarterAssets;
using UnityEngine;

public class Projectile : MonoBehaviour, IParriable
{
    private float damage;
    private float moveSpeed; 

    
    private Rigidbody rb;
    private bool _isParried = false;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("Projectile is missing a Rigidbody component!");
            return;
        }

        rb.useGravity = false;
        rb.isKinematic = true;

        SphereCollider sphereCollider = GetComponent<SphereCollider>();
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
        // Logic for Hitting Player
        if (other.CompareTag("Player") && !_isParried)
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            Destroy(gameObject);
        }

        // After Parry
        else if (other.gameObject.layer == LayerMask.NameToLayer("Enemy") && _isParried)
        {
            var enemyHealth = other.GetComponent<EnemyHealth>();
            if (enemyHealth) enemyHealth.TakeDamage(damage);

            Debug.Log("Reflected projectile hit an enemy!");
            Destroy(gameObject);
        }
    }

    public void OnParried(Vector3 parrySourcePosition)
    {
        if (_isParried) return; // Don't parry twice

        _isParried = true;

        // Flip direction 
        transform.forward = -transform.forward;

        // Speed up slightly for 
        moveSpeed *= 2f;

        // Extend lifetime so it doesn't vanish mid-air
        Destroy(gameObject, 8f);

        // Consider: Change Visuals/Layer
        // gameObject.layer = LayerMask.NameToLayer("PlayerProjectile");
        Debug.Log("Projectile Deflected!");
    }
}