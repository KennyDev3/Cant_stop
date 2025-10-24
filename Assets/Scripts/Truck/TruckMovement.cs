using UnityEngine;
using System.Collections.Generic;

public class TruckMovement : MonoBehaviour
{
    public Route route;
    public float normalMoveSpeed = 5f;
    public float safeZoneMoveSpeed = 2f;
    [Tooltip("How quickly the car rotates to face the waypoint (used as Slerp speed).")]
    public float rotationSpeed = 5f; // Used as the turnSpeed multiplier
    public float waypointReachedDistance = 0.5f; // Used as the arrival threshold
    // Removed: public float lookAheadDistance (as requested)

    // --- Private Variables ---
    private Rigidbody rb; // NEW: Needed for physics-correct movement
    private float currentMoveSpeed;
    private int currentWaypointIndex = 0;


    private void OnEnable()
    {
        SafeZone.OnTruckEnteredSafeZone += SlowDown;
        SafeZone.OnTruckExitedSafeZone += SpeedUp;
    }

    private void OnDisable()
    {
        SafeZone.OnTruckEnteredSafeZone -= SlowDown;
        SafeZone.OnTruckExitedSafeZone -= SpeedUp;
    }

    // --- Initialization ---

    private void Start()
    {
        // 1. Get the Rigidbody component (Crucial for physics)
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component not found on the truck! Cannot move.", this);
            this.enabled = false;
            return;
        }

        currentMoveSpeed = normalMoveSpeed;
        InitializeTruckPosition();
    }

    // --- Existing Initialization Logic (Adapted for Rigidbody) ---
    private void InitializeTruckPosition()
    {
        if (route == null || route.startPoint == null || route.waypoints.Count == 0)
        {
            Debug.LogError("Truck route or start point is not set up correctly!", this);
            this.enabled = false;
            return;
        }

        int startIndex = route.waypoints.IndexOf(route.startPoint);

        if (startIndex == -1)
        {
            Debug.LogError("The assigned 'startPoint' is not in the 'waypoints' list. Starting at index 0.", this);
            startIndex = 0; // Fallback to start at the first point
        }

        // Set the truck's position to the start point.
        transform.position = route.startPoint.position;

        // Set the truck's FIRST target to be the *next* waypoint after the start point.
        currentWaypointIndex = (startIndex + 1) % route.waypoints.Count;

        // Instantly rotate the truck to look at its actual first target using the Rigidbody.
        Transform firstTarget = route.waypoints[currentWaypointIndex];
        Vector3 initialDirection = (firstTarget.position - transform.position).normalized;

        if (initialDirection.sqrMagnitude > 0.001f)
        {
            // Use rb.rotation for instant, physics-aware rotation
            rb.rotation = Quaternion.LookRotation(initialDirection);
        }
    }

    // --- Physics-Based Movement Loop (New Logic, uses FixedUpdate) ---

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        if (route == null || route.waypoints.Count == 0) return;

        Transform targetWaypoint = route.waypoints[currentWaypointIndex];
        Vector3 targetPosition = targetWaypoint.position;
        Vector3 directionToTarget = (targetPosition - transform.position);

        if (directionToTarget.magnitude < waypointReachedDistance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % route.waypoints.Count;
            return; // Skip movement this frame to process the new target next frame
        }

        LookAtTarget(directionToTarget.normalized, false);

        Vector3 forwardMovement = transform.forward * currentMoveSpeed;

        rb.linearVelocity = new Vector3(forwardMovement.x, rb.linearVelocity.y, forwardMovement.z);
    }

    void LookAtTarget(Vector3 direction, bool instant)
    {
        Quaternion lookRotation = Quaternion.LookRotation(direction);

        if (instant)
        {
            rb.rotation = lookRotation;
        }
        else
        {
            Quaternion newRotation = Quaternion.Slerp(
                rb.rotation,
                lookRotation,
                Time.fixedDeltaTime * rotationSpeed
            );
            rb.MoveRotation(newRotation); // Use MoveRotation for physics integrity
        }
    }

    // --- SafeZone Event Callbacks (Kept as is) ---
    private void SlowDown()
    {
        // Keeping original condition check
        if (GameManager.Instance.CurrentRotation > 0)
        {
            currentMoveSpeed = safeZoneMoveSpeed;
        }
    }

    private void SpeedUp()
    {
        currentMoveSpeed = normalMoveSpeed;
    }


    void OnDrawGizmos()
    {
        // Gizmos are only drawn if the route and waypoints are properly assigned
        if (route != null && route.waypoints != null && route.waypoints.Count > 0)
        {
            Gizmos.color = Color.yellow;
            List<Transform> waypoints = route.waypoints;

            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] == null) continue;

                Vector3 currentPoint = waypoints[i].position;

                // Draw a sphere at each waypoint, using waypointReachedDistance
                Gizmos.DrawWireSphere(currentPoint, waypointReachedDistance);

                // Draw a line to the next waypoint
                if (i < waypoints.Count - 1 && waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(currentPoint, waypoints[i + 1].position);
                }
                else if (waypoints.Count > 1 && waypoints[0] != null)
                {
                    // Draw a line back to the first point to complete the circuit
                    Gizmos.DrawLine(currentPoint, waypoints[0].position);
                }
            }
        }
    }
}
