using System.Collections;

using System.Collections.Generic;

using EnglishQuest.Tests;

using Fusion;

using NUnit.Framework;

using UnityEngine;

using UnityEngine.SceneManagement;

using UnityEngine.TestTools;

using UnityServiceLocator;



namespace EnglishQuest.Tests.RunTime

{

    /// <summary>

    /// Play Mode tests for world-travel bootstrap prerequisites (services, map registry, session infrastructure).

    /// </summary>

    public class FusionWorldTravelBootstrapPlayModeTest

    {

        [SetUp]

        public void SetUp() => WorldTravelResilienceTestSupport.ResetTravelStaticState();



        [TearDown]

        public void TearDown() => WorldTravelResilienceTestSupport.ResetTravelStaticState();



        [UnityTest]

        [Category(TestCategories.Integration)]

        public IEnumerator PreLoadBootstrap_ProvidesWorldTravelServices() =>

            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(PreLoadBootstrapBody());



        [UnityTest]

        [Category(TestCategories.Integration)]

        public IEnumerator PreLoadBootstrap_CanRegisterOpenWorldMapWhenAssigned() =>

            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(PreLoadRegisterBody());



        [UnityTest]

        [Category(TestCategories.Integration)]

        public IEnumerator OpenWorldBootstrap_ExposesTravelServiceToPlayerInteraction() =>

            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(OpenWorldBootstrapBody());



        [UnityTest]

        [Category(TestCategories.Fast)]

        public IEnumerator HarnessBootstrap_ProvidesSessionSwitchInfrastructure() =>

            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(HarnessBootstrapBody());



        private static IEnumerator PreLoadBootstrapBody()

        {

            ServiceLocator.ResetForPlayModeTests();

            WorldMapDefinitionLoader.ResetForPlayModeTests();

            yield return LoadPreloadScene();



            Assert.That(Object.FindFirstObjectByType<WorldTravelService>(), Is.Not.Null,

                "PreLoad bootstrap should provide a WorldTravelService.");

            Assert.That(Object.FindFirstObjectByType<WorldMapUI>(FindObjectsInactive.Include), Is.Not.Null,

                "PreLoad bootstrap should provide a WorldMapUI instance.");

            yield return null;

        }



        private static IEnumerator PreLoadRegisterBody()

        {

            ServiceLocator.ResetForPlayModeTests();

            WorldMapDefinitionLoader.ResetForPlayModeTests();

            yield return LoadPreloadScene();



            WorldTravelBootstrap bootstrap = Object.FindFirstObjectByType<WorldTravelBootstrap>();

            if (bootstrap == null)

            {

                Assert.Inconclusive("WorldTravelBootstrap was not found in PreLoad.");

                yield break;

            }



            WorldMapDefinitionSO map = WorldMapDefinitionLoader.LoadOpenWorldMap();

            if (map == null)

            {

                Assert.Inconclusive(

                    "Assign OpenWorldMap on WorldTravelBootstrap in PreLoad to enable runtime registry coverage.");

                yield break;

            }



            WorldMapDefinitionLoader.Initialize(map);

            Assert.That(

                WorldMapDefinitionLoader.ResolveByMapId(WorldTravelResilienceTestSupport.CampaignMapId),

                Is.Not.Null);

            yield return null;

        }



        private static IEnumerator OpenWorldBootstrapBody()

        {

            FusionMultiPeerTestSupport.AssertMultiPeerMode();

            yield return WorldTravelTestSupport.BootstrapOpenWorldMultiPeer();



            var runners = new List<NetworkRunner>();

            yield return FusionMultiPeerTestSupport.WaitForRunnersWithLocalPlayers(1, runners);



            PlayerInteraction interaction = FusionMultiPeerTestSupport.GetLocalPlayerInteraction(runners[0]);

            Assert.That(interaction, Is.Not.Null);

            Assert.That(

                ServiceLocator.For(interaction).TryGet(out IWorldTravelService travelService),

                Is.True,

                "Local player interaction should resolve IWorldTravelService in OpenWorld.");

            Assert.That(travelService, Is.Not.Null);



            yield return FusionMultiPeerTestSupport.ShutdownExistingRunners();

        }



        private static IEnumerator HarnessBootstrapBody()

        {

            FusionMultiPeerTestSupport.AssertMultiPeerMode();

            yield return WorldTravelTestSupport.BootstrapHarnessMultiPeer();



            var runners = new List<NetworkRunner>();

            yield return FusionMultiPeerTestSupport.WaitForRunnersWithLocalPlayers(1, runners);



            PlayerInteraction interaction = FusionMultiPeerTestSupport.GetLocalPlayerInteraction(runners[0]);

            Assert.That(interaction, Is.Not.Null);

            Assert.That(

                ServiceLocator.For(interaction).TryGet(out IWorldTravelService travelService),

                Is.True,

                "Harness should expose IWorldTravelService to local player interaction.");

            Assert.That(travelService, Is.Not.Null);



            Assert.That(

                ServiceLocator.Global.TryGet(out INetworkSessionService _),

                Is.True,

                "Harness should register INetworkSessionService for session-switch travel.");

            Assert.That(

                ServiceLocator.Global.TryGet(out EnglishQuest.UI.Loading.ILoadingScreenService _),

                Is.True,

                "Harness should register ILoadingScreenService for session-switch travel.");



            yield return FusionMultiPeerTestSupport.ShutdownExistingRunners();

        }



        private static IEnumerator LoadPreloadScene()

        {

            yield return FusionMultiPeerTestSupport.ShutdownExistingRunners();

            yield return null;



            AsyncOperation preloadOperation =

                SceneManager.LoadSceneAsync(WorldTravelTestSupport.PreLoadSceneName, LoadSceneMode.Single);

            Assert.That(preloadOperation, Is.Not.Null);

            while (!preloadOperation.isDone)

                yield return null;



            yield return FusionMultiPeerTestSupport.WaitUntil(

                () => Object.FindFirstObjectByType<WorldTravelService>() != null,

                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,

                "PreLoad bootstrap should provide WorldTravelService.");

        }

    }

}



