using UnityEngine;
using MoreMountains.Feedbacks;

/// <summary>
/// Handles visual feedback for a result popup (Correct / Try Again).
/// Each MMF_Player should contain a full fade-in → hold → fade-out sequence
/// configured in the Inspector via CanvasGroup alpha feedbacks.
/// Separated from logic to follow Clean Architecture.
/// </summary>
public class ResultPopupVisuals : MonoBehaviour
{
    [Header("Feedbacks")]
    [Tooltip("Feedback to play when the player answers correctly.")]
    public MMF_Player CorrectFeedback;

    [Tooltip("Feedback to play when the player should try again.")]
    public MMF_Player TryAgainFeedback;

    /// <summary>
    /// Plays the correct feedback (fade-in → hold → fade-out).
    /// </summary>
    public void ShowCorrect()
    {
        if (CorrectFeedback != null)
        {
            CorrectFeedback.PlayFeedbacks();
            AppLog.Info($"[{name}] Playing Correct Feedback");
        }
    }

    /// <summary>
    /// Plays the try-again feedback (fade-in → hold → fade-out).
    /// </summary>
    public void ShowTryAgain()
    {
        if (TryAgainFeedback != null)
        {
            TryAgainFeedback.PlayFeedbacks();
            AppLog.Info($"[{name}] Playing Try Again Feedback");
        }
    }
}
