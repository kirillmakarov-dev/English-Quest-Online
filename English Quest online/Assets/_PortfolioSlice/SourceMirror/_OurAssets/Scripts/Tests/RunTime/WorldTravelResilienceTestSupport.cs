using System.Collections;

using System.Collections.Generic;

using Cysharp.Threading.Tasks;

using Fusion;

using NUnit.Framework;

using UnityEngine;

using UnityServiceLocator;



namespace EnglishQuest.Tests.RunTime

{

    /// <summary>

    /// Helpers for world-travel Play Mode tests (session switching, party follow, state cleanup).

    /// </summary>

    internal static class WorldTravelResilienceTestSupport

    {

        public const string CampaignMapId = "campaign_v1";

        public const float PresentationTimeoutSeconds = 15f;

        public const float StateIdleTimeoutSeconds = 30f;



        public static void ResetTravelTransientState()

        {

            PlayerTravelArrival.ClearAll();

            PartyTravelPresentationGate.ClearAll();



            foreach (NetworkRunner runner in NetworkRunner.Instances)

            {

                if (runner == null)

                    continue;



                FusionSceneTransitionService.ClearRunner(runner);

                TravelSceneLoadCoordinator.ClearRunner(runner);

            }



            WorldTravelService[] travelServices = Object.FindObjectsByType<WorldTravelService>(

                FindObjectsInactive.Include,

                FindObjectsSortMode.None);



            for (int i = 0; i < travelServices.Length; i++)

            {

                WorldTravelService travelService = travelServices[i];

                if (travelService == null)

                    continue;



                foreach (NetworkRunner runner in NetworkRunner.Instances)

                    travelService.HandleRunnerDisconnected(runner);

            }

        }



        public static void ResetTravelStaticState()

        {

            WorldMapDefinitionLoader.ResetForPlayModeTests();

            DestroyStaleHarnessBootstrap();

            ResetTravelTransientState();

        }



        public static void EnsureRuntimeMapRegistry() =>

            FusionHarnessFactory.InitializeHarnessMapRegistry();



        public static bool TryGetTravelService(

            NetworkRunner runner,

            out IWorldTravelService travelService,

            out PlayerInteraction interaction)

        {

            travelService = null;

            interaction = FusionMultiPeerTestSupport.GetLocalPlayerInteraction(runner);

            if (interaction == null)

                return false;



            return ServiceLocator.For(interaction).TryGet(out travelService);

        }



        public static IEnumerator PrepareParty(NetworkRunner leaderRunner, NetworkRunner memberRunner)

        {

            MonoBehaviour leaderContext =

                FusionMultiPeerTestSupport.FindSocialContextForRunner(leaderRunner);

            MonoBehaviour memberContext =

                FusionMultiPeerTestSupport.FindSocialContextForRunner(memberRunner);



            yield return FusionMultiPeerTestSupport.WaitForSocialServicesReady(

                leaderRunner,

                leaderContext,

                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,

                "Leader social services should be ready.");

            yield return FusionMultiPeerTestSupport.WaitForSocialServicesReady(

                memberRunner,

                memberContext,

                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,

                "Member social services should be ready.");



            yield return FusionMultiPeerTestSupport.WaitForRemotePlayersVisible(

                leaderRunner,

                memberRunner,

                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,

                "Both runners should see remote party membership before forming a party.");



            PlayerRef leaderRef = leaderRunner.LocalPlayer;

            Assert.That(

                FusionMultiPeerTestSupport.TryGetRemotePlayerRef(leaderRunner, out PlayerRef memberRef),

                Is.True,

                "Leader runner should resolve the remote member PlayerRef.");



            yield return FusionMultiPeerTestSupport.FormPartyViaInvite(

                leaderRunner, memberRunner, leaderRef, memberRef);

        }



        public static IEnumerator TravelLeaderBetweenNodes(

            NetworkRunner leaderRunner,

            WorldMapNpcInteractable npc,

            string fromNodeId,

            string toNodeId)

        {

            PlayerInteraction leaderInteraction =

                FusionMultiPeerTestSupport.GetLocalPlayerInteraction(leaderRunner);

            Assert.That(leaderInteraction, Is.Not.Null);



            WorldMapDefinitionSO map = WorldTravelTestSupport.ResolveActiveTravelMap();

            Assert.That(map, Is.Not.Null);



            if (npc != null)

            {

                WorldMapUI staleMapUi = WorldTravelTestSupport.FindWorldMapUi();

                if (staleMapUi != null && staleMapUi.IsOpen)

                    staleMapUi.Close();



                Assert.That(npc.Interact(leaderInteraction), Is.True,

                    "Leader should open the world map via NPC.");



                yield return FusionMultiPeerTestSupport.WaitUntil(

                    () =>

                    {

                        WorldMapUI ui = WorldTravelTestSupport.FindWorldMapUi();

                        return ui != null && ui.IsOpen;

                    },

                    5f,

                    "World map should open before travel.");

            }



            Assert.That(TryGetTravelService(leaderRunner, out IWorldTravelService travelService, out _), Is.True);



            yield return WorldTravelTestSupport.RunTravelAsync(

                travelService.TravelAsync(map, fromNodeId, toNodeId, leaderInteraction),

                WorldTravelTestSupport.ActiveTravelTimeoutSeconds,

                $"Leader travel from '{fromNodeId}' to '{toNodeId}' should complete.");

        }



        public static IEnumerator WaitUntilLeaderTraveling(

            NetworkRunner leaderRunner,

            float timeoutSeconds,

            string failureMessage)

        {

            yield return FusionMultiPeerTestSupport.WaitUntil(

                () =>

                {

                    if (!TryGetTravelService(leaderRunner, out IWorldTravelService travelService, out PlayerInteraction interaction))

                        return false;



                    return travelService.IsTravelingFor(interaction);

                },

                timeoutSeconds,

                failureMessage);

        }



        public static IEnumerator AssertAllRunnersNotTraveling(

            IReadOnlyList<NetworkRunner> runners,

            float timeoutSeconds,

            string failureMessage)

        {

            yield return WorldTravelTestSupport.WaitForCoSessionTravelIdle(

                runners is List<NetworkRunner> list ? list : new List<NetworkRunner>(runners),

                timeoutSeconds,

                failureMessage);

        }



        public static IEnumerator AssertNoPendingArrivalForRunners(IReadOnlyList<NetworkRunner> runners)

        {

            for (int i = 0; i < runners.Count; i++)

            {

                NetworkRunner runner = runners[i];

                Assert.That(PlayerTravelArrival.HasPendingFor(runner), Is.False,

                    $"Runner '{WorldTravelTestSupport.FormatRunnerLabel(runner, i)}' should not retain pending travel arrival.");

            }



            yield return null;

        }



        public static void DestroyStaleHarnessBootstrapForShutdown()

        {

            GameObject harnessRoot = GameObject.Find("World Travel Harness Bootstrap");

            if (harnessRoot != null)

                Object.Destroy(harnessRoot);

        }



        private static void DestroyStaleHarnessBootstrap() =>

            DestroyStaleHarnessBootstrapForShutdown();

    }

}



