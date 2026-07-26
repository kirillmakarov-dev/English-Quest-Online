using UnityEngine;
using TMPro;

public class InteractionUI : MonoBehaviour, IInteractionUI
{
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private TextMeshProUGUI interactionText;
    [SerializeField] private TextMeshProUGUI buttonText;

    public void Show(string message , string Button = "E")
    {
        if (uiPanel != null) uiPanel.SetActive(true);
        if (interactionText != null) interactionText.text = message;
        if (buttonText != null) buttonText.text = Button;
    }

    public void Hide()
    {
        if (uiPanel != null) uiPanel.SetActive(false);
    }
}
