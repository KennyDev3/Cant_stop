using UnityEngine;
using System.Collections;


public class TruckTurret : MonoBehaviour
{
    public TurretData turretData;

    [Header("Turret Components")]
    // The part that rotates horizontally (e.g., the base or the whole gun mount)
    public Transform turretPivot;
    // The point where the raycast/bullet originates
    public Transform muzzlePoint; 
    
    private Transform currentTarget;
    private float fireTimer;
    private float shootingAngle = 45f;
    public float _turretDamage;
    public float _turretFireRate;


    private void Awake()
    {
        _turretDamage = turretData.damage;
        _turretFireRate = turretData.fireRate;
        Debug.Log(_turretDamage);
        Debug.Log(_turretFireRate);
    }

    void Start()
    {
        if (turretData == null)
        {
            Debug.LogError("TurretData ScriptableObject is missing on " + gameObject.name);
            enabled = false;
            return;
        }
        if (muzzlePoint == null)
        {
            Debug.LogError("MuzzlePoint is not assigned on " + gameObject.name);
        }
        
        // CRUCIAL DEBUGGING HINT: If rotation is not working, 
        // ensure turretPivot and muzzlePoint are correctly assigned.
        if (turretPivot == null)
        {
            Debug.LogWarning("Turret Pivot not assigned. Turret will attempt to rotate based on its own transform, but alignment may be imperfect.");
        }

       
    }

    void Update()
    {
        fireTimer += Time.deltaTime;

        // 1. Check for Target
        if (currentTarget == null)
        {
            FindNewTarget();
        }

        // 2. If a target is found, lock and shoot
        if (currentTarget != null)
        {
            // NEW LOGICAL CHECK: If the target is no longer valid, drop it and find a new one.
            if (!IsTargetStillValid())
            {
                // The reason for target loss is now logged inside IsTargetStillValid()
                currentTarget = null;
                return;
            }

            RotateTowardsTarget();
            TryShoot();
        }
    }
    
    // NEW: Centralized logic to check if the current target is still a threat, with debug logging for failure.
    private bool IsTargetStillValid()
    {
        // 1. Safety check
        if (currentTarget == null)
        {
            Debug.LogWarning($"{gameObject.name} Validation check failed: currentTarget reference is null.");
            return false;
        }
        
        // 2. Check for Death/Garbage State (Priority Check)
        // If the target has the GarbageItem component, it is officially dead and invalid.
        if (currentTarget.GetComponent<GarbageItem>() != null)
        {
            Debug.Log($"<color=red>TARGET DROPPED:</color> {currentTarget.name} is now a GarbageItem (DEAD).");
            return false;
        }
        
        // 3. Check for EnemyHealth status (If it's not garbage, but the script is disabled, it's also dead)
        EnemyHealth targetHealth = currentTarget.GetComponent<EnemyHealth>();
        
        if (targetHealth == null) 
        {
            Debug.Log($"<color=red>TARGET DROPPED:</color> {currentTarget.name} has no EnemyHealth component (invalid target type).");
            return false;
        }
        
        if (!targetHealth.enabled) 
        {
            Debug.Log($"<color=red>TARGET DROPPED:</color> {currentTarget.name}'s EnemyHealth component is disabled (is dead).");
            return false;
        }
        
        // 4. Check if the target is out of range
        if (Vector3.Distance(transform.position, currentTarget.position) > turretData.targetRange)
        {
            Debug.Log($"<color=yellow>TARGET DROPPED:</color> {currentTarget.name} is out of range ({Vector3.Distance(transform.position, currentTarget.position):F1}m).");
            return false;
        }
        
        // If all checks pass, the target is still a valid, active enemy.
        return true;
    }

    private void FindNewTarget()
    {
        // Use OverlapSphere for fast initial checking of enemies in range
        Collider[] hits = Physics.OverlapSphere(transform.position, turretData.targetRange, turretData.enemyLayer);

        if (hits.Length == 0) return;

        // Find the closest enemy from the hits
        float shortestDistance = turretData.targetRange;
        Transform closestEnemy = null;

        foreach (Collider hit in hits)
        {
            // Filter: Must have EnemyHealth, must be enabled, and must NOT be converted to garbage yet.
            EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();
            bool isGarbage = hit.GetComponent<GarbageItem>() != null;

            if (enemyHealth != null && enemyHealth.enabled && !isGarbage)
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestEnemy = hit.transform;
                }
            }
        }
        currentTarget = closestEnemy;
    }

    private void RotateTowardsTarget()
    {
        // Use the turretPivot if assigned, otherwise use the turret's root transform
        Transform pivot = turretPivot != null ? turretPivot : transform;

        // Calculate the direction vector to the target
        Vector3 direction = currentTarget.position - pivot.position; 
        
        // Keep the rotation flat (only on the Y-axis) for the turret pivot
        direction.y = 0;

        // Create the rotation quaternion
        Quaternion lookRotation = Quaternion.LookRotation(direction);

        // Smoothly rotate the turret pivot towards the target (the "lock on")
        pivot.rotation = Quaternion.Slerp(
            pivot.rotation,
            lookRotation,
            Time.deltaTime * turretData.rotationSpeed
        );
    }

    private void TryShoot()
    {
        if (fireTimer >= _turretFireRate)
        {

            // Recalculate direction right before firing for precision
            Vector3 targetDir = (currentTarget.position - muzzlePoint.position).normalized;
            float angle = Vector3.Angle(muzzlePoint.forward, targetDir);

            // Using 10f tolerance, but you can adjust this via turretData if needed
            if (angle < shootingAngle) 
            {
                fireTimer = 0f;
                Debug.Log($"<color=cyan>{gameObject.name} firing at {currentTarget.name}. Angle difference: {angle:F2} degrees.</color>");
                PerformHitscanShot(targetDir);
            }
            else
            {
                // Debug to see how close the angle is
                Debug.Log($"<color=yellow>LOCKING:</color> Turret ready, but waiting for tighter lock. Angle: {angle:F2}");
            }
        }
    }
    
    private void PerformHitscanShot(Vector3 direction)
    {
        // Placeholder for visual effects (muzzle flash, sound)
        // StartCoroutine(HandleVisualEffects()); 

        RaycastHit hit;
        
        // Raycast from the muzzle point in the direction of the target
        if (Physics.Raycast(muzzlePoint.position, direction, out hit, turretData.targetRange, turretData.enemyLayer))
        {
            // Draw a successful hit line (visible in Scene view)
            Debug.DrawRay(muzzlePoint.position, direction * hit.distance, Color.red, 0.5f); 
            
            EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                // Damage the enemy immediately
                enemyHealth.TakeDamage(_turretDamage);
                Debug.Log($"<color=green>HIT CONFIRMED:</color> {gameObject.name} hit <color=yellow>{hit.collider.name}</color> for <color=red>{turretData.damage} damage</color>.");
            }
            else
            {
                 Debug.Log($"<color=orange>MISS/FRIENDLY FIRE:</color> Raycast hit {hit.collider.name}, but it has no EnemyHealth script.");
            }
            
            // Placeholder for impact effect at hit.point
        }
        else
        {
             // Draw a missed shot line (visible in Scene view)
             Debug.DrawRay(muzzlePoint.position, direction * turretData.targetRange, Color.yellow, 0.5f); 
             Debug.Log($"<color=red>MISS:</color> Hitscan missed the target (possible line-of-sight block).");
        }
    }

    public void IncreaseTurretDamage(float increaseAmount)
    {
        _turretDamage += increaseAmount;
      
    }

    public void IncreaseTurretFireRate(float increaseAmount)
    {
        _turretFireRate -= increaseAmount;
        Debug.Log(_turretFireRate);
    }

    // GIZMOS FOR VISUAL DEBUGGING
    private void OnDrawGizmosSelected()
    {
        if (turretData == null) return;

        // 1. Draw Turret Range (always visible when selected)
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f); // Light Blue, transparent
        Gizmos.DrawWireSphere(transform.position, turretData.targetRange);

        // 2. Draw Aim Line (only if a target is locked)
        if (currentTarget != null && muzzlePoint != null)
        {
            // Draw a solid yellow line from the muzzle to the target
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(muzzlePoint.position, currentTarget.position);

            // Draw a green sphere at the target's position
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentTarget.position, 0.5f);
        }
        else if (muzzlePoint != null)
        {
            // Draw the current forward direction when no target is locked
            Gizmos.color = Color.gray;
            Gizmos.DrawRay(muzzlePoint.position, muzzlePoint.forward * 5f);
        }
    }

    
}
