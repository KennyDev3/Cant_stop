using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    public EnemyData enemyData;
    private float currentHealth;
    private bool isDead = false;
    private EnemyController enemyController;
    private NavMeshAgent navMeshAgent;
    private Collider enemyCollider;
    private Rigidbody rb;

    private float particleEffectDestroyTime = 3f;


    void Start()
    {
        currentHealth = enemyData.maxHealth;
        enemyController = GetComponent<EnemyController>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        enemyCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>(); 

    }

    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (isDead) return;

        currentHealth -= damage;
        PlayHitEffect(hitPoint);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void PlayHitEffect(Vector3 position)
    {
        
        if (enemyData.bloodVFX != null)
        {
            GameObject effect = Instantiate(enemyData.bloodVFX, position, Quaternion.identity);

            Destroy(effect, particleEffectDestroyTime);
          
        }
    }



    private void Die()
{
    isDead = true;
    Debug.Log(gameObject.name + " has died.");

    if (enemyController != null) enemyController.enabled = false;
    if (navMeshAgent != null) navMeshAgent.enabled = false;

    if (rb != null)
    {
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.None;
        rb.useGravity = true;
        
        Vector3 randomTorque = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-0.5f, 0.5f),
            Random.Range(-1f, 1f)
        ) * 3f; // Adjust multiplier for more/less dramatic fall
        
        rb.AddTorque(randomTorque, ForceMode.VelocityChange);
        
        Vector3 randomForce = new Vector3(
            Random.Range(-0.5f, 0.5f),
            0.2f, // Slight upward component
            Random.Range(-0.5f, 0.5f)
        ) * 2f;
        
        rb.AddForce(randomForce, ForceMode.VelocityChange);
    }

    if (enemyData.garbageDataOnDeath != null)
    {
        GarbageItem garbageItem = gameObject.AddComponent<GarbageItem>();
        garbageItem.Initialize(enemyData.garbageDataOnDeath);
        gameObject.layer = LayerMask.NameToLayer("Interactable");
        Debug.Log(gameObject.name + " has become interactable garbage.");
    }
    else
    {
        if(enemyCollider != null) enemyCollider.enabled = false;
    }
}




   
}
