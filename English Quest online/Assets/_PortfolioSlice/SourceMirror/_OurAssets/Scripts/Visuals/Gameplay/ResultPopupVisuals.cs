using UnityEngine;
/// <summary>
/// Handles visual feedback for a result popup (Correct / Try Again).
/// Each MonoBehaviour should contain a full fade-in â†’ hold â†’ fade-out sequence
/// configured in the Inspector via CanvasGroup alpha feedbacks.
/// Separated from logic to follow Clean Architecture.
/// </summary>
public class ResultPopupVisuals : MonoBehaviour
{
    [Header("Feedbacks")]
    [Tooltip("Feedback to play when the player answers correctly.")]
    public MonoBehaviour CorrectFeedback;

    [Tooltip("Feedback to play when the player should try again.")]
    public MonoBehaviour TryAgainFeedback;

    /// <summary>
    /// Plays the correct feedback (fade-in â†’ hold â†’ fade-out).
    /// </summary>
    public void ShowCorrect()
    {
        if (CorrectFeedback != null)
        {
            OptionalFeedbackPlayer.Play(CorrectFeedback);
            AppLog.Info($"[{name}] Playing Correct Feedback");
        }
    }

    /// <summary>
    /// Plays the try-again feedback (fade-in â†’ hold â†’ fade-out).
    /// </summary>
    public void ShowTryAgain()
    {
        if (TryAgainFeedback != null)
        {
            OptionalFeedbackPlayer.Play(TryAgainFeedback);
            AppLog.Info($"[{name}] Playing Try Again Feedback");
        }
    }
}

