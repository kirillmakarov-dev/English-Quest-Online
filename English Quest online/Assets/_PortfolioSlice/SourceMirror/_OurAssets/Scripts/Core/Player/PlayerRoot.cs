using Fusion;
using UnityEngine;

/// <summary>
/// Composition root on the player prefab. Resolves components across nested module prefabs.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerRoot : MonoBehaviour
{
    public Transform Transform => transform;

    public NetworkObject NetworkObject => _networkObject ??= GetComponent<NetworkObject>();

    public T GetPlayerComponent<T>() where T : Component =>
        GetComponent<T>() ?? GetComponentInChildren<T>(true);

    public static PlayerRoot Get(Component component) =>
        component != null ? component.GetComponentInParent<PlayerRoot>() : null;

    public static T Resolve<T>(Component component) where T : Component
    {
        PlayerRoot root = Get(component);
        return root != null ? root.GetPlayerComponent<T>() : component?.GetComponent<T>();
    }

    private NetworkObject _networkObject;
}

public static class PlayerTransformExtensions
{
    public static T GetPlayerComponent<T>(this Transform playerRoot) where T : Component
    {
        if (playerRoot == null) return null;

        if (playerRoot.TryGetComponent(out PlayerRoot root))
            return root.GetPlayerComponent<T>();

        return playerRoot.GetComponentInChildren<T>(true);
    }

    public static T GetPlayerComponent<T>(this GameObject playerRoot) where T : Component =>
        playerRoot != null ? playerRoot.transform.GetPlayerComponent<T>() : null;
}
