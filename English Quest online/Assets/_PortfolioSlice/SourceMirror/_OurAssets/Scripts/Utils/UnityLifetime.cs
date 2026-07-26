using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helpers for safe teardown when Unity destroys objects in undefined order
/// (e.g. stopping Play Mode in the Editor).
/// </summary>
public static class UnityLifetime
{
    public static bool IsAlive(object obj) => obj != null && (obj is not Object unityObj || unityObj);

    public static bool IsApplicationQuitting { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() => IsApplicationQuitting = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        Application.quitting += () => IsApplicationQuitting = true;
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                IsApplicationQuitting = true;
        };
#endif
    }
}
