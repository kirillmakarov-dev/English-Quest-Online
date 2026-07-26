using UnityEngine;

/// <summary>
/// A static reference pattern for components that exist in a scene.
/// This does NOT persist between scenes (no DontDestroyOnLoad).
/// It is useful for managers that are scene-specific.
/// </summary>
public abstract class StaticInstance<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static bool HasInstance => _instance != null;

    public static T Instance
    {
        get
        {
                if (_instance == null)
                {
                    _instance = (T)FindFirstObjectByType<T>(FindObjectsInactive.Include);
                    if (_instance == null)
                    {
                        // add here if application is quitting to avoid false error logs during shutdown
                        if (Application.isPlaying)
                        {
                            AppLog.Error($"[StaticInstance] No instance of {typeof(T).Name} found in the scene. Please ensure one exists.");
                        }
                    }
                }

                return _instance;
            }
    }
    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            AppLog.Warning($"[StaticInstance] Multiple instances of {typeof(T).Name} detected in the scene. Overwriting reference.");
        }
        _instance = this as T;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
