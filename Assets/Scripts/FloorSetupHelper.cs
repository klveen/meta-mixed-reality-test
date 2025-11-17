using UnityEngine;

/// <summary>
/// Helper script to properly set up a fake floor plane for raycast placement.
/// Attach this to your floor plane GameObject.
/// 
/// IMPORTANT SETUP INSTRUCTIONS:
/// 1. Create a new GameObject (GameObject > 3D Object > Plane)
/// 2. Rename it to "FakeFloor"
/// 3. Position it at world coordinates (NOT under TrackingSpace!)
/// 4. Set Transform Position Y to desired floor height (e.g., -1.0 or -1.5)
/// 5. Make sure it has a Collider (Mesh Collider or Box Collider)
/// 6. Set it to a specific layer (optional but recommended):
///    - Edit > Project Settings > Tags and Layers
///    - Add a new layer called "Floor" (e.g., Layer 8)
///    - Assign the FakeFloor GameObject to this layer
/// 7. Attach this script to see position info
/// </summary>
public class FloorSetupHelper : MonoBehaviour
{
    [Header("Visualization")]
    [SerializeField] private bool drawGizmo = true;
    [SerializeField] private Color gizmoColor = Color.green;
    
    void OnDrawGizmos()
    {
        if (!drawGizmo) return;
        
        Gizmos.color = gizmoColor;
        
        // Draw a wire mesh of the floor bounds
        if (TryGetComponent<Collider>(out var collider))
        {
            // Draw in world space
            Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
            
            // Also draw a cross at the transform position for reference
            Gizmos.color = Color.yellow;
            Vector3 pos = transform.position;
            float size = 0.5f;
            Gizmos.DrawLine(pos + Vector3.left * size, pos + Vector3.right * size);
            Gizmos.DrawLine(pos + Vector3.forward * size, pos + Vector3.back * size);
        }
    }
    
    void Start()
    {
        // Validate setup
        Debug.Log($"[FloorSetupHelper] Floor '{gameObject.name}' world position: {transform.position}");
        Debug.Log($"[FloorSetupHelper] Floor Y-coordinate: {transform.position.y}");
        Debug.Log($"[FloorSetupHelper] Floor layer: {LayerMask.LayerToName(gameObject.layer)}");
        
        if (!TryGetComponent<Collider>(out _))
        {
            Debug.LogError($"[FloorSetupHelper] Floor '{gameObject.name}' has NO COLLIDER! Raycasts will not hit it!");
        }
        
        // Check if under TrackingSpace
        Transform parent = transform.parent;
        while (parent != null)
        {
            if (parent.name.Contains("TrackingSpace") || parent.name.Contains("CameraRig"))
            {
                Debug.LogWarning($"[FloorSetupHelper] WARNING: Floor is parented under '{parent.name}'! " +
                    "This will make it move with your headset. Move it to scene root for world-space floor.");
                break;
            }
            parent = parent.parent;
        }
    }
}
