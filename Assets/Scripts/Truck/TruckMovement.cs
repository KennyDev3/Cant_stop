using UnityEngine;

public class TruckMovement : MonoBehaviour
{

    public Route route;
    public float moveSpeed = 5f;
    public float rotationSpeed = 5f;

    private int currentWaypointIndex = 0;
    
    void Update()
    {
        if (route == null || route.waypoints.Count == 0)
        {
            return; 
        }

        // Get the current target waypoint
        Transform targetWaypoint = route.waypoints[currentWaypointIndex];

       
        Vector3 moveDirection = (targetWaypoint.position - transform.position).normalized;
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        
        Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);

        // Check if we are close enough to the waypoint to move to the next one
        if (Vector3.Distance(transform.position, targetWaypoint.position) < 1f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % route.waypoints.Count;
        }
    }


}
