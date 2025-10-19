// REVISED TruckMovement.cs
using UnityEngine;

public class TruckMovement : MonoBehaviour
{
    public Route route;
    public float normalMoveSpeed = 5f;
    public float safeZoneMoveSpeed = 2f;
    public float rotationSpeed = 5f;
    public float waypointReachedDistance = 0.5f;
    public float lookAheadDistance = 3f;

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

    private void Start()
    {
        currentMoveSpeed = normalMoveSpeed;
        InitializeTruckPosition();
    }

    // --- THIS METHOD HAS BEEN REVISED ---
    private void InitializeTruckPosition()
    {
        if (route == null || route.startPoint == null || route.waypoints.Count == 0)
        {
            Debug.LogError("Truck route or start point is not set up correctly!", this);
            this.enabled = false;
            return;
        }

        // 1. Find the index of the start point in the waypoints list.
        int startIndex = route.waypoints.IndexOf(route.startPoint);

        // 2. Error check: What if the assigned startPoint isn't in the waypoints list?
        if (startIndex == -1)
        {
            Debug.LogError("The assigned 'startPoint' is not in the 'waypoints' list. Please fix the Route setup.", this);
            this.enabled = false;
            return;
        }

        // 3. Set the truck's position to the start point.
        transform.position = route.startPoint.position;

        // 4. CRUCIAL FIX: Set the truck's FIRST target to be the *next* waypoint after the start point.
        // The modulo operator (%) ensures it loops back to 0 if the start point is the last in the list.
        currentWaypointIndex = (startIndex + 1) % route.waypoints.Count;

        // 5. Instantly rotate the truck to look at its actual first target.
        Transform firstTarget = route.waypoints[currentWaypointIndex];
        Vector3 initialDirection = (firstTarget.position - transform.position).normalized;
        if (initialDirection != Vector3.zero)
        {
            // Set rotation directly, no Slerp needed here.
            transform.rotation = Quaternion.LookRotation(initialDirection);
        }
    }

    void Update()
    {
        if (route == null || route.waypoints.Count == 0) return;

        Transform targetWaypoint = route.waypoints[currentWaypointIndex];
        
        Vector3 moveDirection = (targetWaypoint.position - transform.position).normalized;
        transform.position += moveDirection * currentMoveSpeed * Time.deltaTime;

        Vector3 lookTarget = GetLookAheadPoint();
        Vector3 lookDirection = (lookTarget - transform.position).normalized;
        
        lookDirection.y = 0;
        
        if (lookDirection != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, targetWaypoint.position) < waypointReachedDistance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % route.waypoints.Count;
        }
    }

    private Vector3 GetLookAheadPoint()
    {
        Transform currentTarget = route.waypoints[currentWaypointIndex];
        float distToCurrent = Vector3.Distance(transform.position, currentTarget.position);
        
        if (distToCurrent > lookAheadDistance)
        {
            return currentTarget.position;
        }
        
        int nextIndex = (currentWaypointIndex + 1) % route.waypoints.Count;
        Transform nextTarget = route.waypoints[nextIndex];
        
        float blend = 1f - (distToCurrent / lookAheadDistance);
        return Vector3.Lerp(currentTarget.position, nextTarget.position, blend);
    }

    private void SlowDown()
    {
        if (GameManager.Instance.CurrentRotation > 0)
        {
            currentMoveSpeed = safeZoneMoveSpeed;
        }
    }

    private void SpeedUp()
    {
        currentMoveSpeed = normalMoveSpeed;
    }
}