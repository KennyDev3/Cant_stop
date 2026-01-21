using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    private EnemyData _data;
    public EnemyData Data => _data;

    [Header("Layer Configuration")]
    [Tooltip("The ID of the 'Corpse' layer created in Project Settings. Must be IGNORED by Player in Physics Matrix.")]
    [SerializeField] private int corpseLayerIndex;
    [Tooltip("The ID of the 'Default' layer. Must be DETECTED by Player.")]
    [SerializeField] private int defaultLayerIndex = 0;

    [Header("Audio")]
    [SerializeField] private SoundDef playerWalksOverEnemyCorpseSound;
    [SerializeField] private SoundDef enemyHitSound;

    public bool isDead { get; private set; } = false;
    private bool _isDissolving = false;
    public float RuntimeDamageMultiplier { get; private set; } = 1f;

    private float currentHealth;
    private float finalMaxHealth;

    // --- Cached Components ---
    private EnemyBrain enemyBrain;
    private NavMeshAgent navMeshAgent;
    private Collider enemyCollider; // The main capsule (alive)
    private Animator animator;
    private AttackBehaviour[] attackBehaviours;

    [SerializeField] FloatingEnemyHealthBar healthBar;

    [Header("Loot System")]
    [Tooltip("The prefab (box) that will be spawned when the player runs over the corpse.")]
    public GameObject garbageBoxPrefab;
    public float spawnBoxDelay = 1.0f;
    
    [Header("Corpse Timings")]
    public float lootInteractableDelay = 0.2f;
    public float ragdollDisableDelay = 10f;

    [Header("Visuals")]
    private float particleEffectDestroyTime = 3f;
    public float damageTextOffsetY = 0f;
    public float dissolveDuration = 2.5f;
    private float meshHitFlashDuration = 0.08f;

    [Header("Ragdoll Setup")]
    public Transform ragdollRootBone;
    public Transform adventurerModel;

    [Tooltip("DRAG THE CHILD 'InteractionSensor' OBJECT HERE. Do NOT drag the Hips bone.")]
    public Collider interactionCollider;

    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;

    private SkinnedMeshRenderer _meshRenderer;
    private MaterialPropertyBlock _propBlock;

    private static readonly int DissolveID = Shader.PropertyToID("_Dissolve");
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    public float deathForceMultiplier = 150f;

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

        // 1. Reset Alive Components
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

        // 2. Disable Ragdoll (Resets layers to Default)
        SetRagdollState(false);

        // 3. Reset Interaction Sensor
        if (interactionCollider != null)
        {
            // Clean up any proxy script left over from pooling
            var oldProxy = interactionCollider.GetComponent<CorpseLootLogic>();
            if (oldProxy != null) Destroy(oldProxy);

            interactionCollider.enabled = false;
            interactionCollider.isTrigger = false;
            // Ensure it's hidden until death
            interactionCollider.gameObject.SetActive(false);
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

        // --- STEP 1: Enable Physics (Bones move to Corpse Layer) ---
        SetRagdollState(true);

        if (enemyBrain != null) enemyBrain.enabled = false;

        ApplyDeathForce();

        if (CorpseManager.Instance != null) CorpseManager.Instance.RegisterCorpse(this);

        // --- STEP 2: Wait, then Enable Trigger (Sensor moves to Default Layer) ---
        StartCoroutine(ActivateLootTriggerDelayed(lootInteractableDelay));
        StartCoroutine(DisableRagdollAfterDelay(ragdollDisableDelay));
    }

    private void SetRagdollState(bool isActive)
    {
        if (ragdollRigidbodies == null) return;

        // Disable "Alive" collider and navmesh
        if (enemyCollider != null) enemyCollider.enabled = !isActive;
        if (navMeshAgent != null) navMeshAgent.enabled = !isActive;
        if (animator != null) animator.enabled = !isActive;

        // Handle Limbs
        foreach (Collider col in ragdollColliders)
        {
            col.enabled = isActive;

            // KEY LOGIC: 
            // If ragdolling (Active) -> Set to "Corpse" layer (Ignored by Player).
            // If resetting (Inactive) -> Set back to "Default" layer.
            col.gameObject.layer = isActive ? corpseLayerIndex : defaultLayerIndex;
        }

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (isActive)
            {
                rb.isKinematic = false;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
            else
            {
                // Reset for pooling
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                }
                rb.isKinematic = true;
            }
        }
    }

    private IEnumerator ActivateLootTriggerDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (interactionCollider != null)
        {
            interactionCollider.gameObject.SetActive(true);

            // CRITICAL: Force the child sensor back to "Default" layer.
            // Even though it's a child of the Hips (which are on "Corpse"), 
            // explicitly setting the child's layer here overrides inheritance.
            interactionCollider.gameObject.layer = defaultLayerIndex;

            interactionCollider.enabled = true;
            interactionCollider.isTrigger = true;

            // Inject the Proxy Script
            var lootLogic = interactionCollider.gameObject.AddComponent<CorpseLootLogic>();

            Outline outline = adventurerModel.GetComponentInChildren<Outline>();
            lootLogic.Setup(this, outline);
        }
    }

    private IEnumerator DisableRagdollAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Don't freeze if we are already dissolving or revived
        if (_isDissolving || !isDead) yield break;

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // We do NOT disable the colliders here, or they would fall through the floor.
        // We just freeze them.
    }


    public void HandleExternalTrigger(Collider other)
    {
        if (!isDead || _isDissolving) return;

        if (other.CompareTag("Player"))
        {
            SoundManager.Instance.Play(playerWalksOverEnemyCorpseSound, transform.position);
            StartCoroutine(HandleCorpseCollectedSequence());
        }
    }

    private IEnumerator HandleCorpseCollectedSequence()
    {
        _isDissolving = true;

        Outline outline = adventurerModel.GetComponentInChildren<Outline>();
        if (outline != null) outline.enabled = false;

        if (CorpseManager.Instance != null) CorpseManager.Instance.UnregisterCorpse(this);

        // Turn off the sensor immediately so we don't trigger twice
        if (interactionCollider != null) interactionCollider.gameObject.SetActive(false);

        StartCoroutine(SpawnGarbageBoxDelayed());

        yield return StartCoroutine(DissolveRoutine());
    }

    private IEnumerator SpawnGarbageBoxDelayed()
    {
        yield return new WaitForSeconds(spawnBoxDelay);

        if (garbageBoxPrefab != null && _data != null && _data.garbageDataOnDeath != null)
        {
            // Spawn box at the hip position
            Vector3 spawnPos = ragdollRootBone.position;

            GameObject box = Instantiate(garbageBoxPrefab, spawnPos, Quaternion.identity);

            if (box.TryGetComponent(out GarbageItem gItem))
            {
                gItem.isPooledObject = false;
                gItem.ActivatePooledInteractable(_data.garbageDataOnDeath);
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

        if (EnemyPooler.Instance != null)
        {
            EnemyPooler.Instance.ReturnEnemyToPool(_data, this.gameObject);
        }
        else
        {
            Destroy(gameObject);
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

    private void OnDrawGizmos()
    {
        if (interactionCollider != null && interactionCollider.enabled)
        {
            Gizmos.color = isDead ? Color.red : Color.gray;
            if (interactionCollider is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.transform.TransformPoint(sphere.center), sphere.radius);
            }
        }
    }
}