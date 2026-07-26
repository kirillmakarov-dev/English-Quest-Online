#if UNITY_EDITOR

using System.Collections;
using System.Reflection;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor-only helper that auto-starts Fusion Multi-Peer Shared clients from test scenes.
/// Place manually on test scenes that should auto-start (e.g. CombatTest) — not injected by
/// <see cref="FusionTestManager"/>, which only ensures <see cref="PlayerSpawnCoordinator"/>.
/// </summary>
[DefaultExecutionOrder(-50)]
public class FusionMultiPeerAutoStart : MonoBehaviour
{
    internal const string TestSessionName = "EditorMultiPeerTestSession";

    [SerializeField] private int _autoStartSharedClients = 2;
    [SerializeField] private float _startDelaySeconds = 0.5f;

    private static MethodInfo _startWithClientsMethod;
    private static PropertyInfo _currentStageProperty;

    private void Start()
    {
        if (_autoStartSharedClients <= 0)
            return;

        if (FusionTestManager.ShouldSuppressPrototypeBootstrap())
        {
            AppLog.Info("[FusionMultiPeerAutoStart] Active editor session detected. Skipping auto-start.");
            return;
        }

        if (NetworkProjectConfig.Global.PeerMode != NetworkProjectConfig.PeerModes.Multiple)
        {
            AppLog.Warning("[FusionMultiPeerAutoStart] PeerMode is not Multiple. Skipping auto-start.");
            return;
        }

        StartCoroutine(AutoStartRoutine());
    }

    private IEnumerator AutoStartRoutine()
    {
        yield return new WaitForSeconds(_startDelaySeconds);

        FusionBootstrap bootstrap = FindActiveSceneBootstrap();
        if (bootstrap == null)
        {
            AppLog.Warning("[FusionMultiPeerAutoStart] No FusionBootstrap found in scene.");
            yield break;
        }

        if (!TryResolveSceneRef(out SceneRef sceneRef))
        {
            AppLog.Warning("[FusionMultiPeerAutoStart] Active scene does not have a valid Fusion SceneRef.");
            yield break;
        }

        AppLog.Info($"[FusionMultiPeerAutoStart] Starting {_autoStartSharedClients} Shared client(s).");
        StartSharedClients(bootstrap, sceneRef, _autoStartSharedClients);
    }

    private static FusionBootstrap FindActiveSceneBootstrap()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        FusionBootstrap[] bootstraps = FindObjectsByType<FusionBootstrap>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < bootstraps.Length; i++)
        {
            FusionBootstrap bootstrap = bootstraps[i];
            if (bootstrap != null && bootstrap.gameObject.scene == activeScene)
                return bootstrap;
        }

        return bootstraps.Length > 0 ? bootstraps[0] : null;
    }

    private static bool TryResolveSceneRef(out SceneRef sceneRef)
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.buildIndex >= 0 && activeScene.buildIndex < SceneManager.sceneCountInBuildSettings)
        {
            sceneRef = SceneRef.FromIndex(activeScene.buildIndex);
            return true;
        }

        if (!string.IsNullOrEmpty(activeScene.path))
        {
            sceneRef = SceneRef.FromPath(activeScene.path);
            return true;
        }

        sceneRef = default;
        return false;
    }

    private static void StartSharedClients(FusionBootstrap bootstrap, SceneRef sceneRef, int clientCount)
    {
        if (!CanAutoStart(bootstrap))
            return;

        bootstrap.DefaultRoomName = TestSessionName;

        _startWithClientsMethod ??= typeof(FusionBootstrap).GetMethod(
            "StartWithClients",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (_startWithClientsMethod == null)
        {
            bootstrap.StartMultipleSharedClients(clientCount);
            return;
        }

        bootstrap.StartCoroutine(
            (IEnumerator)_startWithClientsMethod.Invoke(
                bootstrap,
                new object[] { GameMode.Shared, sceneRef, clientCount }));
    }

    private static bool CanAutoStart(FusionBootstrap bootstrap)
    {
        if (bootstrap.CurrentStage == FusionBootstrap.Stage.Disconnected)
            return true;

        foreach (NetworkRunner runner in NetworkRunner.Instances)
        {
            if (runner != null && runner.IsRunning)
                return false;
        }

        ResetBootstrapStage(bootstrap);
        return bootstrap.CurrentStage == FusionBootstrap.Stage.Disconnected;
    }

    internal static void ResetBootstrapStage(FusionBootstrap bootstrap)
    {
        if (bootstrap == null)
            return;

        _currentStageProperty ??= typeof(FusionBootstrap).GetProperty(
            nameof(FusionBootstrap.CurrentStage),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        _currentStageProperty?.SetValue(bootstrap, FusionBootstrap.Stage.Disconnected);
    }
}

#endif
