using UnityEngine;

public class TruckFollower : MonoBehaviour
{
    public Transform[] waypoints;
    public float moveSpeed = 10f;
    public float rotationSpeed = 3f;
    public float stoppingDistance = 0.5f;
    private int currentWaypointIndex = 0;

    void Update()
    {
        if (waypoints.Length == 0) return;

        Transform targetWaypoint = waypoints[currentWaypointIndex];

        // 1. Movement
        float step = moveSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, step);

        // 2. Rotation (Smooth Turning)
        Vector3 direction = targetWaypoint.position - transform.position;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 3. Waypoint Progression (Circuit Logic)
        if (Vector3.Distance(transform.position, targetWaypoint.position) < stoppingDistance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length; // Loop back to 0
        }
    }
}