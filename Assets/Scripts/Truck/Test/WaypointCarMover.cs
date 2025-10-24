using UnityEngine;

public class WaypointCarMover : MonoBehaviour
{
    // --- Public Variables to Set in Inspector ---

    [Tooltip("The speed at which the car will travel.")]
    public float moveSpeed = 10f;

    [Tooltip("How quickly the car rotates to face the waypoint (turning speed).")]
    public float turnSpeed = 5f;

    [Tooltip("Distance threshold to consider a waypoint reached.")]
    public float arrivalDistance = 1f;

    [Tooltip("Define your 4 waypoint positions here in order.")]
    public Vector3[] waypoints = new Vector3[4];

    // --- Private Variables ---

    private Rigidbody rb;
    private int currentWaypointIndex = 0;

    // --- Unity Methods ---

    void Start()
    {
        // Get the Rigidbody component
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody component not found on the car! Cannot move.");
            enabled = false; // Disable the script if no Rigidbody
        }

        if (waypoints.Length < 2)
        {
            Debug.LogError("You need at least 2 waypoints set in the Inspector!");
            enabled = false;
        }

        // Set the car to face the first waypoint immediately
        if (waypoints.Length > 0)
        {
            LookAtTarget(waypoints[currentWaypointIndex], true);
        }
    }

    void FixedUpdate()
    {
        // FixedUpdate is used for physics-based movement (Rigidbody)
        HandleMovement();
    }

    // --- Custom Methods ---

    void HandleMovement()
    {
        if (waypoints.Length == 0) return;

        Vector3 targetPosition = waypoints[currentWaypointIndex];
        Vector3 directionToTarget = (targetPosition - transform.position);

        // 1. Check if the waypoint is reached
        if (directionToTarget.magnitude < arrivalDistance)
        {
            // Move to the next waypoint in a loop
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            return; // Skip movement this frame to process the new target
        }

        // 2. Calculate and apply the physical turning (rotation)
        LookAtTarget(targetPosition, false);

        // 3. Apply continuous forward force/velocity
        // Move in the car's current forward direction
        Vector3 forwardMovement = transform.forward * moveSpeed;

        // Use velocity for smooth, constant motion
        rb.linearVelocity = new Vector3(forwardMovement.x, rb.linearVelocity.y, forwardMovement.z);
    }

    void LookAtTarget(Vector3 target, bool instant)
    {
        Vector3 direction = (target - transform.position).normalized;

        // We only care about rotation on the Y-axis (flat ground)
        Quaternion lookRotation = Quaternion.LookRotation(direction);

        if (instant)
        {
            rb.rotation = lookRotation;
        }
        else
        {
            // Smoothly rotate towards the target rotation
            Quaternion newRotation = Quaternion.Slerp(
                rb.rotation,
                lookRotation,
                Time.fixedDeltaTime * turnSpeed
            );
            rb.MoveRotation(newRotation); // Use MoveRotation for physics
        }
    }

    // --- Editor Helper (Visualizing Waypoints) ---

    void OnDrawGizmos()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            Gizmos.color = Color.yellow;
            // Draw lines between waypoints
            for (int i = 0; i < waypoints.Length; i++)
            {
                // Draw a sphere at each waypoint
                Gizmos.DrawWireSphere(waypoints[i], arrivalDistance);

                // Draw a line to the next waypoint
                if (i < waypoints.Length - 1)
                {
                    Gizmos.DrawLine(waypoints[i], waypoints[i + 1]);
                }
                else if (waypoints.Length > 1)
                {
                    // Draw a line back to the first point to complete the circuit
                    Gizmos.DrawLine(waypoints[i], waypoints[0]);
                }
            }
        }
    }
}