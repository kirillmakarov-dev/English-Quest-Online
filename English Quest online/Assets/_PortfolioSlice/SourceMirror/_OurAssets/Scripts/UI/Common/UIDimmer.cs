using UnityEngine;

public class UIDimmer : StaticInstance<UIDimmer>
{

    [Header("Optional Animations")]
    public MonoBehaviour FadeInFeedbacks;
    public MonoBehaviour FadeOutFeedbacks;

    private CanvasGroup canvasGroup;

    protected override void Awake()
    {
        base.Awake();
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false; // Off by default
    }

    public void Show()
    {
        canvasGroup.blocksRaycasts = true; // Block clicks behind the menu
        OptionalFeedbackPlayer.Stop(FadeOutFeedbacks);  // Stop hiding if it's currently hiding
        OptionalFeedbackPlayer.Play(FadeInFeedbacks);
    }

    public void Hide()
    {
        canvasGroup.blocksRaycasts = false; // Allow clicks again
        OptionalFeedbackPlayer.Stop(FadeInFeedbacks);
        OptionalFeedbackPlayer.Play(FadeOutFeedbacks);
    }
}

