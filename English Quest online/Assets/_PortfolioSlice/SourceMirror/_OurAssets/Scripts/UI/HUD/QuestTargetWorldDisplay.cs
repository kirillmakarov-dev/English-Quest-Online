using UnityEngine;
using TMPro;

[AddComponentMenu(QuestSystemComponentMenuPaths.UI + "/Quest Target World Display")]
public class QuestTargetWorldDisplay : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _displayText;
    [SerializeField] private RectTransform _displayRoot;

    public void DisplayText(string text)
    {
        if (_displayText != null)
        {
            _displayText.transform.parent.gameObject.SetActive(true);
            _displayText.text = text;
        }
        else
            AppLog.Warning("[QuestTargetWorldDisplay] _displayText is not assigned.");
    }

    public void Hide()
    {
        if (_displayText != null)
            _displayText.transform.parent.gameObject.SetActive(false);
    }
}


