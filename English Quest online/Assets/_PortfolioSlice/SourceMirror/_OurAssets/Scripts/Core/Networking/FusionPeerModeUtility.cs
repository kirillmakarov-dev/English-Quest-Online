using Fusion;

/// <summary>
/// Fusion Multi-Peer is editor-only co-op testing: it merges levels into a per-runner simulation scene,
/// which breaks Unity's global <see cref="UnityEngine.RenderSettings"/> when Menu stays loaded.
/// Use <see cref="NetworkProjectConfig.PeerModes.Single"/> for Menu → gameplay and shipped builds.
/// </summary>
public static class FusionPeerModeUtility
{
    public static bool IsMultiplePeer =>
        NetworkProjectConfig.Global.PeerMode == NetworkProjectConfig.PeerModes.Multiple;
}
