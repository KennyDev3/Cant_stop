using StarterAssets;
using UnityEngine;

public class Projectile : MonoBehaviour, IParriable
{
    private float damage;
    private float moveSpeed;

    [Header("Audio")]

    [SerializeField] private SoundDef enemyHitFromParrySound;


    private Rigidbody rb;
    private bool _isParried = false;

    [Header("Hub Upgrade Data")]
    [Tooltip("Hub upgrade definition for Parry Return Damage. primaryAmount = reflected damage multiplier.")]
    [SerializeField] private HubUpgradeSO _parryReturnDamageUpgrade;


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

            SoundManager.Instance.Play(enemyHitFromParrySound, transform.position);
            Debug.Log("Reflected projectile hit an enemy!");
            Destroy(gameObject);
        }
    }

    public void OnParried(Vector3 parrySourcePosition)
    {
        if (_isParried) return; // Don't parry twice

        _isParried = true;

        // Hub upgrade: damage multiplier on returned projectile
        if (GameManager.Instance != null && GameManager.Instance.IsHubUpgradeUnlocked(HubUpgradeKeys.ParryReturnDamage))
        {
            float multiplier = 2f;
            if (_parryReturnDamageUpgrade != null && _parryReturnDamageUpgrade.primaryAmount > 0f)
                multiplier = _parryReturnDamageUpgrade.primaryAmount;
            damage *= multiplier;
        }

        // Flip direction 
        transform.forward = -transform.forward;

        // Speed up slightly
        moveSpeed *= 2f;

        // Extend lifetime so it doesn't vanish mid-air
        Destroy(gameObject, 8f);

        // Consider: Change Visuals/Layer
        // gameObject.layer = LayerMask.NameToLayer("PlayerProjectile");
        Debug.Log("Projectile Deflected!");
    }
}