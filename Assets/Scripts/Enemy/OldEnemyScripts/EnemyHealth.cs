using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;



[RequireComponent(typeof(EnemyBrain))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyHealth : MonoBehaviour
{
    #region Configuration
    [Header("Data & Stats")]
    private EnemyData _data;
    public EnemyData Data => _data;
    public float RuntimeDamageMultiplier { get; private set; } = 1f;

    [Header("Audio")]
    [SerializeField] private SoundDef _walkOverCorpseSound;
    [SerializeField] private SoundDef _hitSound;

    [Header("Dependencies")]
    [SerializeField] private FloatingEnemyHealthBar _healthBar;

    // Internal State
    public bool IsDead { get; private set; } = false;
    private float _currentHealth;
    private float _maxHealth;
    #endregion

    #region Sub-Systems Configuration
    [Serializable]
    public class RagdollSettings
    {
        public Transform rootBone;
        public int corpseLayerIndex; // "Corpse"
        public int defaultLayerIndex = 0; // "Default"
        public float ragdollDisableDelay = 10f;
        public float deathForceMultiplier = 150f;
    }

    [Serializable]
    public class VisualSettings
    {
        public Transform modelRoot; // The parent of the mesh
        public float dissolveDuration = 2.5f;
        public float hitFlashDuration = 0.08f;
        public float damageTextOffsetY = 0f;
        public float particleLifetime = 3f;
    }

    [Serializable]
    public class LootSettings
    {
        [Tooltip("DRAG THE CHILD 'InteractionSensor' OBJECT HERE.")]
        public Collider interactionSensor;
        public GameObject garbageBoxPrefab;
        public float spawnBoxDelay = 1.0f;
        public float interactableDelay = 0.2f;
    }

    float waitForCorpseToTurnIntoGarbageTime = 1f;

    [Space(10)]
    [SerializeField] private RagdollSettings _ragdollConfig;
    [SerializeField] private VisualSettings _visualConfig;
    [SerializeField] private LootSettings _lootConfig;
    #endregion

    #region Component Cache
    private EnemyBrain _brain;
    private NavMeshAgent _agent;
    private Collider _mainCollider;
    private Animator _animator;
    private AttackBehaviour[] _attacks;
    #endregion

    // Helper Instances
    private RagdollHandler _ragdollHandler;
    private VisualHandler _visualHandler;
    private LootHandler _lootHandler;

    public static event Action<EnemyData> OnEnemyDeath;

    private void Awake()
    {
        CacheComponents();
        InitializeSubSystems();
    }

    private void CacheComponents()
    {
        _brain = GetComponent<EnemyBrain>();
        _agent = GetComponent<NavMeshAgent>();
        _mainCollider = GetComponent<Collider>();
        _animator = GetComponentInChildren<Animator>();
        _attacks = GetComponents<AttackBehaviour>();
    }

    private void InitializeSubSystems()
    {
        _ragdollHandler = new RagdollHandler(this, _ragdollConfig, _animator, _agent, _mainCollider);

        var renderer = _visualConfig.modelRoot != null ? _visualConfig.modelRoot.GetComponentInChildren<SkinnedMeshRenderer>() : null;
        _visualHandler = new VisualHandler(this, _visualConfig, renderer);

        var outline = _visualConfig.modelRoot != null ? _visualConfig.modelRoot.GetComponentInChildren<Outline>() : null;
        _lootHandler = new LootHandler(this, _lootConfig, outline);
    }

    public void Initialize(EnemyData data, float hpMult, float dmgMult)
    {
        if (data == null)
        {
            Debug.LogError($"[EnemyHealth] Missing Data on {name}");
            return;
        }

        _data = data;
        RuntimeDamageMultiplier = dmgMult;
        _maxHealth = _data.maxHealth * hpMult;
        _currentHealth = _maxHealth;

        // Propagate Init to components
        if (_agent) _agent.speed = _data.moveSpeed;
        if (_brain) _brain.Initialize(_data);
        if (_attacks != null)
        {
            foreach (var attack in _attacks) attack.Initialize(_brain, _data);
        }

        if (_healthBar) _healthBar.UpdateHealthBar(_currentHealth, _maxHealth);
    }

    private void OnEnable()
    {
        IsDead = false;
        this.enabled = true;

        // Restore Alive State
        if (_brain) _brain.enabled = true;
        if (_healthBar) _healthBar.gameObject.SetActive(true);

        _ragdollHandler.ToggleRagdoll(false);
        _lootHandler.Reset();
        _visualHandler.Reset();
    }

    #region Public Interface (Damage & Interaction)

    public void TakeDamage(float damage) => TakeDamage(damage, transform.position + Vector3.up);

    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (IsDead) return;

        // Visual Feedback
        if (DamageTextManager.Instance != null)
            DamageTextManager.Instance.ShowDamage(damage, hitPoint + Vector3.up * _visualConfig.damageTextOffsetY, damage > 150);

        _visualHandler.Flash();
        _visualHandler.SpawnBloodEffect(hitPoint, _data?.bloodVFX);
        SoundManager.Instance.Play(_hitSound, transform.position);

        if (_brain) _brain.PlayHitAnimation();

        // Logic
        _currentHealth -= damage;
        if (_healthBar) _healthBar.UpdateHealthBar(_currentHealth, _maxHealth);

        if (_currentHealth <= 0) Die();
    }

    public void HandleExternalTrigger(Collider other)
    {
        // Bridge for the interaction sensor logic
        if (!IsDead || _visualHandler.IsDissolving) return;

        if (other.CompareTag("Player"))
        {
            SoundManager.Instance.Play(_walkOverCorpseSound, transform.position);
            var garbageHandler = other.GetComponentInParent<PlayerGarbageHandler>();
            StartCoroutine(CollectCorpseRoutine(garbageHandler));
        }
    }

    #endregion

    #region Death Logic

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;

        OnEnemyDeath?.Invoke(_data);
        if (_healthBar) _healthBar.gameObject.SetActive(false);
        if (_brain) _brain.enabled = false;

        if (CorpseManager.Instance != null) CorpseManager.Instance.RegisterCorpse(this);

        // Sub-system sequencing
        _ragdollHandler.ToggleRagdoll(true); // 1. Physics
        _lootHandler.EnableLootInteraction(); // 2. Interaction
    }

    private IEnumerator CollectCorpseRoutine(PlayerGarbageHandler collector)
    {
        _lootHandler.DisableInteraction(); // Prevent double trigger

        if (CorpseManager.Instance != null) CorpseManager.Instance.UnregisterCorpse(this);

        // Start the dissolve immediately on player interaction.
        StartCoroutine(_visualHandler.DissolveRoutine());

        // Spawn loot after a short fixed delay, independent of dissolve timing quirks.
        yield return new WaitForSeconds(waitForCorpseToTurnIntoGarbageTime);
        _lootHandler.SpawnGarbageBox(_data, collector);

        // Keep the enemy active long enough for the dissolve to visually complete
        // before returning this enemy to the pool.
        yield return new WaitForSeconds(_visualConfig.dissolveDuration);

        ReturnToPool();
    }

    public void ReturnToPool()
    {
        _ragdollHandler.ResetRootBone();

        if (EnemyPooler.Instance != null)
            EnemyPooler.Instance.ReturnEnemyToPool(_data, this.gameObject);
        else
            Destroy(gameObject);
    }

    #endregion

    #region Helper Classes (Composition)


    /// <summary>
    /// Handles Rigidbodies, Colliders, Layers, and Physical Forces.
    /// </summary>
    private class RagdollHandler
    {
        private readonly EnemyHealth _ctx;
        private readonly RagdollSettings _settings;
        private readonly Animator _animator;
        private readonly NavMeshAgent _agent;
        private readonly Collider _mainCollider;

        private Rigidbody[] _rigidbodies;
        private Collider[] _colliders;

        public RagdollHandler(EnemyHealth ctx, RagdollSettings settings, Animator anim, NavMeshAgent agent, Collider mainCol)
        {
            _ctx = ctx;
            _settings = settings;
            _animator = anim;
            _agent = agent;
            _mainCollider = mainCol;

            if (_settings.rootBone != null)
            {
                _rigidbodies = _settings.rootBone.GetComponentsInChildren<Rigidbody>();
                _colliders = _settings.rootBone.GetComponentsInChildren<Collider>();
            }
        }

        public void ToggleRagdoll(bool isRagdoll)
        {
            // Toggle Logic Components
            if (_mainCollider) _mainCollider.enabled = !isRagdoll;
            if (_agent) _agent.enabled = !isRagdoll;
            if (_animator) _animator.enabled = !isRagdoll;

            if (_rigidbodies == null) return;

            foreach (var rb in _rigidbodies)
            {
                if (isRagdoll)
                {
                    rb.isKinematic = false;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                }
                else
                {
                    
                    if (!rb.isKinematic)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }

                    rb.isKinematic = true;
                    rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                }
            }

            foreach (var col in _colliders)
            {
                col.enabled = isRagdoll;
                // Layer Switching: Corpse vs Default
                col.gameObject.layer = isRagdoll ? _settings.corpseLayerIndex : _settings.defaultLayerIndex;
            }

            if (isRagdoll)
            {
                ApplyDeathForce();
                _ctx.StartCoroutine(DisablePhysicsAfterDelay());
            }
        }

        private void ApplyDeathForce()
        {
            var hipRB = _settings.rootBone.GetComponent<Rigidbody>();
            if (hipRB)
            {
                Vector3 force = -_ctx.transform.forward * _settings.deathForceMultiplier;
                hipRB.AddForce(force, ForceMode.Impulse);
                hipRB.AddTorque(UnityEngine.Random.insideUnitSphere * (_settings.deathForceMultiplier * 0.1f), ForceMode.Impulse);
            }
        }

        private IEnumerator DisablePhysicsAfterDelay()
        {
            yield return new WaitForSeconds(_settings.ragdollDisableDelay);
            if (!_ctx.IsDead) yield break;

            foreach (var rb in _rigidbodies)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }
                
            }
        }

        public void ResetRootBone()
        {
            if (_settings.rootBone)
            {
                _settings.rootBone.localPosition = Vector3.zero;
                _settings.rootBone.localRotation = Quaternion.identity;
            }
        }
    }

    /// Handles Shaders, Materials, Particle Effects, and Outlines.
    private class VisualHandler
    {
        private readonly EnemyHealth _ctx;
        private readonly VisualSettings _settings;
        private readonly SkinnedMeshRenderer _renderer;
        private MaterialPropertyBlock _propBlock;

        private static readonly int DissolveID = Shader.PropertyToID("_Dissolve");
        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

        public bool IsDissolving { get; private set; }

        public VisualHandler(EnemyHealth ctx, VisualSettings settings, SkinnedMeshRenderer renderer)
        {
            _ctx = ctx;
            _settings = settings;
            _renderer = renderer;
            _propBlock = new MaterialPropertyBlock();
        }

        public void Reset()
        {
            IsDissolving = false;
            if (_renderer)
            {
                _renderer.GetPropertyBlock(_propBlock);
                _propBlock.Clear();
                _renderer.SetPropertyBlock(_propBlock);
            }
        }

        public void Flash()
        {
            if (_renderer) _ctx.StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(EmissionColorID, Color.white);
            _renderer.SetPropertyBlock(_propBlock);
            yield return new WaitForSeconds(_settings.hitFlashDuration);
            _propBlock.Clear();
            _renderer.SetPropertyBlock(_propBlock);
        }

        public void SpawnBloodEffect(Vector3 pos, GameObject prefab)
        {
            if (prefab != null)
            {
                var vfx = Instantiate(prefab, pos, Quaternion.identity);
                Destroy(vfx, _settings.particleLifetime);
            }
        }

        public IEnumerator DissolveRoutine()
        {
            IsDissolving = true;
            float timer = 0f;
            while (timer < _settings.dissolveDuration)
            {
                timer += Time.deltaTime;
                float val = Mathf.Lerp(0f, 1f, timer / _settings.dissolveDuration);

                _renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetFloat(DissolveID, val);
                _renderer.SetPropertyBlock(_propBlock);
                yield return null;
            }
        }
    }

    /// Handles interaction sensors, loot spawning, and interaction proxies.
    private class LootHandler
    {
        private readonly EnemyHealth _ctx;
        private readonly LootSettings _settings;
        private readonly Outline _outline;

        public LootHandler(EnemyHealth ctx, LootSettings settings, Outline outline)
        {
            _ctx = ctx;
            _settings = settings;
            _outline = outline;
        }

        public void Reset()
        {
            if (_outline) _outline.enabled = false;

            if (_settings.interactionSensor != null)
            {
                // Remove old proxy if exists
                var oldProxy = _settings.interactionSensor.GetComponent<CorpseLootLogic>();
                if (oldProxy) Destroy(oldProxy);

                _settings.interactionSensor.enabled = false;
                _settings.interactionSensor.isTrigger = false;
                _settings.interactionSensor.gameObject.SetActive(false);
            }
        }

        public void EnableLootInteraction()
        {
            _ctx.StartCoroutine(ActivateTriggerRoutine());
        }

        private IEnumerator ActivateTriggerRoutine()
        {
            yield return new WaitForSeconds(_settings.interactableDelay);

            if (_settings.interactionSensor != null)
            {
                _settings.interactionSensor.gameObject.SetActive(true);
                //Force sensor to Default layer so player can raycast/collide
                _settings.interactionSensor.gameObject.layer = 0; // Default

                _settings.interactionSensor.enabled = true;
                _settings.interactionSensor.isTrigger = true;

                var proxy = _settings.interactionSensor.gameObject.AddComponent<CorpseLootLogic>();
                proxy.Setup(_ctx, _outline);
            }
        }

        public void DisableInteraction()
        {
            if (_outline) _outline.enabled = false;
            if (_settings.interactionSensor) _settings.interactionSensor.gameObject.SetActive(false);
        }

        public void SpawnGarbageBox(EnemyData data, PlayerGarbageHandler magnetTarget = null)
        {
            if (_settings.garbageBoxPrefab == null || data.garbageDataOnDeath == null) return;

            // Spawn immediately after dissolve, before the enemy is returned to the pool.
            Vector3 spawnPos = _ctx.transform.position;
            if (_ctx._ragdollConfig.rootBone != null)
                spawnPos = _ctx._ragdollConfig.rootBone.position;

            GameObject box = Instantiate(_settings.garbageBoxPrefab, spawnPos, Quaternion.identity);

            if (box.TryGetComponent(out GarbageItem gItem))
            {
                gItem.isPooledObject = false;
                gItem.ActivatePooledInteractable(data.garbageDataOnDeath);

                // If configured, immediately start magnet pickup toward the player who collected the corpse
                if (magnetTarget != null && data.garbageDataOnDeath.useMagnetPickup)
                {
                    gItem.StartMagnet(magnetTarget.transform, magnetTarget);
                }
            }
        }
    }
    #endregion

    private void OnDrawGizmos()
    {
        if (_lootConfig.interactionSensor != null && _lootConfig.interactionSensor.enabled)
        {
            Gizmos.color = IsDead ? Color.red : Color.gray;
            if (_lootConfig.interactionSensor is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.transform.TransformPoint(sphere.center), sphere.radius);
            }
        }
    }
}
