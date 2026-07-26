using UnityEngine;
using TMPro;

[AddComponentMenu(QuestSystemComponentMenuPaths.UI + "/Quest Target UI")]
public class QuestTargetUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _displayText;

    public void DisplayText(string text)
    {
        if (_displayText != null)
        {
            _panel.SetActive(true);
            _displayText.text = text;
        }
        else
            AppLog.Warning("[QuestTargetUI] _displayText is not assigned.");
    }

    public void Hide()
    {
        if (_panel != null)
            _panel.SetActive(false);
    }
}
