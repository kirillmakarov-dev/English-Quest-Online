using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityServiceLocator;

/// <summary>
/// Registers social services and tracks party/session state across all local
/// <see cref="NetworkRunner"/> instances (including Fusion Multi-Peer editor peers).
/// </summary>
public class SocialSystemsBootstrap : MonoBehaviour
{
    private readonly PartyService _partyService = new();
    private readonly PartyInviteService _inviteService = new();
    private readonly SessionPlayerRegistry _sessionRegistry = new();
    private readonly RunnerSessionService _runnerSessionService = new();

    private readonly HashSet<NetworkRunner> _registeredRunners = new();
    private readonly Dictionary<NetworkRunner, RunnerSocialServices> _runnerServices = new();
    private readonly Dictionary<Scene, ILocalPlayerReadiness> _sceneReadinessSubscriptions = new();
    private INetworkSessionService _sessionService;
    private bool _sessionServicesBound;
    private Coroutine _bindSessionCoroutine;

    /// <summary>
    /// Binds per-runner social services once the local player object exists in a Fusion scene.
    /// Called from <see cref="PlayerSpawnCoordinator"/> so Multi-Peer peers register reliably.
    /// </summary>
    public static void EnsureForLocalPlayer(NetworkRunner runner, Scene scene, NetworkObject playerObject)
    {
        if (runner == null || !runner.IsRunning || !scene.IsValid() || playerObject == null)
            return;

        PlayerInteraction interaction = PlayerRoot.Resolve<PlayerInteraction>(playerObject);
        if (interaction == null)
            return;

        Scene lifecycleScene = runner.SimulationUnityScene.IsValid() && runner.SimulationUnityScene.isLoaded
            ? runner.SimulationUnityScene
            : scene;

        RunnerSocialServiceRegistry.Ensure(runner, lifecycleScene, interaction);

        SocialSystemsBootstrap[] bootstraps = UnityEngine.Object.FindObjectsByType<SocialSystemsBootstrap>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < bootstraps.Length; i++)
            bootstraps[i]?.SyncRunnerMemberships(runner);
    }

    private void Awake()
    {
        ServiceLocator.For(this).Register<IPartyService>(_partyService);
        ServiceLocator.For(this).Register<IPartyInviteService>(_inviteService);
        ServiceLocator.For(this).Register<ISessionPlayerRegistry>(_sessionRegistry);
    }

    private void OnEnable()
    {
        SubscribeAllRunnerSceneReadiness();
        SubscribeToCallbackHub();
        TryRegisterReadyRunners();
    }

    private void OnDisable()
    {
        UnsubscribeAllSceneReadiness();
        UnsubscribeFromCallbackHub();
    }

    private void SubscribeToCallbackHub()
    {
        NetworkRunnerCallbackHub.Instance.PlayerJoined += HandlePlayerJoined;
        NetworkRunnerCallbackHub.Instance.PlayerLeft += HandlePlayerLeft;
        NetworkRunnerCallbackHub.Instance.SceneLoadDone += HandleSceneLoadDone;
        NetworkRunnerCallbackHub.Instance.ConnectedToServer += HandleConnectedToServer;
        NetworkRunnerCallbackHub.Instance.Shutdown += HandleShutdown;
    }

    private void UnsubscribeFromCallbackHub()
    {
        NetworkRunnerCallbackHub.Instance.PlayerJoined -= HandlePlayerJoined;
        NetworkRunnerCallbackHub.Instance.PlayerLeft -= HandlePlayerLeft;
        NetworkRunnerCallbackHub.Instance.SceneLoadDone -= HandleSceneLoadDone;
        NetworkRunnerCallbackHub.Instance.ConnectedToServer -= HandleConnectedToServer;
        NetworkRunnerCallbackHub.Instance.Shutdown -= HandleShutdown;
    }

    private void HandleLocalPlayerReady(LocalPlayerReadyArgs args)
    {
        if (!args.IsValid || !OwnsRunner(args.Runner))
            return;

        RegisterRunner(args.Runner);

        if (_runnerServices.TryGetValue(args.Runner, out RunnerSocialServices services) && services.Context != null)
            RegisterRunnerServicesOnContext(services.Context, services);
    }

    private void SubscribeAllRunnerSceneReadiness()
    {
        foreach (NetworkRunner runner in NetworkRunner.Instances)
            SubscribeRunnerSceneReadiness(runner);
    }

    private void SubscribeRunnerSceneReadiness(NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning)
            return;

        Scene simulationScene = runner.SimulationUnityScene;
        if (!simulationScene.IsValid()
            || _sceneReadinessSubscriptions.ContainsKey(simulationScene)
            || !ServiceLocator.TryGetForScene(simulationScene, out ILocalPlayerReadiness readiness))
        {
            return;
        }

        readiness.Ready += HandleLocalPlayerReady;
        _sceneReadinessSubscriptions[simulationScene] = readiness;
    }

    private void UnsubscribeAllSceneReadiness()
    {
        foreach (KeyValuePair<Scene, ILocalPlayerReadiness> entry in _sceneReadinessSubscriptions)
            entry.Value.Ready -= HandleLocalPlayerReady;

        _sceneReadinessSubscriptions.Clear();
    }

    private void TryRegisterReadyRunners()
    {
        foreach (NetworkRunner runner in NetworkRunner.Instances)
        {
            if (runner == null || !runner.IsRunning || !OwnsRunner(runner))
                continue;

            Scene simulationScene = runner.SimulationUnityScene;
            if (!simulationScene.IsValid() || !LocalPlayerReadiness.IsReady(runner, simulationScene))
                continue;

            RegisterRunner(runner);

            if (_runnerServices.TryGetValue(runner, out RunnerSocialServices services) && services.Context != null)
                RegisterRunnerServicesOnContext(services.Context, services);
        }
    }

    private void Start()
    {
        if (_bindSessionCoroutine != null)
            StopCoroutine(_bindSessionCoroutine);
        _bindSessionCoroutine = StartCoroutine(BindSessionServicesWhenReady());
    }

    private void OnDestroy()
    {
        if (_bindSessionCoroutine != null)
            StopCoroutine(_bindSessionCoroutine);

        UnregisterAllRunners();

        ServiceLocator.DeregisterFor<IPartyService>(this);
        ServiceLocator.DeregisterFor<IPartyInviteService>(this);
        ServiceLocator.DeregisterFor<ISessionPlayerRegistry>(this);
    }

    private IEnumerator BindSessionServicesWhenReady()
    {
        const int maxFrames = 300;
        for (int i = 0; i < maxFrames; i++)
        {
            if (TryBindSessionServices())
                yield break;

            yield return null;
        }
    }

    private bool TryBindSessionServices()
    {
        if (!TryResolveSceneRunner(out NetworkRunner sceneRunner))
        {
            if (_sessionServicesBound)
                return HasAnyRunningRunnerServices();

            if (!ServiceLocator.For(this).TryGet(out _sessionService))
                return false;
        }
        else
        {
            _runnerSessionService.Bind(sceneRunner);
            _sessionService = _runnerSessionService;
        }

        _partyService.Bind(_sessionService, this);
        _inviteService.Bind(_sessionService, _partyService);
        _sessionRegistry.Bind(_sessionService, _partyService, this);
        _sessionServicesBound = true;
        SyncRegisteredRunners();
        TryRegisterReadyRunners();
        return HasAnyRunningRunnerServices() || _sessionService?.Runner is { IsRunning: true };
    }

    private bool HasAnyRunningRunnerServices()
    {
        foreach (KeyValuePair<NetworkRunner, RunnerSocialServices> entry in _runnerServices)
        {
            if (entry.Key != null && entry.Key.IsRunning)
                return true;
        }

        return false;
    }

    private bool TryResolveSceneRunner(out NetworkRunner runner)
    {
        if (SceneNetworkRunner.TryGetForScene(gameObject.scene, out runner)
            && runner != null
            && runner.IsRunning)
        {
            return true;
        }

        Scene bootstrapScene = gameObject.scene;
        foreach (NetworkRunner instance in NetworkRunner.Instances)
        {
            if (instance == null || !instance.IsRunning)
                continue;

            Scene simulationScene = instance.SimulationUnityScene;
            if (simulationScene.IsValid() && simulationScene.handle == bootstrapScene.handle)
            {
                runner = instance;
                return true;
            }
        }

#if UNITY_EDITOR
        if (NetworkProjectConfig.Global.PeerMode == NetworkProjectConfig.PeerModes.Multiple)
        {
            foreach (NetworkRunner instance in NetworkRunner.Instances)
            {
                if (instance == null || !instance.IsRunning)
                    continue;

                runner = instance;
                return true;
            }
        }
#endif

        runner = null;
        return false;
    }

    private bool OwnsRunner(NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning)
            return false;

        if (TryResolveSceneRunner(out NetworkRunner sceneRunner))
            return FusionCoSessionRunners.AreInSameSession(sceneRunner, runner);

#if UNITY_EDITOR
        return NetworkProjectConfig.Global.PeerMode == NetworkProjectConfig.PeerModes.Multiple;
#else
        return false;
#endif
    }

    private void SyncRegisteredRunners()
    {
        if (!_sessionServicesBound)
            return;

        var activeRunners = new HashSet<NetworkRunner>();

        if (TryResolveSceneRunner(out NetworkRunner sceneRunner))
        {
            foreach (NetworkRunner runner in FusionCoSessionRunners.Enumerate(sceneRunner))
                activeRunners.Add(runner);
        }
        else
        {
            foreach (NetworkRunner runner in NetworkRunner.Instances)
            {
                if (runner != null && runner.IsRunning)
                    activeRunners.Add(runner);
            }
        }

        foreach (NetworkRunner runner in activeRunners)
            RegisterRunner(runner);

        var stoppedRunners = new List<NetworkRunner>();
        foreach (NetworkRunner runner in _registeredRunners)
        {
            if (runner == null || !runner.IsRunning)
                stoppedRunners.Add(runner);
        }

        foreach (NetworkRunner runner in stoppedRunners)
            UnregisterRunner(runner);
    }

    private void RegisterRunner(NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning)
            return;

        EnsureRunnerSocialServices(runner);

        if (!_registeredRunners.Contains(runner))
            _registeredRunners.Add(runner);

        TrackAllMemberships(runner);
        RefreshRegistryForRunner(runner);
    }

    private void UnregisterRunner(NetworkRunner runner)
    {
        if (runner == null || !_registeredRunners.Remove(runner))
            return;

        TeardownRunnerSocialServices(runner);
        RunnerSocialServiceRegistry.Teardown(runner);
        _sessionRegistry.Refresh();
    }

    private void UnregisterAllRunners()
    {
        var runners = new List<NetworkRunner>(_registeredRunners);
        for (int i = 0; i < runners.Count; i++)
            UnregisterRunner(runners[i]);

        _registeredRunners.Clear();
    }

    private void EnsureRunnerSocialServices(NetworkRunner runner)
    {
        EnsureRunnerSocialServices(runner, ResolveServiceContext(runner));
    }

    private void EnsureRunnerSocialServices(NetworkRunner runner, MonoBehaviour context)
    {
        if (runner == null || !runner.IsRunning || context == null)
            return;

        bool shouldRetrackMemberships = false;
        if (_runnerServices.TryGetValue(runner, out RunnerSocialServices existing))
        {
            if (existing.Context == context)
            {
                RegisterRunnerServicesOnContext(context, existing);
                return;
            }

            shouldRetrackMemberships = true;
            TeardownRunnerSocialServices(runner);
        }

        var services = new RunnerSocialServices();
        services.Session.Bind(runner);
        services.Party.Bind(services.Session, context);
        services.Invites.Bind(services.Session, services.Party);
        services.Registry.Bind(services.Session, services.Party, context);
        services.Context = context;

        RegisterRunnerServicesOnContext(context, services);

        _runnerServices[runner] = services;

        if (shouldRetrackMemberships)
            TrackAllMemberships(runner);
    }

    private static void RegisterRunnerServicesOnContext(MonoBehaviour context, RunnerSocialServices services)
    {
        ServiceLocator locator = ServiceLocator.ForSceneOf(context);
        locator.DeregisterIfRegistered<IPartyService>();
        locator.DeregisterIfRegistered<IPartyInviteService>();
        locator.DeregisterIfRegistered<ISessionPlayerRegistry>();
        locator.Register<IPartyService>(services.Party);
        locator.Register<IPartyInviteService>(services.Invites);
        locator.Register<ISessionPlayerRegistry>(services.Registry);
    }

    private void TeardownRunnerSocialServices(NetworkRunner runner)
    {
        if (!_runnerServices.TryGetValue(runner, out RunnerSocialServices services))
            return;

        if (services.Context != null)
        {
            ServiceLocator.ForSceneOf(services.Context).DeregisterIfRegistered<IPartyService>();
            ServiceLocator.ForSceneOf(services.Context).DeregisterIfRegistered<IPartyInviteService>();
            ServiceLocator.ForSceneOf(services.Context).DeregisterIfRegistered<ISessionPlayerRegistry>();
        }

        _runnerServices.Remove(runner);
    }

    private static MonoBehaviour ResolveServiceContext(NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning)
            return null;

        NetworkObject playerObject = runner.GetPlayerObject(runner.LocalPlayer);
        if (playerObject != null)
        {
            PlayerInteraction interaction = PlayerRoot.Resolve<PlayerInteraction>(playerObject);
            if (interaction != null)
                return interaction;
        }

        Scene scene = runner.SimulationUnityScene;
        if (!scene.IsValid())
            return null;

        var roots = new List<GameObject>();
        scene.GetRootGameObjects(roots);

        for (int i = 0; i < roots.Count; i++)
        {
            SocialPanelController panel = roots[i].GetComponentInChildren<SocialPanelController>(true);
            if (panel != null)
                return panel;
        }

        return null;
    }

    private void RefreshRegistryForRunner(NetworkRunner runner)
    {
        if (_runnerServices.TryGetValue(runner, out RunnerSocialServices services))
            services.Registry.Refresh();
        else
            _sessionRegistry.Refresh();
    }

    private void HandlePlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!OwnsRunner(runner))
            return;

        EnsureRunnerSocialServices(runner);
        TrackMembership(runner, player);
        RefreshRegistryForRunner(runner);
        NotifyPartyChanged(runner);
    }

    private void HandlePlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!OwnsRunner(runner))
            return;

        if (_runnerServices.TryGetValue(runner, out RunnerSocialServices services))
            services.Party.HandlePlayerLeft(player);
        else
            _partyService.HandlePlayerLeft(player);

        RefreshRegistryForRunner(runner);
    }

    private void HandleSceneLoadDone(NetworkRunner runner)
    {
        if (!OwnsRunner(runner))
            return;

        SubscribeRunnerSceneReadiness(runner);
        TryBindSessionServices();
        SyncRegisteredRunners();
        EnsureRunnerSocialServices(runner);
        TrackAllMemberships(runner);
        RefreshRegistryForRunner(runner);
        NotifyPartyChanged(runner);
    }

    private void TrackAllMemberships(NetworkRunner runner)
    {
        foreach (PlayerRef player in runner.ActivePlayers)
            TrackMembership(runner, player);
    }

    internal void SyncRunnerMemberships(NetworkRunner runner)
    {
        if (!OwnsRunner(runner))
            return;

        RegisterRunner(runner);
    }

    private void TrackMembership(NetworkRunner runner, PlayerRef player)
    {
        PlayerPartyMembership membership = PartyMembershipUtility.GetMembership(runner, player);
        if (membership == null)
            return;

        if (RunnerSocialServiceRegistry.TryGet(runner, out IPartyService _))
        {
            RunnerSocialServiceRegistry.TrackMembership(runner, membership);
            return;
        }

        if (_runnerServices.TryGetValue(runner, out RunnerSocialServices services))
        {
            services.Party.TrackMembership(membership);
            services.Invites.TrackMembership(membership);
            return;
        }

        _partyService.TrackMembership(membership);
        _inviteService.TrackMembership(membership);
    }

    private void NotifyPartyChanged(NetworkRunner runner)
    {
        if (_runnerServices.TryGetValue(runner, out RunnerSocialServices services))
            services.Party.NotifyChanged();
        else
            _partyService.NotifyChanged();
    }

    private void HandleConnectedToServer(NetworkRunner runner)
    {
        if (!OwnsRunner(runner))
            return;

        TryBindSessionServices();
        SyncRegisteredRunners();
    }

    private void HandleShutdown(NetworkRunner runner, ShutdownReason shutdownReason) => UnregisterRunner(runner);

    private sealed class RunnerSocialServices
    {
        public readonly RunnerSessionService Session = new();
        public readonly PartyService Party = new();
        public readonly PartyInviteService Invites = new();
        public readonly SessionPlayerRegistry Registry = new();
        public MonoBehaviour Context;
    }

    /// <summary>
    /// Minimal <see cref="INetworkSessionService"/> wrapper for Fusion Multi-Peer test scenes
    /// that do not register <see cref="GameNetworkManager"/>.
    /// </summary>
    private sealed class RunnerSessionService : INetworkSessionService
    {
        public NetworkRunner Runner { get; private set; }

        public void Bind(NetworkRunner runner) => Runner = runner;

        public UniTask StartSharedSession(string sessionName, int sceneBuildIndex) =>
            UniTask.FromException(new NotSupportedException(nameof(StartSharedSession)));

        public UniTask StartSharedSession(string sessionName, int sceneBuildIndex, int maxPlayers, bool enableClientSessionCreation = true) =>
            UniTask.FromException(new NotSupportedException(nameof(StartSharedSession)));

        public UniTask JoinSharedSession(string sessionName) =>
            UniTask.FromException(new NotSupportedException(nameof(JoinSharedSession)));

        public UniTask JoinOpenWorldAsync(string openWorldSceneName) =>
            UniTask.FromException(new NotSupportedException(nameof(JoinOpenWorldAsync)));

        public UniTask JoinOpenWorldAsync(NetworkSessionProfile profile) =>
            UniTask.FromException(new NotSupportedException(nameof(JoinOpenWorldAsync)));

        public UniTask StartSharedSession(NetworkSessionProfile profile) =>
            UniTask.FromException(new NotSupportedException(nameof(StartSharedSession)));

        public UniTask Disconnect() =>
            UniTask.FromException(new NotSupportedException(nameof(Disconnect)));
    }
}
