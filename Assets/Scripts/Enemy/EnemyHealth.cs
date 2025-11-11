using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    public EnemyData enemyData;
    private float currentHealth;
    public bool isDead = false;
    private EnemyController enemyController;
    private NavMeshAgent navMeshAgent;
    private Collider enemyCollider;
    private Rigidbody rb;
    public float makeCorposeInteractableDelay = 1.5f;
    private float coprseInteractionSphereSize = 0.006f;

    private float particleEffectDestroyTime = 3f;

    [Header("Ragdoll Setup")]
    [Tooltip("Follows Ragdolled body on Death to allow Pickup")]
    public Transform ragdollRootBone;

    [Tooltip("The GameObject that holds the mesh (sibling to the Hips).")]
    public Transform adventurerModel;

    [Header("Death Physics")]
    public float deathForceMultiplier = 150f;





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

        if (enemyController != null)
            enemyController.PlayHitAnimation();



        if (currentHealth <= 0)
            Die();
    }

    private void PlayHitEffect(Vector3 position)
    {
        if (enemyData.bloodVFX != null)
        {
            GameObject effect = Instantiate(enemyData.bloodVFX, position, Quaternion.Euler(0, 180, 0));
            Destroy(effect, particleEffectDestroyTime);
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log(gameObject.name + " has died.");
        

        enemyController.HandleDeath(); // disables animator

        if (enemyController != null) enemyController.enabled = false;
        if (navMeshAgent != null) navMeshAgent.enabled = false;
        if (enemyCollider != null) enemyCollider.enabled = false;

        Rigidbody[] ragdollRBs = ragdollRootBone.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in ragdollRBs)
        {
            if (rb.gameObject == ragdollRootBone.gameObject)
            {
                rb.isKinematic = false;
            }
            else
            {
                rb.isKinematic = false;
            }
        }

        ApplyDeathForce(ragdollRBs);


        if (enemyData.garbageDataOnDeath != null && adventurerModel != null)
        {
            // Start the coroutine to wait 2 seconds, then create the collider and components
            StartCoroutine(ActivateLootColliderDelayed(makeCorposeInteractableDelay));
        }
        else
        {
            Debug.LogWarning("Enemy Data or Model reference missing. Loot process aborted.");
        }

    }

    private void ApplyDeathForce(Rigidbody[] rbs)
    {
        Rigidbody hipRB = ragdollRootBone.GetComponent<Rigidbody>();

        if (hipRB != null)
        {
           
            hipRB.linearVelocity = Vector3.zero; // Zero out NavMesh Movement momentum


            
            Vector3 backwardDirection = -transform.forward;
            Vector3 horizontalForce = backwardDirection * deathForceMultiplier;

            
            float verticalOffset = 0.1f; 
            Vector3 pointOfImpact = hipRB.position + ragdollRootBone.up * verticalOffset;

            hipRB.AddForceAtPosition(horizontalForce, pointOfImpact, ForceMode.Impulse);

            // : Apply minor torque for extra tumble
            hipRB.AddTorque(Random.insideUnitSphere * deathForceMultiplier * 0.1f, ForceMode.Impulse);

            Debug.Log($"Applied flinging death force of {horizontalForce} at offset position to the Hips.");
        }

        //  Apply a small, random force to the other limbs to make them flail out
        foreach (Rigidbody rb in rbs)
        {
            if (rb != hipRB)
            {
                rb.AddForce(Random.insideUnitSphere * 10f, ForceMode.VelocityChange);
            }
        }
    }

    private IEnumerator ActivateLootColliderDelayed(float delay)
    {
        // Wait for the ragdoll to settle
        yield return new WaitForSeconds(delay);

        GameObject corpseGO = adventurerModel.gameObject;

        
        GarbageItem garbageItem = corpseGO.AddComponent<GarbageItem>();
        garbageItem.Initialize(enemyData.garbageDataOnDeath);

        garbageItem.destroyTarget = this.gameObject; 

        SphereCollider interactionCollider = corpseGO.AddComponent<SphereCollider>();
        interactionCollider.radius = coprseInteractionSphereSize;
        interactionCollider.isTrigger = true;
        interactionCollider.enabled = true; 

        corpseGO.layer = LayerMask.NameToLayer("Interactable");

        Debug.Log($"Loot collider (R={interactionCollider.radius}) activated on {corpseGO.name} after {delay} seconds.");

        // Optional: Clean up the EnemyHealth component's original GO after a long delay
    }
}
