using TMPro;
using UnityEngine;

public class UI_NameTagView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;

    private void Awake()
    {
        if (_nameText != null)
        {
            _nameText.fontStyle = FontStyles.Bold;
            _nameText.color = new Color(0.97f, 0.98f, 1f, 1f);
            _nameText.outlineWidth = 0.18f;
            _nameText.outlineColor = new Color(0.02f, 0.05f, 0.08f, 0.95f);
        }
    }

    public void SetName(string name)
    {
        if (_nameText != null)
        {
            _nameText.text = name;
            _nameText.isRightToLeftText = IsHebrew(name);
        }
    }

    private bool IsHebrew(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        foreach (var c in text)
        {
            if (c >= 0x0590 && c <= 0x05FF)
                return true;
        }
        return false;
    }
}
