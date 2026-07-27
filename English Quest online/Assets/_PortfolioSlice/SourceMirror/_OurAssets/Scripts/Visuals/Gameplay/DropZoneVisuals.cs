using UnityEngine;
/// <summary>
/// Handles optional visual feedback for a drop zone.
/// Separated from logic to follow Clean Architecture.
/// </summary>
public class DropZoneVisuals : MonoBehaviour
{
    [Header("Feedbacks")]
    [Tooltip("Feedback to play when a correct match occurs.")]
    public MonoBehaviour SuccessFeedback;

    [Tooltip("Feedback to play when an incorrect match occurs.")]
    public MonoBehaviour FailureFeedback;

    /// <summary>
    /// Plays the success feedback.
    /// </summary>
    public void PlaySuccess()
    {
        if (SuccessFeedback != null)
        {
            OptionalFeedbackPlayer.Play(SuccessFeedback);
            AppLog.Info($"[{name}] Playing Success Feedback");
        }
    }

    /// <summary>
    /// Plays the failure feedback.
    /// </summary>
    public void PlayFailure()
    {
        if (FailureFeedback != null)
        {
            OptionalFeedbackPlayer.Play(FailureFeedback);
            AppLog.Info($"[{name}] Playing Failure Feedback");
        }
    }
}

