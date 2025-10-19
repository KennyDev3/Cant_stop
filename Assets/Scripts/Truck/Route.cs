// Modified Route.cs
using UnityEngine;
using System.Collections.Generic;

public class Route : MonoBehaviour
{
    
    public Transform startPoint; 
    
    // The list of waypoints the truck will follow in order.
    public List<Transform> waypoints = new List<Transform>();
}