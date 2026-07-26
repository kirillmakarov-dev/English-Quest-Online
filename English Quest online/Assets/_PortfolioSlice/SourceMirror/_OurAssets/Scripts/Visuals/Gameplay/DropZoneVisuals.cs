using UnityEngine;
using MoreMountains.Feedbacks;

/// <summary>
/// Handles visual feedback for a drop zone using MoreMountains Feel (MMF_Player).
/// Separated from logic to follow Clean Architecture.
/// </summary>
public class DropZoneVisuals : MonoBehaviour
{
    [Header("Feedbacks")]
    [Tooltip("Feedback to play when a correct match occurs.")]
    public MMF_Player SuccessFeedback;

    [Tooltip("Feedback to play when an incorrect match occurs.")]
    public MMF_Player FailureFeedback;

    /// <summary>
    /// Plays the success feedback.
    /// </summary>
    public void PlaySuccess()
    {
        if (SuccessFeedback != null)
        {
            SuccessFeedback.PlayFeedbacks();
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
            FailureFeedback.PlayFeedbacks();
            AppLog.Info($"[{name}] Playing Failure Feedback");
        }
    }
}
