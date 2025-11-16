using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Core Setup")]
    public EnemyData enemyData;
    public bool isDead { get; private set; } = false;
    private float currentHealth;

    // --- Cached Components for performance ---
    private EnemyBrain enemyBrain;
    private NavMeshAgent navMeshAgent;
    private Collider enemyCollider;
    private Animator animator; // Cache the Animator

    [SerializeField] FloatingEnemyHealthBar healthBar;

    [Header("Effects")]
    public float makeCorposeInteractableDelay = 1.5f;
    private float coprseInteractionSphereSize = 0.006f;
    private float particleEffectDestroyTime = 3f;

    [Header("Ragdoll Setup")]
    [Tooltip("The root of the ragdoll hierarchy (usually the Hips bone).")]
    public Transform ragdollRootBone;
    [Tooltip("The GameObject that holds the mesh renderer.")]
    public Transform adventurerModel;
    [Tooltip("How long the ragdoll corpse stays physically active before being frozen or pooled.")]
    public float corpseCleanupTime = 10f;


    //  Cached Ragdoll Components  
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;

    // Mesh Components for Hit Flash 
    private SkinnedMeshRenderer _meshRenderer;
    private MaterialPropertyBlock _propBlock;
    private int _colorPropertyID;
    private Coroutine _flashRoutine;

    private float meshHitFlashDuration = 0.08f;

    [Header("Death Physics")]
    public float deathForceMultiplier = 150f;

    void Awake()
    {
        // Cache main components
        enemyBrain = GetComponent<EnemyBrain>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        enemyCollider = GetComponent<Collider>();
        animator = GetComponentInChildren<Animator>(); // Animator is on child
        _meshRenderer = adventurerModel.GetComponentInChildren<SkinnedMeshRenderer>();

        _propBlock = new MaterialPropertyBlock();
        _colorPropertyID = Shader.PropertyToID("_EmissionColor");

        currentHealth = enemyData.maxHealth;

        // Optimized caching of Ragdoll components
        if (ragdollRootBone != null)
        {
            ragdollRigidbodies = ragdollRootBone.GetComponentsInChildren<Rigidbody>();
            ragdollColliders = ragdollRootBone.GetComponentsInChildren<Collider>();
        }
    }

    private void Start()
    {
        healthBar.UpdateHealthBar(currentHealth, enemyData.maxHealth);

        // --- OPTIMIZATION 2: Disable the ragdoll on start ---
        SetRagdollState(false);
    }

    // This is the core optimization function
    private void SetRagdollState(bool isActive)
    {
        if (ragdollRigidbodies == null) return;

        // Toggle the main collider and nav agent based on the *inverse* of the ragdoll state
        if (enemyCollider != null) enemyCollider.enabled = !isActive;
        if (navMeshAgent != null) navMeshAgent.enabled = !isActive;

        // Toggle the animator
        if (animator != null) animator.enabled = !isActive;

        // Toggle all ragdoll colliders
        foreach (Collider col in ragdollColliders)
        {
            col.enabled = isActive;
        }

        // Toggle all ragdoll rigidbodies
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = !isActive;
        }
    }


    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (isDead) return;

        currentHealth -= damage;

        StartFlash(meshHitFlashDuration);
        PlayHitEffect(hitPoint);
        healthBar.UpdateHealthBar(currentHealth, enemyData.maxHealth);

        if (enemyBrain != null)
        {
            enemyBrain.PlayHitAnimation();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (healthBar != null) Destroy(healthBar.gameObject);

        SetRagdollState(true);

        // Disable core logic scripts
        if (enemyBrain != null) enemyBrain.enabled = false;

        ApplyDeathForce();

        if (enemyData.garbageDataOnDeath != null && adventurerModel != null)
        {
            StartCoroutine(ActivateLootColliderDelayed(makeCorposeInteractableDelay));
        }

        StartCoroutine(CorpseCleanup());
    }

    private void ApplyDeathForce()
    {
        Rigidbody hipRB = ragdollRootBone.GetComponent<Rigidbody>();
        if (hipRB != null)
        {
            Vector3 backwardDirection = -transform.forward; // Use the main transform's forward
            Vector3 horizontalForce = backwardDirection * deathForceMultiplier;
            hipRB.AddForce(horizontalForce, ForceMode.Impulse);
            hipRB.AddTorque(Random.insideUnitSphere * deathForceMultiplier * 0.1f, ForceMode.Impulse);
        }
    }

    private IEnumerator CorpseCleanup()
    {
        yield return new WaitForSeconds(corpseCleanupTime);

        
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = true;
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

    private IEnumerator ActivateLootColliderDelayed(float delay)
    {
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
    }

    public void StartFlash(float duration)
    {
        if (_meshRenderer == null)
        {
            return;
        }

        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
        }

        // Start the new flash coroutine
        _flashRoutine = StartCoroutine(FlashRoutine(duration));
    }

    private IEnumerator FlashRoutine(float duration)
    {
        _meshRenderer.GetPropertyBlock(_propBlock);

        _propBlock.SetColor(_colorPropertyID, Color.white);
        _meshRenderer.SetPropertyBlock(_propBlock);

        yield return new WaitForSeconds(duration);

        _propBlock.Clear();
        _meshRenderer.SetPropertyBlock(_propBlock);
        _flashRoutine = null;
    }
}