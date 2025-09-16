using TMPro;
using UnityEngine;


public class TextBoxController : MonoBehaviour
{
    [SerializeField] private TMP_Text textMesh;

    void Awake()
    {
        Debug.Log($"[TextBoxController] Awake on {gameObject.name}, textMesh assigned: {textMesh != null}");
    }

    void OnEnable()
    {
        Debug.Log($"[TextBoxController] OnEnable on {gameObject.name}, textMesh assigned: {textMesh != null}");
    }

    public void SetText(string text)
    {
        if (textMesh != null)
        {
            Debug.Log($"[TextBoxController] Setting text to: '{text}'");
            textMesh.text = text;
        }
        else
        {
            Debug.LogWarning("[TextBoxController] textMesh is not assigned!");
        }
    }
}
