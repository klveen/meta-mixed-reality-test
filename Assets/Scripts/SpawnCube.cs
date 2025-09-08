using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns a cube in front of the right hand when the A button is pressed.
/// </summary>
public class SpawnCubeOnA : MonoBehaviour
{
    [Header("References")]
    public Transform rightHandAnchor;     // Drag OVRRig → TrackingSpace → RightHandAnchor here
    public GameObject cubePrefab;         // Drag [BuildingBlock] Cube prefab here

    [Header("Spawn Settings")]
    public float forwardOffset = 0.3f;    // Distance in front of the hand
    public float upwardOffset = 0.05f;    // Optional: raise it a bit so it’s not inside your hand

    void Update()
    {
    // B button = OVRInput.Button.Two on the right Touch controller
    if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            if (cubePrefab != null && rightHandAnchor != null)
            {
                Vector3 spawnPos = rightHandAnchor.position
                                 + rightHandAnchor.forward * forwardOffset
                                 + rightHandAnchor.up * upwardOffset;

                Quaternion spawnRot = rightHandAnchor.rotation;

                Instantiate(cubePrefab, spawnPos, spawnRot);
            }
        }
    }
}

