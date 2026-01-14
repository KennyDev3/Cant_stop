using UnityEngine;
using System.Collections.Generic;

public class GarbageRefundProjectile : MonoBehaviour
{
    private Transform _targetPlayer;
    private List<GarbageData> _dataToRefund;

    [Header("Movement Settings")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float reachDistance = 0.5f;

    [Header("Squiggly Settings")]
    [SerializeField] private float frequency = 12f; // Speed of wiggle
    [SerializeField] private float magnitude = 0.6f;  // Size of wiggle

    private float _startTime;
    private bool _isHoming = false;
    private Vector3 _wiggleAxis;

    public void Setup(Transform player, List<GarbageData> data)
    {
        _targetPlayer = player;
        _dataToRefund = data;
        _startTime = Time.time;

        // Randomize the "wiggle" plane so multiple items look like a swarm
        _wiggleAxis = Random.insideUnitSphere.normalized;

        // Start with a small pop upwards
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(Vector3.up * 5f, ForceMode.Impulse);
        }

        Invoke(nameof(EnableHoming), 0.2f);
    }

    private void EnableHoming() => _isHoming = true;

    void Update()
    {
        if (!_isHoming || _targetPlayer == null) return;

        // 1. Move towards player
        Vector3 playerCenter = _targetPlayer.position + Vector3.up; // Aim for chest height
        Vector3 direction = (playerCenter - transform.position).normalized;
        speed += Time.deltaTime * acceleration;

        // 2. Calculate Squiggly Offset
        // We use sine waves on the axes perpendicular to the flight path
        float time = (Time.time - _startTime) * frequency;
        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
        if (right.sqrMagnitude < 0.1f) right = Vector3.Cross(direction, Vector3.forward).normalized;
        Vector3 up = Vector3.Cross(direction, right).normalized;

        Vector3 wiggle = (right * Mathf.Sin(time) + up * Mathf.Cos(time)) * magnitude;

        // 3. Apply position
        transform.position += (direction * speed * Time.deltaTime) + (wiggle * Time.deltaTime);

        // Look at player
        transform.rotation = Quaternion.LookRotation(direction);

        // 4. Collision Check
        if (Vector3.Distance(transform.position, playerCenter) < reachDistance)
        {
            Collect();
        }
    }

    private void Collect()
    {
        var handler = _targetPlayer.GetComponent<PlayerGarbageHandler>();
        if (handler != null)
        {
            // This is where we feed the data back into the player's system
            handler.AddRefundedGarbage(_dataToRefund);
        }

        // Optional: Play a small "bloop" sound or VFX
        Destroy(gameObject);
    }
}