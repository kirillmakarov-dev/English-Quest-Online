using System.Collections;

using System.Collections.Generic;

using Cysharp.Threading.Tasks;

using EnglishQuest.Tests.RunTime.Fixtures;

using Fusion;

using NUnit.Framework;

using UnityEngine;

using UnityEngine.TestTools;



namespace EnglishQuest.Tests.RunTime

{

    /// <summary>

    /// Multi-peer Play Mode tests for the session-switching world travel pipeline.

    /// </summary>

    public class FusionWorldTravelResilienceMultiPeerTest : HarnessPartyTravelFixture

    {

        [UnityTest]

        public IEnumerator MultiPeer_RuntimeMapRegistry_ResolvesHarnessMapId() =>

            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(RuntimeMapRegistryBody());



        [UnityTest]

        public IEnumerator MultiPeer_PartyTravel_SwitchesToIsolatedSession() =>

            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(IsolatedSessionBody());



        [UnityTest]

        public IEnumerator MultiPeer_SharedPoolDestination_UsesFixedSessionName() =>

            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(SharedPoolSessionBody());



        [UnityTest]

        public IEnumerator MultiPeer_PartyFollowViaRpc_LandsInSameIsolatedSession() =>

            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(PartyFollowRpcBody());



        [UnityTest]

        public IEnumerator MultiPeer_SequentialBackToBackTravels_Succeed() =>

            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(SequentialTravelsBody());



        [UnityTest]

        public IEnumerator MultiPeer_AllRunnersClearTravelStateAfterArrival() =>

            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(ClearTravelStateBody());



        [UnityTest]

        public IEnumerator MultiPeer_NoPendingArrivalAfterSuccessfulTravel() =>

            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(NoPendingArrivalBody());



        [UnityTest]

        public IEnumerator MultiPeer_ReentrantTravel_BlockedDuringActiveTravel() =>

            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(ReentrantTravelBody());



        [UnityTest]

        public IEnumerator MultiPeer_LoadingScreen_ActiveDuringSessionSwitch() =>

            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(LoadingScreenBody());



        [UnityTest]

        public IEnumerator MultiPeer_DisconnectCleanup_ClearsTravelState() =>

            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(DisconnectCleanupBody());



        private IEnumerator RuntimeMapRegistryBody()

        {

            Assert.That(

                WorldMapDefinitionLoader.ResolveByMapId(FusionHarnessFactory.HarnessMapId),

                Is.Not.Null,

                "Harness map registry should resolve the active travel map.");

            yield return null;

        }



        private IEnumerator IsolatedSessionBody()

        {

            yield return TravelPartyToDefaultDestination();



            WorldTravelTestSupport.AssertAllRunnersShareSession(Runners, expectedSessionPrefix: "Travel_");

            WorldTravelTestSupport.AssertSessionPlayerCountAtMost(Runners, maxExpected: 2);

        }



        private IEnumerator SharedPoolSessionBody()

        {

            yield return WorldTravelResilienceTestSupport.TravelLeaderBetweenNodes(

                Runners[0],

                FusionHarnessFactory.FindHarnessTravelNpc(),

                FusionHarnessFactory.HarnessOriginNodeId,

                FusionHarnessFactory.HarnessSharedDestinationNodeId);



            yield return WorldTravelTestSupport.WaitForAllRunnersInScene(

                Runners,

                WorldTravelTestSupport.ActiveDestinationSceneName,

                WorldTravelTestSupport.ActiveTravelTimeoutSeconds,

                "Party should arrive at the shared-pool harness destination.",

                WorldTravelTestSupport.ActiveDestinationSpawnId);



            WorldTravelTestSupport.AssertAllRunnersShareSession(

                Runners,

                expectedSessionName: FusionHarnessFactory.HarnessSharedSessionName);

        }



        private IEnumerator PartyFollowRpcBody()

        {

            FusionHarnessFactory.InitializeHarnessMapRegistry();



            WorldMapNpcInteractable npc = FusionHarnessFactory.FindHarnessTravelNpc();

            Assert.That(npc, Is.Not.Null);



            yield return WorldTravelTestSupport.TriggerPartyTravelFromLeader(Runners[0], npc);



            yield return WorldTravelTestSupport.WaitForAllRunnersInScene(

                Runners,

                WorldTravelTestSupport.ActiveDestinationSceneName,

                WorldTravelTestSupport.ActiveTravelTimeoutSeconds,

                "Member party follow via RPC should reach the harness destination.",

                WorldTravelTestSupport.ActiveDestinationSpawnId);



            WorldTravelTestSupport.AssertAllRunnersShareSession(Runners, expectedSessionPrefix: "Travel_");

            WorldTravelTestSupport.AssertSessionPlayerCountAtMost(Runners, maxExpected: 2);

        }



        private IEnumerator SequentialTravelsBody()

        {

            WorldMapNpcInteractable npc = FusionHarnessFactory.FindHarnessTravelNpc();



            for (int trip = 0; trip < 2; trip++)

            {

                yield return WorldTravelResilienceTestSupport.TravelLeaderBetweenNodes(

                    Runners[0],

                    npc,

                    WorldTravelTestSupport.ActiveOriginNodeId,

                    WorldTravelTestSupport.ActiveDestinationNodeId);



                yield return WorldTravelTestSupport.WaitForAllRunnersInScene(

                    Runners,

                    WorldTravelTestSupport.ActiveDestinationSceneName,

                    WorldTravelTestSupport.ActiveTravelTimeoutSeconds,

                    $"Trip {trip + 1}: all members should reach the harness destination.",

                    WorldTravelTestSupport.ActiveDestinationSpawnId);



                yield return WorldTravelResilienceTestSupport.AssertAllRunnersNotTraveling(

                    Runners,

                    WorldTravelResilienceTestSupport.StateIdleTimeoutSeconds,

                    $"Trip {trip + 1}: travel should be idle before returning.");



                yield return WorldTravelResilienceTestSupport.TravelLeaderBetweenNodes(

                    Runners[0],

                    null,

                    WorldTravelTestSupport.ActiveDestinationNodeId,

                    WorldTravelTestSupport.ActiveOriginNodeId);



                yield return WorldTravelTestSupport.WaitForAllRunnersInScene(

                    Runners,

                    WorldTravelTestSupport.ActiveOriginSceneName,

                    WorldTravelTestSupport.ActiveTravelTimeoutSeconds,

                    $"Trip {trip + 1}: all members should return to the harness origin.",

                    WorldTravelTestSupport.ActiveOriginSpawnId);

            }

        }



        private IEnumerator ClearTravelStateBody()

        {

            yield return TravelPartyToDefaultDestination();



            yield return WorldTravelResilienceTestSupport.AssertAllRunnersNotTraveling(

                Runners,

                WorldTravelResilienceTestSupport.StateIdleTimeoutSeconds,

                "All runners should clear travel state after arrival.");



            for (int i = 0; i < Runners.Count; i++)

            {

                Assert.That(

                    WorldTravelResilienceTestSupport.TryGetTravelService(

                        Runners[i],

                        out IWorldTravelService travelService,

                        out PlayerInteraction interaction),

                    Is.True);



                Assert.That(travelService.IsTravelingFor(interaction), Is.False,

                    $"Runner '{Runners[i].name}' should not remain in traveling state.");

            }

        }



        private IEnumerator NoPendingArrivalBody()

        {

            yield return TravelPartyToDefaultDestination();

            yield return WorldTravelResilienceTestSupport.AssertNoPendingArrivalForRunners(Runners);

        }



        private IEnumerator ReentrantTravelBody()

        {

            NetworkRunner leaderRunner = Runners[0];

            Assert.That(

                WorldTravelResilienceTestSupport.TryGetTravelService(

                    leaderRunner,

                    out IWorldTravelService travelService,

                    out PlayerInteraction interaction),

                Is.True);



            WorldMapDefinitionSO map = WorldTravelTestSupport.ResolveActiveTravelMap();

            Assert.That(map, Is.Not.Null);



            UniTask firstTravel = travelService.TravelAsync(

                map,

                WorldTravelTestSupport.ActiveOriginNodeId,

                WorldTravelTestSupport.ActiveDestinationNodeId,

                interaction);



            yield return WorldTravelResilienceTestSupport.WaitUntilLeaderTraveling(

                leaderRunner,

                WorldTravelResilienceTestSupport.PresentationTimeoutSeconds,

                "Leader should enter traveling state before re-entrancy check.");



            Assert.That(travelService.CanTravel(interaction), Is.False);

            Assert.That(travelService.IsTravelingFor(interaction), Is.True);



            UniTask secondTravel = travelService.TravelAsync(

                map,

                WorldTravelTestSupport.ActiveOriginNodeId,

                WorldTravelTestSupport.ActiveDestinationNodeId,

                interaction);



            yield return WorldTravelTestSupport.RunTravelAsync(

                firstTravel,

                WorldTravelTestSupport.ActiveTravelTimeoutSeconds,

                "First travel should still complete successfully.");



            Assert.That(secondTravel.GetAwaiter().IsCompleted, Is.True,

                "Second travel attempt should complete without starting a parallel trip.");



            yield return WorldTravelTestSupport.WaitForAllRunnersInScene(

                Runners,

                WorldTravelTestSupport.ActiveDestinationSceneName,

                WorldTravelTestSupport.ActiveTravelTimeoutSeconds,

                "Party should still arrive after guarded re-entrancy.",

                WorldTravelTestSupport.ActiveDestinationSpawnId);

        }



        private IEnumerator LoadingScreenBody()

        {

            EnglishQuest.UI.Loading.LoadingScreenManager loadingScreen =

                Object.FindFirstObjectByType<EnglishQuest.UI.Loading.LoadingScreenManager>(

                    FindObjectsInactive.Include);



            if (loadingScreen == null)

            {

                Assert.Inconclusive("LoadingScreenManager is not present in the harness scene.");

                yield break;

            }



            Assert.That(loadingScreen.IsLoading, Is.False,

                "Loading screen should be idle before travel begins.");



            WorldMapNpcInteractable npc = FusionHarnessFactory.FindHarnessTravelNpc();

            IEnumerator travelRoutine = WorldTravelTestSupport.TriggerPartyTravelFromLeader(Runners[0], npc);



            while (travelRoutine.MoveNext())

                yield return travelRoutine.Current;



            yield return WorldTravelTestSupport.WaitForAllRunnersInScene(

                Runners,

                WorldTravelTestSupport.ActiveDestinationSceneName,

                WorldTravelTestSupport.ActiveTravelTimeoutSeconds,

                "Party should arrive after session switch.",

                WorldTravelTestSupport.ActiveDestinationSpawnId);



            Assert.That(loadingScreen.IsLoading, Is.False,

                "Loading screen should be idle after travel completes.");

        }



        private IEnumerator DisconnectCleanupBody()

        {

            NetworkRunner leaderRunner = Runners[0];

            WorldTravelService travelServiceComponent = Object.FindFirstObjectByType<WorldTravelService>();

            Assert.That(travelServiceComponent, Is.Not.Null);



            Assert.That(

                WorldTravelResilienceTestSupport.TryGetTravelService(

                    leaderRunner,

                    out IWorldTravelService travelService,

                    out PlayerInteraction interaction),

                Is.True);



            travelServiceComponent.HandleRunnerDisconnected(leaderRunner);

            Assert.That(travelService.IsTravelingFor(interaction), Is.False);

            Assert.That(PlayerTravelArrival.HasPendingFor(leaderRunner), Is.False);

            Assert.That(FusionSceneTransitionService.IsLoading(leaderRunner), Is.False);

            yield return null;

        }



        private IEnumerator TravelPartyToDefaultDestination()

        {

            WorldMapNpcInteractable npc = FusionHarnessFactory.FindHarnessTravelNpc();

            yield return WorldTravelTestSupport.TriggerPartyTravelFromLeader(Runners[0], npc);

            yield return WorldTravelTestSupport.WaitForAllRunnersInScene(

                Runners,

                WorldTravelTestSupport.ActiveDestinationSceneName,

                WorldTravelTestSupport.ActiveTravelTimeoutSeconds,

                "All party members should arrive in the harness destination.",

                WorldTravelTestSupport.ActiveDestinationSpawnId);

        }

    }

}



