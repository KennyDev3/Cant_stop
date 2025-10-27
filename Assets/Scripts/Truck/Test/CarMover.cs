using UnityEngine;
using System.Collections.Generic;

public class CarMover : MonoBehaviour
{
    // The list of waypoints (Transform components) that the car will follow.
    [Tooltip("Drag the waypoint GameObjects here in the order you want the truck to follow.")]
    public List<Transform> waypoints = new List<Transform>();

    // The speed at which the car will move between waypoints.
    [Tooltip("The movement speed of the truck in units per second.")]
    [Range(1f, 20f)] // A reasonable range for speed adjustment in the Inspector
    public float speed = 5f;

    // The minimum distance to a waypoint before moving to the next one.
    [Tooltip("How close the truck needs to be to a waypoint to consider it reached.")]
    public float waypointThreshold = 0.1f;

    // The index of the current waypoint the car is moving towards.
    private int currentWaypointIndex = 0;

    // Called every frame
    void Update()
    {
        // Only attempt to move if we have waypoints assigned
        if (waypoints.Count == 0)
        {
            Debug.LogWarning("CarMover: No waypoints assigned! Please add some to the list in the Inspector.");
            return;
        }

        // Get the position of the current target waypoint
        Vector3 targetPosition = waypoints[currentWaypointIndex].position;

        // Calculate the distance to the target waypoint
        float distanceToWaypoint = Vector3.Distance(transform.position, targetPosition);

        // Check if we have reached the current waypoint
        if (distanceToWaypoint < waypointThreshold)
        {
            // Move to the next waypoint
            currentWaypointIndex++;

            // If we've reached the end of the list, loop back to the first waypoint
            if (currentWaypointIndex >= waypoints.Count)
            {
                currentWaypointIndex = 0;
            }

            // Re-target the position to the new waypoint
            targetPosition = waypoints[currentWaypointIndex].position;
        }

        // 1. Calculate the direction to the target
        Vector3 direction = (targetPosition - transform.position).normalized;

        // 2. Move the car towards the target at a constant speed
        transform.position += direction * speed * Time.deltaTime;

        // 3. Optional: Rotate the truck to face the direction of movement
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            // Smoothly rotate towards the target rotation (optional: remove the * Time.deltaTime for instant turn)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    // Called in the editor to draw visual aids (Gizmos)
    private void OnDrawGizmos()
    {
        // Ensure we have at least one waypoint
        if (waypoints == null || waypoints.Count == 0)
        {
            return;
        }

        // Set the color for the lines connecting the waypoints
        Gizmos.color = Color.yellow;

        // Draw spheres at each waypoint and lines connecting them
        for (int i = 0; i < waypoints.Count; i++)
        {
            Transform currentWaypoint = waypoints[i];
            if (currentWaypoint != null)
            {
                // Draw a small sphere to represent the waypoint location
                Gizmos.DrawSphere(currentWaypoint.position, 0.3f);
            }

            // Draw a line segment to the next waypoint in the sequence
            if (i < waypoints.Count - 1 && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }

        // Draw a line from the last waypoint back to the first one to show the loop
        if (waypoints.Count > 1 && waypoints[waypoints.Count - 1] != null && waypoints[0] != null)
        {
            Gizmos.color = Color.red; // Use a different color for the loop
            Gizmos.DrawLine(waypoints[waypoints.Count - 1].position, waypoints[0].position);
        }
    }
}