using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityServiceLocator;

public class LevelFinishTrigger : NetworkBehaviour
{
    [Header("Next Level Settings")]
    [Tooltip("The exact Path of the scene (e.g., Assets/Scenes/Level2.unity) or name if added to Build Settings.")]
    [SerializeField] private string _nextLevelScenePath;

#if UNITY_EDITOR
    [SerializeField] private UnityEditor.SceneAsset _sceneAsset;

    private void OnValidate()
    {
        if (_sceneAsset != null)
        {
            _nextLevelScenePath = UnityEditor.AssetDatabase.GetAssetPath(_sceneAsset);
        }
    }
#endif

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") && other.GetComponentInParent<NetworkObject>() == null)
            return;

        RPC_RequestTransition(_nextLevelScenePath);
    }

    // Any client can call this; it executes only on the StateAuthority (Master Client),
    // which is the only peer allowed to call runner.LoadScene in Shared Mode.
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestTransition(string scenePath)
    {
        AppLog.Info($"[LevelFinishTrigger] Requesting transition to {scenePath}...");
        if (ServiceLocator.For(this).TryGet<ILevelManager>(out var levelManager))
            levelManager.TransitionToSceneAsync(scenePath).Forget();
        else
            AppLog.Error("[LevelFinishTrigger] ILevelManager not found via ServiceLocator.");
    }
}
