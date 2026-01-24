using System;
using System.Collections;
using UnityEngine;

using Random = UnityEngine.Random; // Differentiate Randoms


public class TruckTurret : MonoBehaviour, IStatReceiver
{
    public TurretData turretData;

    [SerializeField] private StatController _stats;

    private float _currentDamage;

    [Header("Turret Components")]
    // The part that rotates horizontally (e.g., the base or the whole gun mount)
    public Transform turretPivot;
    // The point where the raycast/bullet originates
    public Transform muzzlePoint;
    public Transform ejectionPort;


    [Header("VFX")]
    public GameObject muzzleVFXPrefab;
    public GameObject hitVFXPrefab;
    public GameObject bulletTrailPrefab; 
    public float trailSpeed = 200f;


    [Header("Bullet Casing")]
    public GameObject casingPrefab;
    public float ejectForce = 15f;
    [Tooltip("Randomness applied to the ejection force (min/max range).")]
    public Vector2 forceRandomness = new Vector2(0.5f, 1.5f);
    [Tooltip("How much random spin (torque) is applied to the casing.")]
    public float maxRandomSpin = 10f;
    [Tooltip("How long before the casing is automatically destroyed.")]
    public float casingLifetime = 6f;

    [Header("Accuracy")]
    public float maxHitDeviation = 0.5f;

    private Transform currentTarget;
    private Collider currentTargetCollider;
    private float fireTimer;

    [Header("Turret Audio")]
    [SerializeField] SoundDef turretOneShot;


    [Header("Turret Stats")]
    public float _turretDamage;
    public float _turretFireRate;






    public static event Action<Vector3, Vector3> OnTurretShoot;





    private void Awake()
    {
        
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
  
        if (turretPivot == null)
        {
            Debug.LogWarning("Turret Pivot not assigned. Turret will attempt to rotate based on its own transform, but alignment may be imperfect.");
        }

        // Intitialize stats 
        if (_stats != null && turretData != null)
        {
            _stats.InitializeStat(StatType.Damage, turretData.damage);
            _stats.InitializeStat(StatType.FireRate, turretData.fireRate);
        }

        OnStatsRecalculated();



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
            // If the target is no longer valid, drop it and find a new one.
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
    
    //  Centralized logic to check if the current target is still a threat
    private bool IsTargetStillValid()
    {
        // 1. Safety check
        if (currentTarget == null)
        {
            Debug.LogWarning($"{gameObject.name} Validation check failed: currentTarget reference is null.");
            currentTargetCollider = null;
            return false;
        }
        
        // If the target has the GarbageItem component, it is officially dead and invalid.
        if (currentTarget.GetComponent<GarbageItem>() != null)
        {
            Debug.Log($"<color=red>TARGET DROPPED:</color> {currentTarget.name} is now a GarbageItem (DEAD).");
            currentTargetCollider = null;
            return false;
        }
        
        // Check for EnemyHealth status (If it's not garbage, but the script is disabled, it's also dead)
        EnemyHealth targetHealth = currentTarget.GetComponent<EnemyHealth>();
        
        if (targetHealth == null) 
        {
            Debug.Log($"<color=red>TARGET DROPPED:</color> {currentTarget.name} has no EnemyHealth component (invalid target type).");
            currentTargetCollider = null;
            return false;
        }
        
        if (!targetHealth.enabled) 
        {
            Debug.Log($"<color=red>TARGET DROPPED:</color> {currentTarget.name}'s EnemyHealth component is disabled (is dead).");
            currentTargetCollider = null;
            return false;
        }

        if (targetHealth.IsDead)
        {
            Debug.Log("It's Dead yo");
            currentTargetCollider = null;
            return false;
        }

        // Check if the target is out of range
        if (Vector3.Distance(transform.position, currentTarget.position) > turretData.targetRange)
        {
            Debug.Log($"<color=yellow>TARGET DROPPED:</color> {currentTarget.name} is out of range ({Vector3.Distance(transform.position, currentTarget.position):F1}m).");
            currentTargetCollider = null;
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

        if (currentTarget != null) 
        {
            currentTargetCollider = currentTarget.GetComponent<Collider>();
        }
    }

    private void RotateTowardsTarget()
    {
        if (currentTargetCollider == null) return;

        Transform pivot = turretPivot != null ? turretPivot : transform;

        Vector3 targetPoint = currentTargetCollider.bounds.center; 
        Vector3 direction = targetPoint - pivot.position; 

        direction.y = 0;

        Quaternion lookRotation = Quaternion.LookRotation(direction);

        pivot.rotation = Quaternion.Slerp(
            pivot.rotation,
            lookRotation,
            Time.deltaTime * turretData.rotationSpeed
        );
    }

    private void TryShoot()
    {
        float shotsPerSecond = _stats ? _stats.GetStat(StatType.FireRate) : turretData.fireRate;

        if (shotsPerSecond <= 0.001f) shotsPerSecond = 0.001f; // Prevent dividing by 0
        float timeBetweenShots = 1.0f / shotsPerSecond;

        if (fireTimer >= timeBetweenShots)
        {
            if (currentTargetCollider == null) return;

            Vector3 targetPoint = currentTargetCollider.bounds.center;
            Vector3 targetDir = (targetPoint - muzzlePoint.position).normalized; 
            float angle = Vector3.Angle(muzzlePoint.forward, targetDir);

           
            fireTimer = 0f;
            Debug.Log($"<color=cyan>{gameObject.name} firing at {currentTarget.name}. Angle difference: {angle:F2} degrees.</color>");

            PlayMuzzleFlash();                                                                      // 1. Play Muzzle VFX
            EjectCasing();                                                                         // 2. Eject Casing
            PerformHitscanShot(targetDir);                                                        // 3. Perform Hitscan
            SoundManager.Instance.Play(turretOneShot, transform.position);                       // 4. Play one shot sound





        }
    }

    public void OnStatsRecalculated()
    {
        // Get base damage from stats 
        float baseDmg = _stats != null ? _stats.GetStat(StatType.Damage) : turretData.damage;

        //  Get Global Multiplier
        float globalMult = _stats != null ? _stats.GetStat(StatType.GlobalDamageMultiplier) : 1.0f;

        // 3. Store Final
        _currentDamage = baseDmg * globalMult;

        Debug.Log($"Turret Damage Updated: {_currentDamage}");
    }

    private void PerformHitscanShot(Vector3 direction)
    {
        // Placeholder for visual effects (muzzle flash, sound)
        // StartCoroutine(HandleVisualEffects()); 

        RaycastHit hit;
        Vector3 trailEndPoint; 

        if (Physics.Raycast(muzzlePoint.position, direction, out hit, turretData.targetRange, turretData.enemyLayer))
        {
            trailEndPoint = hit.point;

            OnTurretShoot?.Invoke(muzzlePoint.position, hit.point); // Invoke Event


            Vector3 randomOffset = new Vector3(
                Random.Range(-maxHitDeviation, maxHitDeviation), // X-axis (side-to-side)
                Random.Range(-maxHitDeviation, maxHitDeviation), // Y-axis (up-down)
                0f 
            );

            Vector3 deviatedPoint = hit.point + hit.collider.transform.TransformDirection(randomOffset);

            // Hit VFX
            if (hitVFXPrefab != null)
            {
                hit = HitVFX(hit, deviatedPoint); // Your code
            }

            Debug.DrawRay(hit.point, (deviatedPoint - hit.point), Color.magenta, 0.5f);
            Debug.DrawRay(muzzlePoint.position, direction * hit.distance, Color.red, 0.5f);

            EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(_currentDamage, deviatedPoint);
                Debug.Log($"<color=green>HIT CONFIRMED:</color> {gameObject.name} hit <color=yellow>{hit.collider.name}</color> for <color=red>{_currentDamage} damage</color>. Hit deviated by up to {maxHitDeviation:F2}m.");
            }
            else
            {
                Debug.Log($"<color=orange>MISS/FRIENDLY FIRE:</color> Raycast hit {hit.collider.name}, but it has no EnemyHealth script.");
            }
        }
        else
        {
            
            trailEndPoint = muzzlePoint.position + direction * turretData.targetRange;


            // Your existing miss logic
            Debug.DrawRay(muzzlePoint.position, direction * turretData.targetRange, Color.yellow, 0.5f);
            Debug.Log($"<color=red>MISS:</color> Hitscan missed the target (possible line-of-sight block).");
        }

        if (bulletTrailPrefab != null && muzzlePoint != null)
        {
            StartCoroutine(SpawnTrail(trailEndPoint));
        }
    }



   
    
    private void OnDrawGizmosSelected()
    {
        if (turretData == null) return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f); // Light Blue, transparent
        Gizmos.DrawWireSphere(transform.position, turretData.targetRange);

        if (currentTarget != null && muzzlePoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(muzzlePoint.position, currentTarget.position);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentTarget.position, 0.5f);
        }
        else if (muzzlePoint != null)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawRay(muzzlePoint.position, muzzlePoint.forward * 5f);
        }
    }
    private RaycastHit HitVFX(RaycastHit hit, Vector3 deviatedPoint)
    {
        Quaternion impactRotation = Quaternion.LookRotation(hit.normal);

        Instantiate(
            hitVFXPrefab,
            deviatedPoint,
            impactRotation
        );
        return hit;
    }

    private void PlayMuzzleFlash()
    {
        if (muzzleVFXPrefab == null || muzzlePoint == null)
        {
            return;
        }

        GameObject muzzleFlash = Instantiate(
            muzzleVFXPrefab,
            muzzlePoint.position,
            muzzlePoint.rotation,
            muzzlePoint 
        );

        
        Destroy(muzzleFlash, 0.5f);
    }


    private IEnumerator SpawnTrail(Vector3 endPoint)
    {
        GameObject trailObject = Instantiate(bulletTrailPrefab, muzzlePoint.position, Quaternion.identity);

        float distance = Vector3.Distance(muzzlePoint.position, endPoint);
        float timeToTravel = distance / trailSpeed;
        float timer = 0f;

        while (timer < timeToTravel)
        {
            trailObject.transform.position = Vector3.Lerp(muzzlePoint.position, endPoint, timer / timeToTravel);

            timer += Time.deltaTime;
            yield return null; 
        }

        trailObject.transform.position = endPoint;

        Destroy(trailObject, 1f);
    }

    private void EjectCasing()
    {
        if (casingPrefab == null || ejectionPort == null)
        {
            Debug.LogError("Casing Prefab or Ejection Port is missing!");
            return;
        }

        GameObject casingObject = Instantiate(
            casingPrefab,
            ejectionPort.position,
            ejectionPort.rotation
        );

        Rigidbody casingRb = casingObject.GetComponent<Rigidbody>();
        if (casingRb == null)
        {
            Debug.LogError("Casing prefab must have a Rigidbody component!");
            Destroy(casingObject);
            return;
        }

        
        Vector3 baseEjectDirection = ejectionPort.right; 

       
        float randomMagnitude = Random.Range(forceRandomness.x, forceRandomness.y) * ejectForce;

       
        Vector3 randomizedDirection = baseEjectDirection + new Vector3(
            Random.Range(-0.1f, 0.1f),  
            Random.Range(0.2f, 0.5f),   
            Random.Range(-0.05f, 0.05f) 
        );
        randomizedDirection.Normalize(); // Keep the direction vector unit length

        casingRb.AddForce(randomizedDirection * randomMagnitude, ForceMode.Impulse);

        
        casingRb.AddTorque(
            Random.insideUnitSphere * maxRandomSpin,
            ForceMode.Impulse
        );

        Destroy(casingObject, casingLifetime);
    }

}
