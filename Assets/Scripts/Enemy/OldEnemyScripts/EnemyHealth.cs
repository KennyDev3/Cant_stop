using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    private EnemyData _data;
    public EnemyData Data => _data;

    public bool isDead { get; private set; } = false;
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

    [Header("Pooling Architecture")]
    public GarbageItem attachedGarbageComponent;

    [Header("Effects")]
    public float makeCorposeInteractableDelay = 2.75f;
    private float particleEffectDestroyTime = 3f;
    public float damageTextOffsetY = 0f;

    [Header("Dissolve Settings")]
    public float dissolveDuration = 2.5f;

    [Header("Ragdoll Setup")]
    public Transform ragdollRootBone;
    public Transform adventurerModel;

    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;

    private SkinnedMeshRenderer _meshRenderer;
    private MaterialPropertyBlock _propBlock;

    private static readonly int DissolveID = Shader.PropertyToID("_Dissolve");
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    private Coroutine _flashRoutine;
    private float meshHitFlashDuration = 0.08f;
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

        if (attachedGarbageComponent != null)
        {
            attachedGarbageComponent.ResetPooledInteractable();
            attachedGarbageComponent.OnCollected += HandleLootCollected;
        }

        SetRagdollState(false);
    }

    private void OnDisable()
    {
        if (attachedGarbageComponent != null)
        {
            attachedGarbageComponent.OnCollected -= HandleLootCollected;
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

        OnEnemyDeath?.Invoke(_data); // Kill count event

        if (healthBar != null) healthBar.gameObject.SetActive(false);

        // Enable full ragdoll Physics
        SetRagdollState(true);

        if (enemyBrain != null) enemyBrain.enabled = false;

        ApplyDeathForce();

        if (_data.garbageDataOnDeath != null && attachedGarbageComponent != null)
        {
            StartCoroutine(ActivateLootColliderDelayed(makeCorposeInteractableDelay));
        }

        if (CorpseManager.Instance != null)
        {
            CorpseManager.Instance.RegisterCorpse(this);
        }
    }

    private IEnumerator ActivateLootColliderDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (attachedGarbageComponent != null)
        {
            attachedGarbageComponent.ActivatePooledInteractable(_data.garbageDataOnDeath);
        }

        OptimizeCorpsePhysics();
    }

    private void OptimizeCorpsePhysics()
    {
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = true;
        }

        foreach (Collider col in ragdollColliders)
        {
            if (col.transform != ragdollRootBone)
            {
                col.enabled = false;
            }
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

    private void HandleLootCollected(GarbageItem item)
    {
        if (CorpseManager.Instance != null) CorpseManager.Instance.UnregisterCorpse(this);
        StartCoroutine(DissolveRoutine());
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

        _meshRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetFloat(DissolveID, 1f);
        _meshRenderer.SetPropertyBlock(_propBlock);

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

    private void PlayHitEffect(Vector3 position)
    {
        if (_data == null) return;
        if (_data.bloodVFX != null)
        {
            GameObject effect = Instantiate(_data.bloodVFX, position, Quaternion.identity);
            Destroy(effect, particleEffectDestroyTime);
        }
    }

    public void TakeDamage(float damage)
    {
        if (_data == null) return;
        TakeDamage(damage, transform.position + Vector3.up);
    }

    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (isDead) return;
        if (_data == null) return;

        if (DamageTextManager.Instance != null) DamageTextManager.Instance.ShowDamage(damage, hitPoint + Vector3.up * damageTextOffsetY, damage > 150);

        currentHealth -= damage;
        StartFlash(meshHitFlashDuration);
        PlayHitEffect(hitPoint);

        if (healthBar != null) healthBar.UpdateHealthBar(currentHealth, finalMaxHealth);
        if (enemyBrain != null) enemyBrain.PlayHitAnimation();

        if (currentHealth <= 0) Die();
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
        _flashRoutine = null;
    }
}