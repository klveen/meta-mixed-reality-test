using TMPro;
using UnityEngine;

public class TextBoxController : MonoBehaviour
{
    [SerializeField] private TMP_Text textMesh;

    public void SetText(string text)
    {
        if (textMesh != null)
            textMesh.text = text;
    }
}
