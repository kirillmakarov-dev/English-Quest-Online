using TMPro;
using UnityEngine;

public class DoorWordLabel : MonoBehaviour
{
    [SerializeField] private string word;
    [SerializeField] private TextMeshProUGUI wordText;

    private void Awake()
    {
        ApplyWord();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyWord();
    }
#endif

    private void ApplyWord()
    {
        if (wordText != null)
            wordText.text = word;
    }
}