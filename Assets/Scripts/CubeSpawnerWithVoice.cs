using UnityEngine;
using Meta.WitAi.Dictation;
using OVR; // from Meta XR SDK

public class CubeSpawnerWithVoice : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject cubePrefab; // Your cube prefab (text display removed)
    [SerializeField] private Transform rightHand;   // RightHandAnchor (also used as raycast origin)
    [SerializeField] private DictationService dictationService;

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask raycastLayerMask = -1; // Layers to hit with raycast
    [SerializeField] private float maxRayDistance = 10f;

    [Header("Laser Pointer (Optional)")]
    [SerializeField] private LineRenderer laserPointer; // Optional: shows ray while A is held
    [SerializeField] private Color laserColor = Color.red;
    [SerializeField] private float laserWidth = 0.005f;

    [Header("Debug")]
    [SerializeField] private bool showDebugRaycast = true; // Show raycast debug info in console
    [SerializeField] private bool drawDebugRay = true; // Draw ray in Scene view

    private GameObject currentCube;
    private bool isRecording = false;
    private string finalDictationText = "";
    private Vector3 lastHitPosition = Vector3.zero;
    private Vector3 lastHitNormal = Vector3.up;
    private float lastDebugLogTime = 0f;

    void Start()
    {
        // Validate required components
        // Debug.Log("[CubeSpawnerWithVoice] ===== SETUP VALIDATION =====");
        // Debug.Log($"[CubeSpawnerWithVoice] Cube Prefab: {(cubePrefab != null ? cubePrefab.name : "MISSING!")}");
        // Debug.Log($"[CubeSpawnerWithVoice] Right Hand: {(rightHand != null ? rightHand.name : "MISSING!")}");
        // Debug.Log($"[CubeSpawnerWithVoice] Dictation Service: {(dictationService != null ? "Assigned" : "MISSING!")}");
        // Debug.Log($"[CubeSpawnerWithVoice] Laser Pointer: {(laserPointer != null ? "Assigned" : "Not assigned (optional)")}");
        
        if (dictationService == null)
            Debug.LogError("[CubeSpawnerWithVoice] DictationService is NULL! Voice recognition will not work!");
        
        // Initialize laser pointer if provided
        if (laserPointer != null)
        {
            laserPointer.positionCount = 2; // CRITICAL: Set position count for line
            laserPointer.startWidth = laserWidth;
            laserPointer.endWidth = laserWidth;
            laserPointer.material = new Material(Shader.Find("Sprites/Default"));
            laserPointer.startColor = laserColor;
            laserPointer.endColor = laserColor;
            laserPointer.enabled = false;
            // Debug.Log($"[CubeSpawnerWithVoice] LineRenderer initialized: width={laserWidth}, color={laserColor}");
        }
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            StartRecording();

        if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch))
            StopRecording();

        if (isRecording && currentCube != null && rightHand != null)
        {
            // Perform raycast from right controller forward
            PerformRaycastPlacement();
        }
    }

    private void StartRecording()
    {
        if (currentCube != null) return;

        // Spawn cube at a default position (will move to raycast hit in Update)
        Vector3 spawnPosition = rightHand.position + rightHand.forward * 1.5f;
        currentCube = Instantiate(cubePrefab, spawnPosition, Quaternion.identity);
        // Debug.Log($"[CubeSpawnerWithVoice] Cube spawned at {spawnPosition}");
    
        if (dictationService != null)
        {
            // Debug.Log("[CubeSpawnerWithVoice] Subscribing to dictation events...");
            dictationService.DictationEvents.OnPartialTranscription.AddListener(OnPartial);
            dictationService.DictationEvents.OnFullTranscription.AddListener(OnFinal);
            
            // Debug.Log($"[CubeSpawnerWithVoice] Event listener counts - Partial: {dictationService.DictationEvents.OnPartialTranscription.GetPersistentEventCount()}, Full: {dictationService.DictationEvents.OnFullTranscription.GetPersistentEventCount()}");
            
            Debug.Log("[CubeSpawnerWithVoice] Activating dictation service...");
            dictationService.Activate();
            Debug.Log($"[CubeSpawnerWithVoice] Dictation active: {dictationService.Active}, MicActive: {dictationService.MicActive}");
        }
        else
        {
            Debug.LogError("[CubeSpawnerWithVoice] Cannot start dictation - DictationService is null!");
        }

        isRecording = true;
        finalDictationText = "";
        
        // Initialize last hit to initial spawn position
        lastHitPosition = spawnPosition;
        lastHitNormal = Vector3.up;

        // Enable laser pointer if available
        if (laserPointer != null)
        {
            laserPointer.gameObject.SetActive(true); // Ensure GameObject is active
            laserPointer.enabled = true;
            // Debug.Log($"[CubeSpawnerWithVoice] Laser pointer enabled. GameObject active: {laserPointer.gameObject.activeInHierarchy}");
        }
        // else
        // {
        //     Debug.LogWarning("[CubeSpawnerWithVoice] No laser pointer assigned - won't show ray visualization");
        // }
    }

    private void StopRecording()
    {
        // Debug.Log("[CubeSpawnerWithVoice] StopRecording called");
        if (!isRecording) return;

        if (dictationService != null)
        {
            // Try to get the final transcription before deactivating
            // if (dictationService.MicActive)
            // {
            //     Debug.Log("[CubeSpawnerWithVoice] Mic still active, waiting for final transcription...");
            // }
            
            dictationService.Deactivate();
            
            // Small delay to allow events to fire
            StartCoroutine(CleanupAfterDelay());
        }
        else
        {
            CompleteDictation();
        }
    }

    private System.Collections.IEnumerator CleanupAfterDelay()
    {
        // Wait a moment for dictation events to complete
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log($"[CubeSpawnerWithVoice] After delay, finalDictationText = '{finalDictationText}'");
        
        if (dictationService != null)
        {
            dictationService.DictationEvents.OnPartialTranscription.RemoveListener(OnPartial);
            dictationService.DictationEvents.OnFullTranscription.RemoveListener(OnFinal);
        }
        
        CompleteDictation();
    }

    private void CompleteDictation()
    {
        // Disable laser pointer
        if (laserPointer != null)
            laserPointer.enabled = false;

        // Print JSON output to console
        PrintDictationResult();

        // Let the cube "drop" (detach from hand). Do not change physics; prefab handles its own Rigidbody/physics.

        isRecording = false;
        currentCube = null;
    }

    private void OnPartial(string text)
    {
        // Store the latest partial text for final output
        finalDictationText = text;
        Debug.Log($"[CubeSpawnerWithVoice] OnPartial: '{text}'");

        // NO LONGER display text on cube during dictation
        // Text will only appear in the final JSON output
    }

    private void OnFinal(string text)
    {
        // Store the final dictation result
        finalDictationText = text;
        Debug.Log($"[CubeSpawnerWithVoice] OnFinal called with text: '{text}'");

        // NO LONGER display text on cube
        // Text will only appear in the final JSON output
    }

    /// <summary>
    /// Performs raycast from right controller and snaps cube to hit point
    /// </summary>
    private void PerformRaycastPlacement()
    {
        Ray ray = new Ray(rightHand.position, rightHand.forward);
        RaycastHit hit;

        bool didHit = Physics.Raycast(ray, out hit, maxRayDistance, raycastLayerMask);

        // Debug logging (throttled to once per second to avoid spam)
        if (showDebugRaycast && Time.time - lastDebugLogTime > 1f)
        {
            lastDebugLogTime = Time.time;
            if (didHit)
            {
                Debug.Log($"[Raycast] HIT: {hit.collider.name} at {hit.point}, distance: {hit.distance:F2}m, layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            }
            else
            {
                Debug.Log($"[Raycast] MISS: No hit within {maxRayDistance}m from {rightHand.position}. LayerMask: {raycastLayerMask.value}");
            }
        }

        if (didHit)
        {
            // Snap cube to hit point
            lastHitPosition = hit.point;
            lastHitNormal = hit.normal;
            currentCube.transform.position = hit.point;
            
            // Orient cube to stand upright on the surface
            currentCube.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

            // Update laser pointer to hit point
            if (laserPointer != null && laserPointer.enabled)
            {
                laserPointer.SetPosition(0, rightHand.position);
                laserPointer.SetPosition(1, hit.point);
            }
            
            // Draw debug ray in Scene view
            if (drawDebugRay)
            {
                Debug.DrawLine(rightHand.position, hit.point, Color.green);
                Debug.DrawRay(hit.point, hit.normal * 0.3f, Color.blue);
            }
        }
        else
        {
            // No hit: cube stays where it was (don't move it)
            // Only update the laser to show we're not hitting anything
            if (laserPointer != null && laserPointer.enabled)
            {
                laserPointer.SetPosition(0, rightHand.position);
                laserPointer.SetPosition(1, rightHand.position + rightHand.forward * maxRayDistance);
            }
            
            // Draw debug ray in Scene view (red for miss)
            if (drawDebugRay)
            {
                Debug.DrawRay(rightHand.position, rightHand.forward * maxRayDistance, Color.red);
            }
        }
    }

    /// <summary>
    /// Prints the final dictation result and last raycast hit point as JSON
    /// </summary>
    private void PrintDictationResult()
    {
        // Build JSON manually
        string json = string.Format(
            "{{\n  \"utterance\": \"{0}\",\n  \"point\": {{\n    \"position\": [{1}, {2}, {3}],\n    \"normal\": [{4}, {5}, {6}]\n  }}\n}}",
            finalDictationText,
            lastHitPosition.x.ToString("F3"),
            lastHitPosition.y.ToString("F3"),
            lastHitPosition.z.ToString("F3"),
            lastHitNormal.x.ToString("F3"),
            lastHitNormal.y.ToString("F3"),
            lastHitNormal.z.ToString("F3")
        );

        Debug.Log($"[CubeSpawnerWithVoice] Dictation Result:\n{json}");
    }
}
