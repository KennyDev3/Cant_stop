using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One node of the fire trail. Lives on "Fire Trailer Node Parent".
/// Starts child particle systems when spawned, damages enemies in trigger every tick, then scales down and destroys.
/// Requires a Rigidbody (added at runtime if missing) so Unity generates trigger events with enemy colliders.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class FireTrailNode : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float _damagePerTick = 10f;
    [SerializeField] private float _tickInterval = 0.25f;

    [Header("Lifetime")]
    [SerializeField] private float _lifetime = 4f;
    [SerializeField] private float _scaleDownDuration = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool _debugLog;

    private HashSet<EnemyHealth> _enemiesInTrigger = new HashSet<EnemyHealth>();
    private float _nextDamageTime;
    private Vector3 _initialScale;

    private void Awake()
    {
        var col = GetComponent<BoxCollider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        _initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        _enemiesInTrigger.Clear();
        _nextDamageTime = Time.time + _tickInterval;

        if (_debugLog)
            Debug.Log($"[FireTrailNode] OnEnable at {transform.position}. BoxCollider enabled={GetComponent<BoxCollider>().enabled}, isTrigger={GetComponent<BoxCollider>().isTrigger}", this);

        // Start all child particle systems so fire plays as soon as node is dropped
        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
            ps.Play(true);

        StartCoroutine(LifetimeRoutine());
    }

    private void Update()
    {
        if (Time.time < _nextDamageTime) return;

        _nextDamageTime += _tickInterval;
        int damaged = 0;
        foreach (var health in _enemiesInTrigger)
        {
            if (health != null && !health.IsDead)
            {
                health.TakeDamage(_damagePerTick);
                damaged++;
            }
        }
        if (_debugLog && damaged > 0)
            Debug.Log($"[FireTrailNode] Tick: dealt {_damagePerTick} to {damaged} enemy(ies). Set size={_enemiesInTrigger.Count}", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore the player (they spawn the trail; only enemies should take damage)
        if (other.CompareTag("Player") || other.GetComponentInChildren<PlayerHealth>() != null)
            return;

        if (_debugLog)
            Debug.Log($"[FireTrailNode] OnTriggerEnter: other={other.name}, root={other.transform.root.name}, layer={LayerMask.LayerToName(other.gameObject.layer)}", this);

        // EnemyHealth is often on a child (e.g. Enemy_advanturer); collider may be on root or that child
        var health = other.GetComponentInChildren<EnemyHealth>();
        if (health != null && !health.IsDead)
        {
            _enemiesInTrigger.Add(health);
            if (_debugLog)
                Debug.Log($"[FireTrailNode] Added EnemyHealth from {other.name}. Set size={_enemiesInTrigger.Count}", this);
        }
        else if (_debugLog && health == null)
            Debug.Log($"[FireTrailNode] No EnemyHealth on {other.name} or its children.", this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponentInChildren<PlayerHealth>() != null)
            return;

        var health = other.GetComponentInChildren<EnemyHealth>();
        if (health != null)
        {
            _enemiesInTrigger.Remove(health);
            if (_debugLog)
                Debug.Log($"[FireTrailNode] OnTriggerExit: removed {other.name}. Set size={_enemiesInTrigger.Count}", this);
        }
    }

    private IEnumerator LifetimeRoutine()
    {
        float waitTime = Mathf.Max(0f, _lifetime - _scaleDownDuration);
        yield return new WaitForSeconds(waitTime);

        float elapsed = 0f;
        while (elapsed < _scaleDownDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _scaleDownDuration;
            transform.localScale = Vector3.Lerp(_initialScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(gameObject);
    }
}
