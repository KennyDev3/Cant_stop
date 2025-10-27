using UnityEngine;
using System.Collections.Generic;

public class TruckMovement : MonoBehaviour
{
    public Route route;
    public float normalMoveSpeed = 5f;
    public float safeZoneMoveSpeed = 2f;
    [Tooltip("How quickly the car rotates to face the waypoint (used as Slerp speed).")]
    public float rotationSpeed = 10f; // Matches the original script's rotation multiplier
    public float waypointReachedDistance = 0.1f; // Matches the original script's threshold

    // --- Private Variables ---
    private float currentMoveSpeed;
    private int currentWaypointIndex = 0;


    private void OnEnable()
    {
        // Assuming SafeZone and GameManager exist in your project
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
        // No Rigidbody check needed, but we keep initialization logic
        currentMoveSpeed = normalMoveSpeed;
        InitializeTruckPosition();
    }

    // --- Existing Initialization Logic (Using Transform) ---
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
            startIndex = 0;
        }

        // Set the truck's position to the start point.
        transform.position = route.startPoint.position;

        // Set the truck's FIRST target to be the *next* waypoint after the start point.
        currentWaypointIndex = (startIndex + 1) % route.waypoints.Count;

        // Instantly rotate the truck to look at its actual first target.
        Transform firstTarget = route.waypoints[currentWaypointIndex];
        Vector3 initialDirection = (firstTarget.position - transform.position).normalized;

        if (initialDirection.sqrMagnitude > 0.001f)
        {
            // Use transform.rotation for instant rotation
            transform.rotation = Quaternion.LookRotation(initialDirection);
        }
    }

    // --- Movement Loop (Switched back to Update) ---

    // Now uses Update() for frame-rate-dependent, direct Transform manipulation
    void Update()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        if (route == null || route.waypoints.Count == 0) return;

        // --- 1. Waypoint Reached Check ---
        Transform targetWaypoint = route.waypoints[currentWaypointIndex];
        Vector3 targetPosition = targetWaypoint.position;

        // Calculate the distance using the Transform's current position
        float distanceToWaypoint = Vector3.Distance(transform.position, targetPosition);

        if (distanceToWaypoint < waypointReachedDistance)
        {
            // Move to the next waypoint
            currentWaypointIndex++;

            // Loop back to the first waypoint
            if (currentWaypointIndex >= route.waypoints.Count)
            {
                currentWaypointIndex = 0;
            }

            // Re-target the position to the new waypoint (needed for calculation below)
            targetPosition = route.waypoints[currentWaypointIndex].position;
        }

        // --- 2. Calculate Direction and Movement (Direct Transform Move) ---
        Vector3 directionToTarget = (targetPosition - transform.position);

        // Use normalized direction for movement
        Vector3 direction = directionToTarget.normalized;

        // Move the truck towards the target at a constant speed
        // Uses Time.deltaTime because it's in Update()
        transform.position += direction * currentMoveSpeed * Time.deltaTime;

        // --- 3. Rotate the truck to face the direction of movement ---
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Smoothly rotate using transform.rotation
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );
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