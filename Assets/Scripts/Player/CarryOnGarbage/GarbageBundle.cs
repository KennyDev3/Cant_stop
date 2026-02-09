using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class GarbageBundle : MonoBehaviour, IInteractable
{
    private List<GarbageData> _contents = new List<GarbageData>();
    private Rigidbody _rb;
    private Collider _col;
    private bool _isConsumed = false;
    private Vector3 _originalScale;

    [Header("Impact Thresholds")]
    [Tooltip("Minimum velocity to play any sound at all.")]
    [SerializeField] private float minImpactVelocity = 1.5f;
    [Tooltip("Velocity required to trigger the Medium sound.")]
    [SerializeField] private float mediumThreshold = 6f;
    [Tooltip("Velocity required to trigger the Strong sound.")]
    [SerializeField] private float strongThreshold = 12f;

    [Header("Impact Sounds")]
    [SerializeField] private SoundDef soundImpactSmall;
    [SerializeField] private SoundDef soundImpactMedium;
    [SerializeField] private SoundDef soundImpactStrong;

    private float _lastImpactSoundTime;
    private const float IMPACT_COOLDOWN = 0.1f;

    [Header("Impact Particles")]
    [SerializeField] private ParticleSystem impactPuffPrefab;
    [SerializeField] private float minParticleVelocity = 2f;



    [SerializeField] private Outline targetOutline;

    [Header("Heavy Physics Settings")]
    [Tooltip("Multiplies global gravity. 1 = Normal. 3 = Heavy/Snappy.")]
    [SerializeField] private float gravityMultiplier = 3.0f;

    [Header("Damping & Friction")]
    [Tooltip("Air Resistance. Keep extremely low (0.05) so it flies fast.")]
    [SerializeField] private float airDamping = 0.05f;

    [Tooltip("Damping applied when on ground but still moving fast (Sliding/Rolling).")]
    [SerializeField] private float rollingDamping = 1.0f;

    [Tooltip("Damping applied when nearly stopped to prevent 'ghost sliding'.")]
    [SerializeField] private float stoppingDamping = 5.0f;

    [Header("Bounce Settings")]
    [Tooltip("How bouncy the object is (0.0 to 1.0). Trash bags usually are 0.2.")]
    [SerializeField] private float bounciness = 0.25f;
    [Tooltip("Friction against the floor. Higher = stops sliding sooner.")]
    [SerializeField] private float friction = 0.6f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float rotationDamping = 1f;

    [Header("Visuals & Effects")]
    [SerializeField] private float minScaleXYZ = 1.3f;
    [SerializeField] private float maxScaleXYZ = 2.5f;
    [SerializeField] private TrailRenderer speedTrail;

    // Thresholds
    private float _slideThreshold = 2.0f; // Speed below which we consider the object "stopping"
    private bool _hasHitGround = false;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();

        // 1. Setup Rigidbody
        _rb.useGravity = false; // We use custom gravity
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // 2. Setup Physics Material (Unity 6 / 2023+ Compatible)
        PhysicsMaterial garbageMat = new PhysicsMaterial("GarbageMat");
        garbageMat.bounciness = bounciness;
        garbageMat.dynamicFriction = friction;
        garbageMat.staticFriction = friction;
        garbageMat.bounceCombine = PhysicsMaterialCombine.Average;
        garbageMat.frictionCombine = PhysicsMaterialCombine.Average;

        _col.material = garbageMat;
    }

    void FixedUpdate()
    {
        if (_rb == null || _rb.isKinematic) return;

        _rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);

        if (_hasHitGround)
        {
            float currentSpeed = _rb.linearVelocity.magnitude;

            if (currentSpeed > _slideThreshold)
            {
                // Moving fast on ground = Slide/Roll (Low Damping)
                _rb.linearDamping = rollingDamping;
            }
            else
            {
                // Moving slow on ground = Brake (High Damping)
                _rb.linearDamping = stoppingDamping;
            }
        }
        else
        {
            // In Air
            _rb.linearDamping = airDamping;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Switch to ground mode on impact
        if (!_hasHitGround)
        {
            _hasHitGround = true;
            _rb.angularDamping = 2.0f; // Dampen spin slightly on impact
        }

        HandleImpactSound(collision);
        HandleImpactParticles(collision);
    }

    public void InitializeBundle(List<GarbageData> data, Vector3 direction, float force, float fullnessRatio)
    {
        _contents = new List<GarbageData>(data);

        // Scale Logic
        fullnessRatio = Mathf.Clamp01(fullnessRatio);
        float targetScale = Mathf.Lerp(minScaleXYZ, maxScaleXYZ, fullnessRatio);
        transform.localScale = Vector3.one * targetScale;
        _originalScale = transform.localScale;

        // Physics Reset
        _rb.isKinematic = false;
        _hasHitGround = false;

        // Reset Damping
        _rb.linearDamping = airDamping;
        _rb.angularDamping = rotationDamping;

        // Launch
        _rb.AddForce(direction * force, ForceMode.VelocityChange);
        _rb.AddTorque(Random.insideUnitSphere * rotationSpeed, ForceMode.Impulse);

        if (speedTrail != null)
        {
            speedTrail.emitting = true;
            StartCoroutine(TrailWatchdog());
        }
    }

    // ---------------------------------------------
    // LOGIC & INTERACTION
    // ---------------------------------------------

    public List<GarbageData> GetContents()
    {
        return _contents;
    }

    public int GetTotalCapacity()
    {
        int total = 0;
        foreach (var item in _contents) total += item.capacityCost;
        return total;
    }



    // --- RESTORED METHOD ---
    public void ShrinkToPercentage(float percentage)
    {
        transform.localScale = _originalScale * percentage;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (_isConsumed) return;

        var handler = interactor.GetComponent<PlayerGarbageHandler>();
        if (handler != null)
        {
            if (handler.TryCollectBundle(this))
            {
                _isConsumed = true;
                Destroy(gameObject);
            }
        }
    }

    // ---------------------------------------------
    // Sound & Particle Logic
    // ---------------------------------------------

    private void HandleImpactSound(Collision collision)
    {
        if (Time.time < _lastImpactSoundTime + IMPACT_COOLDOWN) return;

        float impactVelocity = collision.relativeVelocity.magnitude;

        // Ignore tiny vibrations or slow rolls
        if (impactVelocity < minImpactVelocity) return;

        _lastImpactSoundTime = Time.time;

        // Determine which tier to play
        if (impactVelocity >= strongThreshold)
        {
            SoundManager.Instance.Play(soundImpactStrong, transform.position);
        }
        else if (impactVelocity >= mediumThreshold)
        {
            SoundManager.Instance.Play(soundImpactMedium, transform.position);
        }
        else
        {
            // For small impacts,  pass a slight pitch override 
            float pitchMod = Mathf.Lerp(0.8f, 1.2f, impactVelocity / mediumThreshold);
            SoundManager.Instance.Play(soundImpactSmall, transform.position, pitchMod);
        }
    }

    private void HandleImpactParticles(Collision collision)
    {
        float impactVelocity = collision.relativeVelocity.magnitude;

        // Ignore tiny hits
        if (impactVelocity < minImpactVelocity) return;

        
        float targetSize = (impactVelocity >= mediumThreshold) ? 0.25f : 0.15f;
        ContactPoint contact = collision.GetContact(0);
        Vector3 spawnPos = contact.point + (contact.normal * 0.1f);

        ParticleSystem puff = Instantiate(impactPuffPrefab, spawnPos, Quaternion.LookRotation(contact.normal));

        puff.transform.localScale = new Vector3(targetSize, targetSize, targetSize);

        
        Destroy(puff.gameObject, 2.0f);
    }

    public string GetInteractionPrompt()
    {
        return $"Pick up Bundle (+{GetTotalCapacity()})";
    }

    public void Highlight()
    {
        if (targetOutline) { targetOutline.OutlineColor = Color.yellow; targetOutline.enabled = true; }
    }

    public void Unhighlight()
    {
        if (targetOutline) { targetOutline.OutlineColor = Color.white; targetOutline.enabled = false; }
    }

    private IEnumerator TrailWatchdog()
    {
        yield return new WaitForSeconds(0.1f);
        // Note: linearVelocity is the Unity 6 API. Use 'velocity' if on older versions.
        while (_rb.linearVelocity.sqrMagnitude > 1f)
        {
            yield return new WaitForSeconds(0.1f);
        }
        if (speedTrail != null) speedTrail.emitting = false;
    }
}