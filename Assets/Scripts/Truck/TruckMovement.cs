using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SphereCollider))]
public class TruckMovement : MonoBehaviour
{
    [Header("Route & Speed")]
    public Route route;
    public float normalMoveSpeed = 5f;
    public float safeZoneMoveSpeed = 2f;
    public float rotationSpeed = 5f;

    [Header("Escort Feel & Behavior")]
    public Transform playerTransform;
    public float escortSafeZoneRadius = 8f;
    public float escortAcceleration = 1.5f;
    public float escortSafeZoneCenterOffset = 0f;

    private float _patrolMoveSpeed;
    private float _currentSpeed = 0f;
    
    private float _currentDistanceAlongRoute;
    private float[] _waypointDistances;
    private float _totalRouteLength;

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
        _patrolMoveSpeed = normalMoveSpeed;

        if (!ValidateRoute() || playerTransform == null)
        {
            Debug.LogError("Truck is missing a Route or Player Transform! Disabling component.", this);
            this.enabled = false;
            return;
        }

        PrecalculateRouteData();
        InitializeTruckPosition();
    }

    void Update()
    {
        if (playerTransform != null)
        {
            HandleEscortMovement();
        }
        else
        {
            HandlePatrolMovement();
        }
        UpdateTransform();
    }

    void HandlePatrolMovement()
    {
        _currentDistanceAlongRoute += _patrolMoveSpeed * Time.deltaTime;
        if (_currentDistanceAlongRoute > _totalRouteLength)
        {
            _currentDistanceAlongRoute -= _totalRouteLength;
        }
    }

    void HandleEscortMovement()
    {
        float playerDistanceOnRoute = GetDistanceAlongRoute(playerTransform.position);

        float directDistToPlayer = playerDistanceOnRoute - _currentDistanceAlongRoute;
        float wrapDistToPlayer = (_totalRouteLength - Mathf.Abs(directDistToPlayer)) * -Mathf.Sign(directDistToPlayer);
        float shortestDistToPlayer = Mathf.Abs(directDistToPlayer) < Mathf.Abs(wrapDistToPlayer) ? directDistToPlayer : wrapDistToPlayer;

        float safeZoneCenter = _currentDistanceAlongRoute + escortSafeZoneCenterOffset; // Use the offset
        float directDistToCenter = playerDistanceOnRoute - safeZoneCenter;
        float wrapDistToCenter = (_totalRouteLength - Mathf.Abs(directDistToCenter)) * -Mathf.Sign(directDistToCenter);
        float shortestDistToCenter = Mathf.Abs(directDistToCenter) < Mathf.Abs(wrapDistToCenter) ? directDistToCenter : wrapDistToCenter;


        float targetSpeed = 0f;
        int moveDirection = 1;

        if (Mathf.Abs(shortestDistToCenter) > escortSafeZoneRadius) // Check against the offset center
        {
            targetSpeed = normalMoveSpeed;
            moveDirection = System.Math.Sign(shortestDistToPlayer); // Move towards the player
        }

        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.deltaTime * escortAcceleration);
        _currentDistanceAlongRoute += _currentSpeed * moveDirection * Time.deltaTime;

        
        _currentDistanceAlongRoute = (_totalRouteLength + (_currentDistanceAlongRoute % _totalRouteLength)) % _totalRouteLength;
    }

    void UpdateTransform()
    {
        RoutePositionInfo newPositionInfo = GetRoutePositionInfoAtDistance(_currentDistanceAlongRoute);
        transform.position = newPositionInfo.Position;
        
        if (newPositionInfo.Direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(newPositionInfo.Direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    private bool ValidateRoute()
    {
        if (route == null || route.waypoints.Count < 2) return false;
        return true;
    }

    void PrecalculateRouteData()
    {
        _waypointDistances = new float[route.waypoints.Count];
        _totalRouteLength = 0f;
        for (int i = 0; i < route.waypoints.Count; i++)
        {
            _waypointDistances[i] = _totalRouteLength;
            Vector3 start = route.waypoints[i].position;
            Vector3 end = route.waypoints[(i + 1) % route.waypoints.Count].position;
            _totalRouteLength += Vector3.Distance(start, end);
        }
    }

    private void InitializeTruckPosition()
    {
        transform.position = route.startPoint != null ? route.startPoint.position : route.waypoints[0].position;
        _currentDistanceAlongRoute = GetDistanceAlongRoute(transform.position);
    }

    private struct RoutePositionInfo { public Vector3 Position; public Vector3 Direction; }

    private float GetDistanceAlongRoute(Vector3 worldPoint)
    {
        int closestSegmentIndex = 0;
        float minDistanceToSegment = float.MaxValue;
        Vector3 closestPointOnRoute = Vector3.zero;

        for (int i = 0; i < route.waypoints.Count; i++)
        {
            Vector3 start = route.waypoints[i].position;
            Vector3 end = route.waypoints[(i + 1) % route.waypoints.Count].position;
            Vector3 pointOnSegment = GetClosestPointOnLineSegment(start, end, worldPoint);
            float distance = Vector3.Distance(worldPoint, pointOnSegment);
            if (distance < minDistanceToSegment)
            {
                minDistanceToSegment = distance;
                closestSegmentIndex = i;
                closestPointOnRoute = pointOnSegment;
            }
        }
        
        Vector3 segmentStartPoint = route.waypoints[closestSegmentIndex].position;
        float distanceIntoSegment = Vector3.Distance(segmentStartPoint, closestPointOnRoute);
        return _waypointDistances[closestSegmentIndex] + distanceIntoSegment;
    }
    
    private RoutePositionInfo GetRoutePositionInfoAtDistance(float distance)
    {
        distance = (_totalRouteLength + (distance % _totalRouteLength)) % _totalRouteLength;
        
        for (int i = 0; i < route.waypoints.Count; i++)
        {
            int nextIndex = (i + 1) % route.waypoints.Count;
            if (_waypointDistances[i] <= distance && (nextIndex == 0 || _waypointDistances[nextIndex] > distance))
            {
                Vector3 start = route.waypoints[i].position;
                Vector3 end = route.waypoints[nextIndex].position;
                float distIntoSeg = distance - _waypointDistances[i];
                float segLen = Vector3.Distance(start, end);
                float t = segLen > 0 ? distIntoSeg / segLen : 0;
                return new RoutePositionInfo { Position = Vector3.Lerp(start, end, t), Direction = (end - start).normalized };
            }
        }
        return new RoutePositionInfo { Position = route.waypoints[0].position, Direction = (route.waypoints[1].position - route.waypoints[0].position).normalized };
    }
    
    private Vector3 GetClosestPointOnLineSegment(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ab = b - a;
        float magSqr = ab.sqrMagnitude;
        if (magSqr < 0.001f) return a;
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / magSqr);
        return a + ab * t;
    }
    
    private void SlowDown() { _patrolMoveSpeed = safeZoneMoveSpeed; }
    private void SpeedUp() { _patrolMoveSpeed = normalMoveSpeed; }

    void OnDrawGizmos() 
    {
        if (route != null && route.waypoints != null && route.waypoints.Count > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < route.waypoints.Count; i++)
            {
                if (route.waypoints[i] == null) continue;
                int nextIndex = (i + 1) % route.waypoints.Count;
                if (route.waypoints[nextIndex] == null) continue;

                Vector3 current = route.waypoints[i].position;
                Vector3 next = route.waypoints[nextIndex].position;
                Gizmos.DrawLine(current, next);
            }
        }

        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;

            float gizmoCenterDistance = _currentDistanceAlongRoute + escortSafeZoneCenterOffset;

            RoutePositionInfo p1 = GetRoutePositionInfoAtDistance(gizmoCenterDistance - escortSafeZoneRadius);
            RoutePositionInfo p2 = GetRoutePositionInfoAtDistance(gizmoCenterDistance + escortSafeZoneRadius);
            RoutePositionInfo pCenter = GetRoutePositionInfoAtDistance(gizmoCenterDistance); 

            Gizmos.DrawSphere(p1.Position, 0.75f); // 
            Gizmos.DrawSphere(p2.Position, 0.75f); // 
            Gizmos.DrawLine(p1.Position, p2.Position); 

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(pCenter.Position, 0.85f);
        }
    }
}