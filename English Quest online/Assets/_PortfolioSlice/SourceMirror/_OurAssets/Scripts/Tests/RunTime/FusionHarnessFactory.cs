using System.Collections;
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
    /// <summary>
    /// Builds a lightweight Fusion + world-travel harness inside CombatTest at runtime.
    /// </summary>
    internal static class FusionHarnessFactory
    {
        public const string HarnessMapAssetPath =
            "Assets/_OurAssets/Scripts/Tests/RunTime/Data/TravelHarnessMap.asset";
        public const string HarnessMapId = HarnessTravelSpawnUtility.HarnessMapId;
        public const string HarnessOriginSceneName = FusionMultiPeerTestSupport.CombatTestSceneName;
        public const string HarnessDestinationSceneName = HarnessTravelSpawnUtility.HarnessDestinationSceneName;
        public const string HarnessOriginNodeId = "combat_origin";
        public const string HarnessDestinationNodeId = "gameplay_dest";
        public const string HarnessOriginSpawnId = "combat_origin";
        public const string HarnessDestinationSpawnId = HarnessTravelSpawnUtility.HarnessDestinationSpawnId;
        public const string HarnessSharedDestinationNodeId = "shared_dest";
        public const string HarnessSharedSessionName = "HarnessSharedDest";

        private const string CoreScenePrefabPath = "Assets/_OurAssets/Art/Prefabs/Core/CoreScene.prefab";
        private const string PrototypeNetworkStartPrefabPath =
            "Assets/_OurAssets/Art/Prefabs/Core/CoreGameObjects/Prototype Network Start.prefab";
        private const string WorldMapCanvasPrefabPath =
            "Assets/_OurAssets/Art/Prefabs/UI/WorldMap/WorldMapCanvas.prefab";
        private const string TravelStationPrefabPath =
            "Assets/_OurAssets/Art/Prefabs/Levels/00 Open World/TravelStation.prefab";
        private const string LoadingCanvasPrefabPath =
            "Assets/_OurAssets/Art/Prefabs/UI/Core/LoadingCanvas.prefab";

        public static IEnumerator BootstrapCombatHarnessMultiPeer()
        {
            AssertHarnessDestinationIsInBuild();
            TestDebugLog.Info("Harness", "BootstrapCombatHarnessMultiPeer starting.");
            yield return FusionMultiPeerTestSupport.ShutdownExistingRunners();
            yield return null;

            ServiceLocator.ResetForPlayModeTests();
            WorldMapDefinitionLoader.ResetForPlayModeTests();
            yield return null;

            AsyncOperation loadOperation =
                SceneManager.LoadSceneAsync(HarnessOriginSceneName, LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null,
                $"Scene '{HarnessOriginSceneName}' must be in the active Unity build profile scene list.");

            while (!loadOperation.isDone)
                yield return null;

            TestDebugLog.LoadedScenes("Harness", "After CombatTest load");
            EnsureHarnessContent();
            InitializeHarnessMapRegistry();
            WorldTravelTestSupport.EnsureFusionMultiPeerBootstrap();
            yield return new WaitForSeconds(FusionMultiPeerTestSupport.AutoStartGraceSeconds);

            yield return FusionMultiPeerTestSupport.WaitUntil(
                () => Object.FindFirstObjectByType<WorldTravelService>() != null
                      && Object.FindFirstObjectByType<WorldMapUI>(FindObjectsInactive.Include) != null,
                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,
                "Harness world-travel services should be ready after bootstrap.");

            WorldMapUI staleMapUi = WorldTravelTestSupport.FindWorldMapUi();
            if (staleMapUi != null && staleMapUi.IsOpen)
                staleMapUi.Close();

            TestDebugLog.AllRunners("Harness");
            RegisterAllHarnessServicesGlobally();
            TestDebugLog.Info("Harness", "BootstrapCombatHarnessMultiPeer complete.");
        }

        public static WorldMapDefinitionSO LoadHarnessMap()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<WorldMapDefinitionSO>(HarnessMapAssetPath);
#else
            return null;
#endif
        }

        public static void InitializeHarnessMapRegistry()
        {
            WorldMapDefinitionSO map = LoadHarnessMap();
            Assert.That(map, Is.Not.Null, $"Travel harness map missing at '{HarnessMapAssetPath}'.");
            WorldMapDefinitionLoader.Initialize(map);
        }

        public static WorldMapNpcInteractable FindHarnessTravelNpc()
        {
            return WorldTravelTestSupport.FindTravelNpc(HarnessOriginSceneName);
        }

        public static void EnsureHarnessContentIfNeeded() => EnsureHarnessContent();

        public static void AssertHarnessDestinationIsInBuild()
        {
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(
                "Assets/_OurAssets/Scenes/TestScenes/GameplayTestScene.unity");
            Assert.That(buildIndex, Is.GreaterThanOrEqualTo(0),
                $"Harness destination scene '{HarnessDestinationSceneName}' must be in the active build settings.");

            WorldMapDefinitionSO map = LoadHarnessMap();
            Assert.That(map, Is.Not.Null, "TravelHarnessMap must be loadable for harness tests.");
            Assert.That(
                map.TryGetNode(HarnessDestinationNodeId, out WorldMapNodeData destinationNode),
                Is.True,
                $"TravelHarnessMap must contain destination node '{HarnessDestinationNodeId}'.");
            Assert.That(
                map.TryGetRoute(HarnessOriginNodeId, HarnessDestinationNodeId, out _),
                Is.True,
                $"TravelHarnessMap must contain a route from '{HarnessOriginNodeId}' to '{HarnessDestinationNodeId}'.");
            Assert.That(destinationNode.destinationType, Is.EqualTo(WorldMapDestinationType.LoadScene),
                "Harness destination node must require a scene load.");
        }

        private static void EnsureHarnessContent()
        {
#if UNITY_EDITOR
            Scene activeScene = SceneManager.GetActiveScene();

            if (Object.FindFirstObjectByType<FusionBootstrap>(FindObjectsInactive.Include) == null)
            {
                GameObject coreScenePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoreScenePrefabPath);
                Assert.That(coreScenePrefab, Is.Not.Null,
                    $"CoreScene prefab missing at '{CoreScenePrefabPath}'.");
                Object.Instantiate(coreScenePrefab);
            }

            if (Object.FindFirstObjectByType<WorldTravelService>(FindObjectsInactive.Include) == null)
            {
                var bootstrapRoot = new GameObject("World Travel Harness Bootstrap");
                Object.DontDestroyOnLoad(bootstrapRoot);

                WorldMapDefinitionSO harnessMap = LoadHarnessMap();
                GameObject canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldMapCanvasPrefabPath);
                Assert.That(canvasPrefab, Is.Not.Null,
                    $"WorldMapCanvas prefab missing at '{WorldMapCanvasPrefabPath}'.");

                GameObject canvas = Object.Instantiate(canvasPrefab, bootstrapRoot.transform);
                canvas.name = "WorldMapCanvas";

                bootstrapRoot.AddComponent<NetworkTravelCoordinator>();
                WorldTravelService travelService = bootstrapRoot.AddComponent<WorldTravelService>();

                if (Object.FindFirstObjectByType<GameNetworkManager>(FindObjectsInactive.Include) == null)
                {
                    GameNetworkManager networkManager = bootstrapRoot.AddComponent<GameNetworkManager>();
                    RegisterHarnessNetworkServiceGlobally(networkManager);
                }
                else
                {
                    RegisterHarnessNetworkServiceGlobally(
                        Object.FindFirstObjectByType<GameNetworkManager>(FindObjectsInactive.Include));
                }

                if (Object.FindFirstObjectByType<EnglishQuest.UI.Loading.LoadingScreenManager>(
                        FindObjectsInactive.Include) == null)
                {
                    GameObject loadingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LoadingCanvasPrefabPath);
                    if (loadingPrefab != null)
                    {
                        GameObject loadingInstance = Object.Instantiate(loadingPrefab, bootstrapRoot.transform);
                        RegisterHarnessLoadingServiceGlobally(
                            loadingInstance.GetComponentInChildren<EnglishQuest.UI.Loading.LoadingScreenManager>(
                                includeInactive: true));
                    }
                }
                else
                {
                    RegisterHarnessLoadingServiceGlobally(
                        Object.FindFirstObjectByType<EnglishQuest.UI.Loading.LoadingScreenManager>(
                            FindObjectsInactive.Include));
                }

                RegisterHarnessTravelServiceGlobally(travelService);

                if (harnessMap != null)
                    WorldMapDefinitionLoader.Initialize(harnessMap);
            }
            else
            {
                RegisterAllHarnessServicesGlobally();
            }

            if (FindHarnessTravelNpc() == null)
            {
                GameObject travelStationPrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(TravelStationPrefabPath);
                Assert.That(travelStationPrefab, Is.Not.Null,
                    $"TravelStation prefab missing at '{TravelStationPrefabPath}'.");
                Object.Instantiate(travelStationPrefab);
            }
#else
            Assert.Fail("Fusion harness bootstrap is editor-only.");
#endif
        }

        private static void RegisterAllHarnessServicesGlobally()
        {
            RegisterHarnessTravelServiceGlobally(
                Object.FindFirstObjectByType<WorldTravelService>(FindObjectsInactive.Include));
            RegisterHarnessNetworkServiceGlobally(
                Object.FindFirstObjectByType<GameNetworkManager>(FindObjectsInactive.Include));
            RegisterHarnessLoadingServiceGlobally(
                Object.FindFirstObjectByType<EnglishQuest.UI.Loading.LoadingScreenManager>(
                    FindObjectsInactive.Include));
        }

        private static void RegisterHarnessNetworkServiceGlobally(GameNetworkManager networkManager)
        {
            if (networkManager == null)
                return;

            ServiceLocator global = ServiceLocator.Global;
            if (global == null)
                return;

            if (!global.TryGet(out INetworkSessionService _))
                global.Register<INetworkSessionService>(networkManager);
        }

        private static void RegisterHarnessLoadingServiceGlobally(
            EnglishQuest.UI.Loading.LoadingScreenManager loadingScreen)
        {
            if (loadingScreen == null)
                return;

            ServiceLocator global = ServiceLocator.Global;
            if (global == null)
                return;

            if (!global.TryGet(out EnglishQuest.UI.Loading.ILoadingScreenService _))
                global.Register<EnglishQuest.UI.Loading.ILoadingScreenService>(loadingScreen);
        }

        private static void RegisterHarnessTravelServiceGlobally(WorldTravelService travelService)
        {
            if (travelService == null)
                return;

            ServiceLocator global = ServiceLocator.Global;
            if (global == null)
                return;

            if (!global.TryGet(out IWorldTravelService _))
                global.Register<IWorldTravelService>(travelService);
        }
    }
}

