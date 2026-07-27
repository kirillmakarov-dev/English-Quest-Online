using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityServiceLocator;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EnglishQuest.Tests.RunTime
{
    internal static class WorldTravelTestSupport
    {
        public const string PreLoadSceneName = "PreLoad";
        public const string OpenWorldSceneName = "OpenWorld";
        public const string TravelDestinationSceneName = "Lesson 1 Introduction";
        public const string TravelOriginNodeId = "open_world";
        public const string TravelDestinationNodeId = "lesson_1_intro";
        public const string TravelDestinationSpawnId = "lesson_1_intro";
        public const string OpenWorldSpawnId = "open_world";
        public const float BootstrapGraceSeconds = 3f;
        public const float TravelTimeoutSeconds = 120f;
        public const float HarnessTravelTimeoutSeconds = 90f;

        public static float ActiveTravelTimeoutSeconds =>
            IsHarnessMapActive() ? HarnessTravelTimeoutSeconds : TravelTimeoutSeconds;

        public static string ActiveOriginSceneName =>
            IsHarnessMapActive() ? FusionHarnessFactory.HarnessOriginSceneName : OpenWorldSceneName;

        public static string ActiveDestinationSceneName =>
            IsHarnessMapActive()
                ? FusionHarnessFactory.HarnessDestinationSceneName
                : TravelDestinationSceneName;

        public static string ActiveOriginNodeId =>
            IsHarnessMapActive() ? FusionHarnessFactory.HarnessOriginNodeId : TravelOriginNodeId;

        public static string ActiveDestinationNodeId =>
            IsHarnessMapActive() ? FusionHarnessFactory.HarnessDestinationNodeId : TravelDestinationNodeId;

        public static string ActiveDestinationSpawnId =>
            IsHarnessMapActive() ? FusionHarnessFactory.HarnessDestinationSpawnId : TravelDestinationSpawnId;

        public static string ActiveOriginSpawnId =>
            IsHarnessMapActive() ? FusionHarnessFactory.HarnessOriginSpawnId : OpenWorldSpawnId;

        private const string PrototypeNetworkStartPrefabPath =
            "Assets/_OurAssets/Art/Prefabs/Core/CoreGameObjects/Prototype Network Start.prefab";

        public static IEnumerator BootstrapOpenWorldMultiPeer()
        {
            TestDebugLog.Info("WorldTravel", "BootstrapOpenWorldMultiPeer starting.");
            yield return FusionMultiPeerTestSupport.ShutdownExistingRunners();
            yield return null;

            ServiceLocator.ResetForPlayModeTests();
            WorldMapDefinitionLoader.ResetForPlayModeTests();
            AssertTravelDestinationIsInBuild();
            yield return null;

            AsyncOperation preloadOperation =
                SceneManager.LoadSceneAsync(PreLoadSceneName, LoadSceneMode.Single);
            Assert.That(preloadOperation, Is.Not.Null,
                $"Scene '{PreLoadSceneName}' must be in the active Unity build profile scene list.");

            while (!preloadOperation.isDone)
                yield return null;

            TestDebugLog.LoadedScenes("WorldTravel", "After PreLoad");
            yield return null;

            AsyncOperation openWorldOperation =
                SceneManager.LoadSceneAsync(OpenWorldSceneName, LoadSceneMode.Single);
            Assert.That(openWorldOperation, Is.Not.Null,
                $"Scene '{OpenWorldSceneName}' must be in the active Unity build profile scene list.");

            while (!openWorldOperation.isDone)
                yield return null;

            TestDebugLog.LoadedScenes("WorldTravel", "After OpenWorld load");
            EnsureFusionMultiPeerBootstrap();
            yield return FusionMultiPeerTestSupport.WaitUntil(
                () => Object.FindFirstObjectByType<FusionBootstrap>(FindObjectsInactive.Include) != null,
                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,
                "Fusion bootstrap should be present after OpenWorld load.");

            WorldMapUI staleMapUi = FindWorldMapUi();
            if (staleMapUi != null && staleMapUi.IsOpen)
                staleMapUi.Close();

            TestDebugLog.AllRunners("WorldTravel");
            TestDebugLog.Info("WorldTravel", "BootstrapOpenWorldMultiPeer complete.");
        }

        public static IEnumerator BootstrapHarnessMultiPeer() =>
            FusionHarnessFactory.BootstrapCombatHarnessMultiPeer();

        public static bool IsHarnessMapActive() =>
            WorldMapDefinitionLoader.ResolveByMapId(FusionHarnessFactory.HarnessMapId) != null;

        public static WorldMapDefinitionSO ResolveActiveTravelMap()
        {
            WorldMapDefinitionSO harnessMap =
                WorldMapDefinitionLoader.ResolveByMapId(FusionHarnessFactory.HarnessMapId);
            if (harnessMap != null)
                return harnessMap;

            return WorldMapDefinitionLoader.LoadOpenWorldMap();
        }

        public static void EnsureFusionMultiPeerBootstrap()
        {
            if (Object.FindFirstObjectByType<FusionBootstrap>(FindObjectsInactive.Include) == null)
            {
#if UNITY_EDITOR
                GameObject prototypePrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(PrototypeNetworkStartPrefabPath);
                Assert.That(prototypePrefab, Is.Not.Null,
                    $"Prototype Network Start prefab missing at '{PrototypeNetworkStartPrefabPath}'.");
                Object.Instantiate(prototypePrefab);
#else
                Assert.Fail("FusionBootstrap is required for OpenWorld multi-peer travel tests.");
#endif
            }

#if UNITY_EDITOR
            if (Object.FindFirstObjectByType<FusionMultiPeerAutoStart>(FindObjectsInactive.Include) == null)
            {
                var autoStartObject = new GameObject("Fusion Multi-Peer Auto Start");
                autoStartObject.AddComponent<FusionMultiPeerAutoStart>();
            }
#endif
        }

        public static WorldMapNpcInteractable FindWorldMapNpcInOpenWorld() =>
            FindTravelNpc(OpenWorldSceneName);

        public static WorldMapNpcInteractable FindTravelNpc(string expectedSceneName)
        {
            WorldMapNpcInteractable[] npcs = Object.FindObjectsByType<WorldMapNpcInteractable>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < npcs.Length; i++)
            {
                WorldMapNpcInteractable npc = npcs[i];
                if (npc != null && npc.gameObject.scene.name == expectedSceneName)
                    return npc;
            }

            return npcs.Length > 0 ? npcs[0] : null;
        }

        public static WorldMapUI FindWorldMapUi() =>
            Object.FindFirstObjectByType<WorldMapUI>(FindObjectsInactive.Include);

        public static IEnumerator WaitForWorldTravelReady(float timeoutSeconds, string failureMessage)
        {
            yield return FusionMultiPeerTestSupport.WaitUntil(
                () => FindWorldMapUi() != null && Object.FindFirstObjectByType<WorldTravelService>() != null,
                timeoutSeconds,
                failureMessage);
        }

        public static IEnumerator WaitForRunnerSimulationScene(
            NetworkRunner runner,
            string sceneName,
            float timeoutSeconds,
            string failureMessage,
            string travelSpawnNodeId = null)
        {
            yield return FusionMultiPeerTestSupport.WaitUntil(
                () => IsRunnerInDestination(runner, sceneName, travelSpawnNodeId),
                timeoutSeconds,
                failureMessage);
        }

        public static bool IsRunnerInDestination(
            NetworkRunner runner,
            string sceneName,
            string travelSpawnNodeId = null)
        {
            if (!IsRunnerAlive(runner))
                return false;

            int buildIndex = ResolveGameplaySceneBuildIndex(sceneName);
            string scenePath = buildIndex >= 0 ? SceneUtility.GetScenePathByBuildIndex(buildIndex) : null;

            if (!SceneLoadWaitUtility.IsDestinationSceneLoaded(buildIndex, runner, sceneName, scenePath))
                return false;

            if (string.IsNullOrEmpty(travelSpawnNodeId))
                return true;

            NetworkObject player = runner.GetPlayerObject(runner.LocalPlayer);
            if (SceneLoadWaitUtility.TryResolveLoadedDestinationScene(
                    buildIndex,
                    sceneName,
                    scenePath,
                    runner,
                    out Scene destinationScene)
                && PlayerArrivalUtility.IsUsablePlayerObject(player, destinationScene))
            {
                return true;
            }

            return PlayerSpawnPoint.TryGetSpawnForNode(travelSpawnNodeId, destinationScene, out _, out _);
        }

        private static int ResolveGameplaySceneBuildIndex(string sceneName)
        {
            string[] paths =
            {
                $"Assets/_OurAssets/Scenes/{sceneName}.unity",
                $"Assets/_OurAssets/Scenes/Level 1/{sceneName}.unity",
                $"Assets/_OurAssets/Scenes/TestScenes/{sceneName}.unity"
            };

            for (int i = 0; i < paths.Length; i++)
            {
                int buildIndex = SceneUtility.GetBuildIndexByScenePath(paths[i]);
                if (buildIndex >= 0)
                    return buildIndex;
            }

            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.IsNullOrEmpty(scenePath))
                    continue;

                string sceneFileName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (sceneFileName == sceneName)
                    return i;
            }

            return -1;
        }

        public static void AssertTravelDestinationIsInBuild()
        {
            int buildIndex = ResolveGameplaySceneBuildIndex(TravelDestinationSceneName);
            Assert.That(buildIndex, Is.GreaterThanOrEqualTo(0),
                $"Travel destination scene '{TravelDestinationSceneName}' must be in the active build settings.");

            WorldMapDefinitionSO map = WorldMapDefinitionLoader.LoadOpenWorldMap();
            Assert.That(map, Is.Not.Null, "OpenWorldMap must be loadable for travel tests.");
            Assert.That(
                map.TryGetNode(TravelDestinationNodeId, out WorldMapNodeData destinationNode),
                Is.True,
                $"OpenWorldMap must contain destination node '{TravelDestinationNodeId}'.");
            Assert.That(
                map.TryGetRoute(TravelOriginNodeId, TravelDestinationNodeId, out _),
                Is.True,
                $"OpenWorldMap must contain a route from '{TravelOriginNodeId}' to '{TravelDestinationNodeId}'.");
            Assert.That(destinationNode.destinationType, Is.EqualTo(WorldMapDestinationType.LoadScene),
                "Travel destination node must require a scene load.");
        }

        public static IEnumerator WaitForAllRunnersInScene(
            System.Collections.Generic.List<NetworkRunner> runners,
            string sceneName,
            float timeoutSeconds,
            string failureMessage,
            string travelSpawnNodeId = null)
        {
            TestDebugLog.Info("WorldTravel",
                $"WaitForAllRunnersInScene target='{sceneName}', spawn='{travelSpawnNodeId}', runners={runners.Count}, timeout={timeoutSeconds}s.");
            TestDebugLog.LoadedScenes("WorldTravel", "Before wait");

            float elapsed = 0f;
            float logInterval = 5f;
            float nextLogAt = 0f;

            while (elapsed < timeoutSeconds)
            {
                bool allReady = true;
                for (int i = 0; i < runners.Count; i++)
                {
                    NetworkRunner runner = runners[i];
                    bool ready = IsRunnerInDestination(runner, sceneName, travelSpawnNodeId);
                    if (!ready)
                    {
                        allReady = false;
                        if (elapsed >= nextLogAt)
                        {
                            string currentScene = IsRunnerAlive(runner) && runner.SimulationUnityScene.IsValid()
                                ? runner.SimulationUnityScene.name
                                : "<invalid>";
                            TestDebugLog.Warn("WorldTravel",
                                $"Runner[{i}] '{FormatRunnerLabel(runner, i)}' still in '{currentScene}', waiting for '{sceneName}'.");
                        }
                    }
                }

                if (allReady)
                {
                    TestDebugLog.Info("WorldTravel",
                        $"All {runners.Count} runners reached '{sceneName}' after {elapsed:0.1}s.");
                    yield break;
                }

                if (elapsed >= nextLogAt)
                {
                    nextLogAt += logInterval;
                    TestDebugLog.AllRunners("WorldTravel");
                    TestDebugLog.LoadedScenes("WorldTravel", $"Waiting ({elapsed:0.0}s)");
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            TestDebugLog.AllRunners("WorldTravel");
            Assert.Fail($"{failureMessage} Loaded scenes: {TestDebugLog.FormatLoadedScenes()}");
        }

        public static bool IsTravelPresentationActive()
        {
            WorldMapUI mapUi = FindWorldMapUi();
            return mapUi != null && mapUi.IsOpen && mapUi.IsInTravelMode && mapUi.IsVehicleVisible;
        }

        public static IEnumerator WaitForWorldMapTravelPresentation(
            float timeoutSeconds,
            string failureMessage)
        {
            yield return FusionMultiPeerTestSupport.WaitUntil(
                IsTravelPresentationActive,
                timeoutSeconds,
                failureMessage);
        }

        public static IEnumerator TriggerPartyTravelFromLeader(
            NetworkRunner leaderRunner,
            WorldMapNpcInteractable npc)
        {
            PlayerInteraction leaderInteraction =
                FusionMultiPeerTestSupport.GetLocalPlayerInteraction(leaderRunner);

            Assert.That(leaderInteraction, Is.Not.Null, "Leader runner should have a local PlayerInteraction.");
            Assert.That(npc, Is.Not.Null, "OpenWorld should contain a WorldMapNpcInteractable.");

            WorldMapUI staleMapUi = FindWorldMapUi();
            if (staleMapUi != null && staleMapUi.IsOpen)
                staleMapUi.Close();

            WorldMapDefinitionSO map = ResolveActiveTravelMap();
            Assert.That(map, Is.Not.Null, "Travel map should be loadable for travel tests.");

            Assert.That(npc.Interact(leaderInteraction), Is.True,
                "Leader should open the world map via WorldMapNpcInteractable.");

            yield return FusionMultiPeerTestSupport.WaitUntil(
                () =>
                {
                    WorldMapUI ui = FindWorldMapUi();
                    return ui != null && ui.IsOpen;
                },
                5f,
                "World map should be open after NPC interaction.");

            WorldMapUI mapUi = FindWorldMapUi();
            Assert.That(mapUi, Is.Not.Null, "WorldMapUI should exist after NPC interaction.");

            Assert.That(
                ServiceLocator.For(leaderInteraction).TryGet(out IWorldTravelService travelService),
                Is.True,
                "Leader should resolve IWorldTravelService.");

            yield return RunTravelAsync(
                travelService.TravelAsync(
                    map,
                    ActiveOriginNodeId,
                    ActiveDestinationNodeId,
                    leaderInteraction),
                ActiveTravelTimeoutSeconds,
                "Leader party travel should complete within the timeout.");
        }

        public static IEnumerator AttemptTravelAsNonLeader(NetworkRunner memberRunner)
        {
            PlayerInteraction memberInteraction =
                FusionMultiPeerTestSupport.GetLocalPlayerInteraction(memberRunner);

            Assert.That(memberInteraction, Is.Not.Null, "Member runner should have a local PlayerInteraction.");

            WorldMapDefinitionSO map = ResolveActiveTravelMap();
            Assert.That(map, Is.Not.Null, "Travel map should be loadable for travel tests.");

            Assert.That(
                ServiceLocator.For(memberInteraction).TryGet(out IWorldTravelService travelService),
                Is.True,
                "Member should resolve IWorldTravelService.");

            Assert.That(travelService.CanTravel(memberInteraction), Is.False,
                "Non-leader party members should not be able to initiate travel.");

            yield return RunTravelAsync(
                travelService.TravelAsync(
                    map,
                    ActiveOriginNodeId,
                    ActiveDestinationNodeId,
                    memberInteraction),
                5f,
                "Non-leader travel attempt should finish quickly without blocking.");
        }

        public static IEnumerator WaitForCoSessionTravelIdle(
            System.Collections.Generic.List<NetworkRunner> runners,
            float timeoutSeconds,
            string failureMessage)
        {
            yield return FusionMultiPeerTestSupport.WaitUntil(
                () =>
                {
                    for (int i = 0; i < runners.Count; i++)
                    {
                        NetworkRunner runner = runners[i];
                        PlayerInteraction interaction =
                            FusionMultiPeerTestSupport.GetLocalPlayerInteraction(runner);
                        if (interaction == null)
                            continue;

                        if (ServiceLocator.For(interaction).TryGet(out IWorldTravelService travelService)
                            && travelService.IsTravelingFor(interaction))
                        {
                            return false;
                        }
                    }

                    return true;
                },
                timeoutSeconds,
                failureMessage);
        }

        internal static bool IsRunnerAlive(NetworkRunner runner) =>
            UnityLifetime.IsAlive(runner) && runner.IsRunning;

        internal static string FormatRunnerLabel(NetworkRunner runner, int index) =>
            IsRunnerAlive(runner) ? runner.name : $"<runner-{index}:unavailable>";

        public static IEnumerator RunTravelAsync(UniTask travelTask, float timeoutSeconds, string failureMessage)
        {
            TestDebugLog.Info("WorldTravel", $"RunTravelAsync starting, timeout={timeoutSeconds}s.");
            float elapsed = 0f;
            float logInterval = 3f;
            float nextLogAt = 0f;

            while (!travelTask.GetAwaiter().IsCompleted && elapsed < timeoutSeconds)
            {
                if (elapsed >= nextLogAt)
                {
                    nextLogAt += logInterval;
                    TestDebugLog.TravelPresentation("WorldTravel");
                    TestDebugLog.AllRunners("WorldTravel");
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            TestDebugLog.Info("WorldTravel",
                $"RunTravelAsync finished: completed={travelTask.GetAwaiter().IsCompleted}, elapsed={elapsed:0.1}s.");
            Assert.That(travelTask.GetAwaiter().IsCompleted, Is.True, failureMessage);

            if (travelTask.GetAwaiter().IsCompleted)
                travelTask.GetAwaiter().GetResult();
        }

        public static string GetRunnerSessionName(NetworkRunner runner)
        {
            if (!IsRunnerAlive(runner) || !runner.SessionInfo.IsValid)
                return string.Empty;

            return runner.SessionInfo.Name ?? string.Empty;
        }

        public static void AssertAllRunnersShareSession(
            IReadOnlyList<NetworkRunner> runners,
            string expectedSessionName = null,
            string expectedSessionPrefix = null)
        {
            Assert.That(runners, Is.Not.Null);
            Assert.That(runners.Count, Is.GreaterThan(0));

            string referenceSession = null;
            for (int i = 0; i < runners.Count; i++)
            {
                string sessionName = GetRunnerSessionName(runners[i]);
                Assert.That(sessionName, Is.Not.Empty,
                    $"Runner '{FormatRunnerLabel(runners[i], i)}' should be in an active Fusion session.");

                if (referenceSession == null)
                    referenceSession = sessionName;

                Assert.That(sessionName, Is.EqualTo(referenceSession),
                    $"Runner '{FormatRunnerLabel(runners[i], i)}' should share session '{referenceSession}'.");
            }

            if (!string.IsNullOrEmpty(expectedSessionName))
            {
                Assert.That(referenceSession, Is.EqualTo(expectedSessionName),
                    "All runners should be in the expected travel session.");
            }

            if (!string.IsNullOrEmpty(expectedSessionPrefix))
            {
                Assert.That(referenceSession, Does.StartWith(expectedSessionPrefix),
                    $"All runners should be in a session starting with '{expectedSessionPrefix}'.");
            }
        }

        public static int CountActivePlayers(NetworkRunner runner)
        {
            if (!IsRunnerAlive(runner))
                return 0;

            int count = 0;
            foreach (PlayerRef _ in runner.ActivePlayers)
                count++;

            return count;
        }

        public static void AssertSessionPlayerCountAtMost(
            IReadOnlyList<NetworkRunner> runners,
            int maxExpected)
        {
            for (int i = 0; i < runners.Count; i++)
            {
                int activePlayers = CountActivePlayers(runners[i]);
                Assert.That(activePlayers, Is.LessThanOrEqualTo(maxExpected),
                    $"Runner '{FormatRunnerLabel(runners[i], i)}' should not exceed session capacity ({maxExpected}).");
            }
        }
    }
}

