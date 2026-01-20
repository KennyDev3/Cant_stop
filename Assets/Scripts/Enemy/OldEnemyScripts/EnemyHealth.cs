using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    private EnemyData _data;
    public EnemyData Data => _data;

    public bool isDead { get; private set; } = false;
    private bool _isDissolving = false;
    public float RuntimeDamageMultiplier { get; private set; } = 1f;

    private float currentHealth;
    private float finalMaxHealth;

    // --- Cached Components ---
    private EnemyBrain enemyBrain;
    private NavMeshAgent navMeshAgent;
    private Collider enemyCollider;
    private Animator animator;
    private AttackBehaviour[] attackBehaviours;

    [SerializeField] FloatingEnemyHealthBar healthBar;

    [Header("New Loot System")]
    [Tooltip("The prefab (box) that will be spawned when the player runs over the corpse.")]
    public GameObject garbageBoxPrefab;
    [Tooltip("How long after the player touches the corpse does the box appear?")]
    public float spawnBoxDelay = 1.0f;

    [Header("Effects & Timing")]
    public float makeCorposeInteractableDelay = 2.75f;
    private float particleEffectDestroyTime = 3f;
    public float damageTextOffsetY = 0f;

    [Header("Dissolve Settings")]
    public float dissolveDuration = 2.5f;

    [Header("Ragdoll Setup")]
    public Transform ragdollRootBone;
    public Transform adventurerModel;
    [Tooltip("This should be the Sphere Collider on the enemy's hip.")]
    public Collider interactionCollider;

    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;

    private SkinnedMeshRenderer _meshRenderer;
    private MaterialPropertyBlock _propBlock;

    [Header("Audio")]
    [SerializeField] private SoundDef enemyHitSound;

    private static readonly int DissolveID = Shader.PropertyToID("_Dissolve");
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    public float deathForceMultiplier = 150f;
    private float meshHitFlashDuration = 0.08f;

    public static event System.Action<EnemyData> OnEnemyDeath;

    void Awake()
    {
        enemyBrain = GetComponent<EnemyBrain>();
        attackBehaviours = GetComponents<AttackBehaviour>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        enemyCollider = GetComponent<Collider>();
        animator = GetComponentInChildren<Animator>();

        if (adventurerModel != null)
            _meshRenderer = adventurerModel.GetComponentInChildren<SkinnedMeshRenderer>();

        _propBlock = new MaterialPropertyBlock();

        if (ragdollRootBone != null)
        {
            ragdollRigidbodies = ragdollRootBone.GetComponentsInChildren<Rigidbody>();
            ragdollColliders = ragdollRootBone.GetComponentsInChildren<Collider>();
        }
    }

    public void Initialize(EnemyData dataToUse, float hpMult, float dmgMult)
    {
        if (dataToUse == null) { Debug.LogError($"[EnemyHealth] Missing Data on {gameObject.name}"); return; }

        _data = dataToUse;
        RuntimeDamageMultiplier = dmgMult;
        finalMaxHealth = _data.maxHealth * hpMult;
        currentHealth = finalMaxHealth;

        if (navMeshAgent != null) navMeshAgent.speed = _data.moveSpeed;
        if (enemyBrain != null) enemyBrain.Initialize(_data);

        if (attackBehaviours != null)
        {
            foreach (var attack in attackBehaviours) attack.Initialize(enemyBrain, _data);
        }

        if (healthBar != null) healthBar.UpdateHealthBar(currentHealth, finalMaxHealth);
    }

    private void OnEnable()
    {
        isDead = false;
        _isDissolving = false;
        this.enabled = true;

        if (enemyBrain != null) enemyBrain.enabled = true;
        if (enemyCollider != null) enemyCollider.enabled = true;
        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = true;
            navMeshAgent.isStopped = false;
        }
        if (animator != null) animator.enabled = true;
        if (healthBar != null) healthBar.gameObject.SetActive(true);

        ResetVisuals();
        SetRagdollState(false);

        // Ensure interaction collider starts disabled and not a trigger
        if (interactionCollider != null)
        {
            interactionCollider.enabled = false;
            interactionCollider.isTrigger = false;
        }
    }

    private void ResetVisuals()
    {
        if (_meshRenderer != null)
        {
            _meshRenderer.GetPropertyBlock(_propBlock);
            _propBlock.Clear();
            _meshRenderer.SetPropertyBlock(_propBlock);
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        OnEnemyDeath?.Invoke(_data);

        if (healthBar != null) healthBar.gameObject.SetActive(false);

        // Physics: Enable Ragdoll
        SetRagdollState(true);

        if (enemyBrain != null) enemyBrain.enabled = false;

        ApplyDeathForce();

        // 1. Register to manager for cleanup if the player never walks over it
        if (CorpseManager.Instance != null)
        {
            CorpseManager.Instance.RegisterCorpse(this);
        }

        // 2. Wait for ragdoll to settle before making it "collidable/triggerable"
        StartCoroutine(ActivateLootTriggerDelayed(makeCorposeInteractableDelay));
    }

    private IEnumerator ActivateLootTriggerDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        foreach (Rigidbody rb in ragdollRigidbodies) rb.isKinematic = true;
        foreach (Collider col in ragdollColliders) col.enabled = false;

        if (interactionCollider != null)
        {
            interactionCollider.enabled = true;
            interactionCollider.isTrigger = true;

            // Add the logic script to the Hips
            var lootLogic = interactionCollider.gameObject.GetComponent<CorpseLootLogic>();
            if (lootLogic == null) lootLogic = interactionCollider.gameObject.AddComponent<CorpseLootLogic>();

            // Find the outline on the model
            Outline outline = adventurerModel.GetComponentInChildren<Outline>();

            // Setup the colors and link to this script
            lootLogic.Setup(this, outline);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // DEBUG: Log everything that touches this trigger to see if it's working at all
        Debug.Log($"[Physics Check] {gameObject.name} trigger hit by: {other.name} (Tag: {other.tag})");

        if (!isDead || _isDissolving) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("<color=green>SUCCESS:</color> Player detected! Starting dissolve sequence.");
            StartCoroutine(HandleCorpseCollectedSequence());
        }
    }


    private IEnumerator HandleCorpseCollectedSequence()
    {
        _isDissolving = true;

        Outline outline = adventurerModel.GetComponentInChildren<Outline>();
        if (outline != null) outline.enabled = false;

        // Unregister from the manager so it doesn't try to ForceReturn while we dissolve
        if (CorpseManager.Instance != null) CorpseManager.Instance.UnregisterCorpse(this);

        // Start the box spawn routine (the box is NOT pooled)
        StartCoroutine(SpawnGarbageBoxDelayed());

        // Start the dissolve shader logic (which ends in ForceReturnToPool)
        yield return StartCoroutine(DissolveRoutine());
    }

    private IEnumerator SpawnGarbageBoxDelayed()
    {
        yield return new WaitForSeconds(spawnBoxDelay);

        if (garbageBoxPrefab != null && _data != null && _data.garbageDataOnDeath != null)
        {
            Vector3 spawnPos = interactionCollider != null ? interactionCollider.transform.position : transform.position;

            GameObject box = Instantiate(garbageBoxPrefab, spawnPos, Quaternion.identity);

            if (box.TryGetComponent(out GarbageItem gItem))
            {
                
                gItem.isPooledObject = false;

                gItem.ActivatePooledInteractable(_data.garbageDataOnDeath);

                Debug.Log($"[Loot System] Spawned box and injected data: {_data.garbageDataOnDeath.itemName}");
            }
            else
            {
                Debug.LogError("[Loot System] Spawned box prefab is missing a GarbageItem component!");
            }
        }
    }

    private IEnumerator DissolveRoutine()
    {
        float timer = 0f;
        while (timer < dissolveDuration)
        {
            timer += Time.deltaTime;
            float dissolveValue = Mathf.Lerp(0f, 1f, timer / dissolveDuration);

            _meshRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(DissolveID, dissolveValue);
            _meshRenderer.SetPropertyBlock(_propBlock);

            yield return null;
        }

        ForceReturnToPool();
    }

    public void ForceReturnToPool()
    {
        if (ragdollRootBone != null)
        {
            ragdollRootBone.localPosition = Vector3.zero;
            ragdollRootBone.localRotation = Quaternion.identity;
        }

        // Reset trigger state for next use
        if (interactionCollider != null) interactionCollider.isTrigger = false;

        if (EnemyPooler.Instance != null)
        {
            EnemyPooler.Instance.ReturnEnemyToPool(_data, this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetRagdollState(bool isActive)
    {
        if (ragdollRigidbodies == null) return;

        if (enemyCollider != null) enemyCollider.enabled = !isActive;
        if (navMeshAgent != null) navMeshAgent.enabled = !isActive;
        if (animator != null) animator.enabled = !isActive;

        foreach (Collider col in ragdollColliders) col.enabled = isActive;

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (isActive)
            {
                rb.isKinematic = false;
            }
            else
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
            }
        }
    }

    private void ApplyDeathForce()
    {
        Rigidbody hipRB = ragdollRootBone.GetComponent<Rigidbody>();
        if (hipRB != null)
        {
            Vector3 backwardDirection = -transform.forward;
            Vector3 horizontalForce = backwardDirection * deathForceMultiplier;
            hipRB.AddForce(horizontalForce, ForceMode.Impulse);
            hipRB.AddTorque(Random.insideUnitSphere * deathForceMultiplier * 0.1f, ForceMode.Impulse);
        }
    }

    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (isDead) return;
        if (_data == null) return;

        if (DamageTextManager.Instance != null)
            DamageTextManager.Instance.ShowDamage(damage, hitPoint + Vector3.up * damageTextOffsetY, damage > 150);

        currentHealth -= damage;
        StartFlash(meshHitFlashDuration);
        PlayHitEffect(hitPoint);

        SoundManager.Instance.Play(enemyHitSound, transform.position);

        if (healthBar != null) healthBar.UpdateHealthBar(currentHealth, finalMaxHealth);
        if (enemyBrain != null) enemyBrain.PlayHitAnimation();

        if (currentHealth <= 0) Die();
    }

    public void TakeDamage(float damage) => TakeDamage(damage, transform.position + Vector3.up);

    private void PlayHitEffect(Vector3 position)
    {
        if (_data?.bloodVFX != null)
        {
            GameObject effect = Instantiate(_data.bloodVFX, position, Quaternion.identity);
            Destroy(effect, particleEffectDestroyTime);
        }
    }

    public void StartFlash(float duration) { if (_meshRenderer) StartCoroutine(FlashRoutine(duration)); }

    private IEnumerator FlashRoutine(float d)
    {
        _meshRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(EmissionColorID, Color.white);
        _meshRenderer.SetPropertyBlock(_propBlock);

        yield return new WaitForSeconds(d);

        _propBlock.Clear();
        _meshRenderer.SetPropertyBlock(_propBlock);
    }

    // Enemy Collider Proxy messenger 
    public void HandleExternalTrigger(Collider other)
    {
        if (!isDead || _isDissolving) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log($"<color=green>[Loot System]</color> Player detected via Proxy on {gameObject.name}!");
            StartCoroutine(HandleCorpseCollectedSequence());
        }
    }
    // Visual Aid

    private void OnDrawGizmos()
    {
        if (interactionCollider != null && interactionCollider.enabled)
        {
            Gizmos.color = isDead ? Color.red : Color.gray;

            // If it's a sphere collider, draw a sphere
            if (interactionCollider is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.transform.TransformPoint(sphere.center), sphere.radius);
            }
        }
    }
}