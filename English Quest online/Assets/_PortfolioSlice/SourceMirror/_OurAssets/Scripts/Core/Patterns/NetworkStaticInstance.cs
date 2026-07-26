using UnityEngine;
using Fusion;

/// <summary>
/// A static reference pattern for NetworkBehaviours that exist in a scene.
/// This does NOT persist between scenes (no DontDestroyOnLoad).
/// Useful for managers that are scene-specific and need Fusion networking.
/// </summary>
public abstract class NetworkStaticInstance<T> : NetworkBehaviour where T : NetworkBehaviour
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = (T)FindFirstObjectByType<T>(FindObjectsInactive.Include);
                if (_instance == null)
                {
                    AppLog.Error($"[NetworkStaticInstance] No instance of {typeof(T).Name} found in the scene. Please ensure one exists.");
                }
            }

            return _instance;
        }
    }

    // NetworkBehaviours can also use Awake for initial setup, but Spawned is for networking initialization.
    // Static reference should be set early in Awake.
    protected virtual void Awake()
    {
        if (_instance != null && _instance != (this as T))
        {
            AppLog.Warning($"[NetworkStaticInstance] Multiple instances of {typeof(T).Name} detected in the scene. Overwriting reference.");
        }
        _instance = this as T;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        if (_instance == this)
        {
            _instance = null;
        }
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
