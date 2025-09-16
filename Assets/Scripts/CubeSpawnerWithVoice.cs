using UnityEngine;
using Meta.WitAi.Dictation;
using OVR; // from Meta XR SDK

public class CubeSpawnerWithVoice : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject cubePrefab; // Your [BuildingBlock] Cube prefab with TMP
    [SerializeField] private Transform rightHand;   // RightHandAnchor
    [SerializeField] private DictationService dictationService;

    private GameObject currentCube;
    private TextBoxController cubeText;
    private bool isRecording = false;

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            StartRecording();

        if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch))
            StopRecording();

        if (isRecording && currentCube != null && rightHand != null)
        {
            // Keep cube attached to hand while holding A
            currentCube.transform.position = rightHand.position + rightHand.forward * 0.25f;
            currentCube.transform.rotation = Quaternion.LookRotation(rightHand.forward, Vector3.up);
        }
    }

    private void StartRecording()
    {
        if (currentCube != null) return;

        currentCube = Instantiate(cubePrefab);
        Debug.Log($"[CubeSpawnerWithVoice] Instantiated prefab: {currentCube.name}");
        cubeText = currentCube.GetComponentInChildren<TextBoxController>();
        if (cubeText != null)
            Debug.Log("[CubeSpawnerWithVoice] Found TextBoxController in spawned cube.");
        else
            Debug.LogWarning("[CubeSpawnerWithVoice] No TextBoxController found in spawned cube!");

        if (dictationService != null)
        {
            dictationService.DictationEvents.OnPartialTranscription.AddListener(OnPartial);
            dictationService.DictationEvents.OnFullTranscription.AddListener(OnFinal);
            dictationService.Activate();
        }

        isRecording = true;
    }

    private void StopRecording()
    {
        Debug.Log("[CubeSpawnerWithVoice] StopRecording called");
        if (!isRecording) return;

        if (dictationService != null)
        {
            dictationService.Deactivate();
            dictationService.DictationEvents.OnPartialTranscription.RemoveListener(OnPartial);
            dictationService.DictationEvents.OnFullTranscription.RemoveListener(OnFinal);
        }

        // Let the cube "drop" (detach from hand). Do not change physics; prefab handles its own Rigidbody/physics.

        isRecording = false;
        currentCube = null;
        cubeText = null;
    }

    private void OnPartial(string text)
    {
        if (cubeText != null)
        {
            Debug.Log($"[CubeSpawnerWithVoice] OnPartial called, setting text: {text}");
            cubeText.SetText(text);
        }
        else
        {
            Debug.LogWarning("[CubeSpawnerWithVoice] OnPartial called but cubeText is null!");
        }
    }

    private void OnFinal(string text)
    {
        if (cubeText != null)
        {
            Debug.Log($"[CubeSpawnerWithVoice] OnFinal called, setting text: {text}");
            cubeText.SetText(text);
        }
        else
        {
            Debug.LogWarning("[CubeSpawnerWithVoice] OnFinal called but cubeText is null!");
        }
    }
}
