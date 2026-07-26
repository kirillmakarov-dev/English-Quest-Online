using UnityEngine;
using MoreMountains.Feedbacks; // Required for Feel

public class UIDimmer : StaticInstance<UIDimmer>
{

    [Header("Feel Animations")]
    public MMF_Player FadeInFeedbacks;
    public MMF_Player FadeOutFeedbacks;

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
        FadeOutFeedbacks.StopFeedbacks();  // Stop hiding if it's currently hiding
        FadeInFeedbacks.PlayFeedbacks();
    }

    public void Hide()
    {
        canvasGroup.blocksRaycasts = false; // Allow clicks again
        FadeInFeedbacks.StopFeedbacks();
        FadeOutFeedbacks.PlayFeedbacks();
    }
}