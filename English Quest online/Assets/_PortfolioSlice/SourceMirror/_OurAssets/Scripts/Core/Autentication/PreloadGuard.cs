using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to the Camera and the EventSystem inside the PreLoad scene.
///
/// In production the PreLoad scene is ALWAYS scene 0 and those objects are
/// the only ones in existence � this script does nothing.
///
/// In the editor, EditorPreloadInjector loads PreLoad additively into a
/// mid-game scene that already has its own Camera / EventSystem.
/// This guard detects the duplicate and destroys itself, preventing:
///   � Two cameras rendering simultaneously
///   � "There can be only one active EventSystem" spam in the console
///
/// Setup: add this component to the Camera AND EventSystem GameObjects
/// inside PreLoad.unity � nothing else needed.
/// </summary>
public class PreloadGuard : MonoBehaviour
{
    private void Awake()
    {
        if (GetComponent<Camera>() != null)
        {
            if (FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length > 1)
            {
                Destroy(gameObject);
                return;
            }
        }

        if (GetComponent<EventSystem>() != null)
        {
            if (FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length > 1)
            {
                Destroy(gameObject);
                return;
            }
        }
    }
}
