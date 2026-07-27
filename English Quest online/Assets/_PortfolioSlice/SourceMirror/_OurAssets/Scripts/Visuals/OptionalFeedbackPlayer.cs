using System.Reflection;
using UnityEngine;

public static class OptionalFeedbackPlayer
{
    public static void Play(MonoBehaviour feedback)
    {
        Invoke(feedback, "PlayFeedbacks");
    }

    public static void Stop(MonoBehaviour feedback)
    {
        Invoke(feedback, "StopFeedbacks");
    }

    private static void Invoke(MonoBehaviour feedback, string methodName)
    {
        if (feedback == null)
            return;

        MethodInfo method = feedback.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            System.Type.EmptyTypes,
            null);

        method?.Invoke(feedback, null);
    }
}
