using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu(QuestSystemComponentMenuPaths.UI + "/Quest Selection Entry View")]
public class QuestSelectionEntryView : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image background;
    [SerializeField] private Color normalColor = new(0.94f, 0.94f, 0.96f, 1f);
    [SerializeField] private Color selectedColor = new(0.35f, 0.62f, 0.98f, 0.45f);

    public Button Button => GetComponent<Button>();

    public void SetLabel(string text)
    {
        if (label == null)
            return;

        label.text = text;
        RtlDetector.Apply(label, text);
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
            background.color = selected ? selectedColor : normalColor;
    }
}
