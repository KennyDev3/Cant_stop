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


    void Start()
    {
        currentHealth = enemyData.maxHealth;
        enemyController = GetComponent<EnemyController>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        enemyCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>(); 

    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
{
    isDead = true;
    Debug.Log(gameObject.name + " has died.");

    // Disable the "brain" and movement
    if (enemyController != null) enemyController.enabled = false;
    if (navMeshAgent != null) navMeshAgent.enabled = false;

    if (rb != null)
    {
        // Make the rigidbody fully dynamic
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.None;
        rb.useGravity = true;
        
        // Apply a random torque to make it topple over
        Vector3 randomTorque = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-0.5f, 0.5f),
            Random.Range(-1f, 1f)
        ) * 3f; // Adjust multiplier for more/less dramatic fall
        
        rb.AddTorque(randomTorque, ForceMode.VelocityChange);
        
        // Optional: Apply a small random force to push it slightly
        Vector3 randomForce = new Vector3(
            Random.Range(-0.5f, 0.5f),
            0.2f, // Slight upward component
            Random.Range(-0.5f, 0.5f)
        ) * 2f;
        
        rb.AddForce(randomForce, ForceMode.VelocityChange);
    }

    if (enemyData.garbageDataOnDeath != null)
    {
        // Add the GarbageItem component to this GameObject.
        GarbageItem garbageItem = gameObject.AddComponent<GarbageItem>();

        // Initialize it with the data from our EnemyData asset.
        garbageItem.Initialize(enemyData.garbageDataOnDeath);

        // Change the GameObject's layer to "Interactable".
        gameObject.layer = LayerMask.NameToLayer("Interactable");

        Debug.Log(gameObject.name + " has become interactable garbage.");
    }
    else
    {
        // If no garbage data is assigned, just make it non-collidable.
        if(enemyCollider != null) enemyCollider.enabled = false;
    }
}




   
}
