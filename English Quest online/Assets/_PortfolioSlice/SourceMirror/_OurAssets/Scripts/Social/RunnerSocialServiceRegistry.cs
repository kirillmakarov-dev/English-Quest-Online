using System;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityServiceLocator;

/// <summary>
/// Per-<see cref="NetworkRunner"/> social services for Fusion Multi-Peer sessions.
/// Lives outside scene objects so CombatTest simulation peers can bind reliably.
/// </summary>
public static class RunnerSocialServiceRegistry
{
    private static readonly System.Collections.Generic.Dictionary<NetworkRunner, RunnerBundle> s_bundles = new();

    public static bool TryGet<T>(NetworkRunner runner, out T service) where T : class
    {
        service = null;
        if (runner == null || !s_bundles.TryGetValue(runner, out RunnerBundle bundle))
            return false;

        if (typeof(T) == typeof(IPartyService))
            service = bundle.Party as T;
        else if (typeof(T) == typeof(IPartyInviteService))
            service = bundle.Invites as T;
        else if (typeof(T) == typeof(ISessionPlayerRegistry))
            service = bundle.Registry as T;

        return service != null;
    }

    public static void Ensure(NetworkRunner runner, Scene lifecycleScene, PlayerInteraction interaction)
    {
        if (runner == null || !runner.IsRunning || interaction == null)
            return;

        Scene scene = ResolveLifecycleScene(runner, lifecycleScene);
        if (!scene.IsValid())
            return;

        if (s_bundles.TryGetValue(runner, out RunnerBundle existing) && existing.Context == interaction)
            return;

        if (existing != null)
            Teardown(runner);

        var bundle = new RunnerBundle
        {
            Session = new RunnerSessionService(),
            Party = new PartyService(),
            Invites = new PartyInviteService(),
            Registry = new SessionPlayerRegistry(),
            Context = interaction
        };

        bundle.Session.Bind(runner);
        bundle.Party.Bind(bundle.Session, interaction);
        bundle.Invites.Bind(bundle.Session, bundle.Party);
        bundle.Registry.Bind(bundle.Session, bundle.Party, interaction);

        ServiceLocator locator = ServiceLocator.ForSceneOf(interaction);
        locator.DeregisterIfRegistered<IPartyService>();
        locator.DeregisterIfRegistered<IPartyInviteService>();
        locator.DeregisterIfRegistered<ISessionPlayerRegistry>();
        locator.Register<IPartyService>(bundle.Party);
        locator.Register<IPartyInviteService>(bundle.Invites);
        locator.Register<ISessionPlayerRegistry>(bundle.Registry);

        s_bundles[runner] = bundle;
        TrackRunnerMemberships(runner, bundle);
    }

    private static void TrackRunnerMemberships(NetworkRunner runner, RunnerBundle bundle)
    {
        if (runner == null || !runner.IsRunning || bundle == null)
            return;

        foreach (PlayerRef player in FusionCoSessionRunners.EnumeratePlayers(runner))
        {
            PlayerPartyMembership membership = PartyMembershipUtility.GetMembership(runner, player);
            if (membership == null)
                continue;

            bundle.Party.TrackMembership(membership);
            bundle.Invites.TrackMembership(membership);
        }
    }

    public static void Teardown(NetworkRunner runner)
    {
        if (runner == null || !s_bundles.TryGetValue(runner, out RunnerBundle bundle))
            return;

        if (bundle.Context != null)
        {
            ServiceLocator.ForSceneOf(bundle.Context).DeregisterIfRegistered<IPartyService>();
            ServiceLocator.ForSceneOf(bundle.Context).DeregisterIfRegistered<IPartyInviteService>();
            ServiceLocator.ForSceneOf(bundle.Context).DeregisterIfRegistered<ISessionPlayerRegistry>();
        }

        s_bundles.Remove(runner);
    }

    public static void TeardownAll()
    {
        var runners = new System.Collections.Generic.List<NetworkRunner>(s_bundles.Keys);
        for (int i = 0; i < runners.Count; i++)
            Teardown(runners[i]);
    }

    public static void TrackMembership(NetworkRunner runner, PlayerPartyMembership membership)
    {
        if (runner == null || membership == null || !s_bundles.TryGetValue(runner, out RunnerBundle bundle))
            return;

        bundle.Party.TrackMembership(membership);
        bundle.Invites.TrackMembership(membership);
    }

    private static Scene ResolveLifecycleScene(NetworkRunner runner, Scene fallbackScene)
    {
        Scene simulationScene = runner.SimulationUnityScene;
        return simulationScene.IsValid() && simulationScene.isLoaded
            ? simulationScene
            : fallbackScene;
    }

    private sealed class RunnerBundle
    {
        public RunnerSessionService Session;
        public PartyService Party;
        public PartyInviteService Invites;
        public SessionPlayerRegistry Registry;
        public PlayerInteraction Context;
    }

    private sealed class RunnerSessionService : INetworkSessionService
    {
        public NetworkRunner Runner { get; private set; }

        public void Bind(NetworkRunner runner) => Runner = runner;

        public UniTask StartSharedSession(string sessionName, int sceneBuildIndex) =>
            UniTask.FromException(new NotSupportedException(nameof(StartSharedSession)));

        public UniTask StartSharedSession(string sessionName, int sceneBuildIndex, int maxPlayers, bool enableClientSessionCreation = true) =>
            UniTask.FromException(new NotSupportedException(nameof(StartSharedSession)));

        public UniTask StartSharedSession(NetworkSessionProfile profile) =>
            UniTask.FromException(new NotSupportedException(nameof(StartSharedSession)));

        public UniTask JoinSharedSession(string sessionName) =>
            UniTask.FromException(new NotSupportedException(nameof(JoinSharedSession)));

        public UniTask JoinOpenWorldAsync(string openWorldSceneName) =>
            UniTask.FromException(new NotSupportedException(nameof(JoinOpenWorldAsync)));

        public UniTask JoinOpenWorldAsync(NetworkSessionProfile profile) =>
            UniTask.FromException(new NotSupportedException(nameof(JoinOpenWorldAsync)));

        public UniTask Disconnect() =>
            UniTask.FromException(new NotSupportedException(nameof(Disconnect)));
    }
}
