using System.Collections;
using Cysharp.Threading.Tasks;
using EnglishKingdom.Tests.RunTime.Fixtures;

using Fusion;

using NUnit.Framework;

using UnityEngine.TestTools;



namespace EnglishKingdom.Tests.RunTime

{

    /// <summary>

    /// Multi-peer Play Mode tests for party travel presentation sync and authority guardrails.

    /// </summary>

    public class FusionWorldTravelPresentationMultiPeerTest : HarnessPartyTravelFixture

    {

        [UnityTest]

        public IEnumerator MultiPeer_MemberSeesPresentationDuringLeaderTravel() =>

            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(MemberPresentationBody());



        [UnityTest]

        public IEnumerator MultiPeer_NonLeaderCannotInitiateTravel() =>

            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(NonLeaderBlockedBody());



        [UnityTest]

        public IEnumerator MultiPeer_MemberTravelStateClearsAfterLeaderTravel() =>

            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(MemberTravelStateClearsBody());



        [UnityTest]

        public IEnumerator MultiPeer_WorldMapClosesAfterPartyTravel() =>

            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(WorldMapClosesBody());



        [UnityTest]

        public IEnumerator MultiPeer_MemberPresentationGateWaitsForLeaderEpoch() =>

            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(PresentationEpochBody());



        private IEnumerator MemberPresentationBody()

        {

            WorldMapNpcInteractable npc = FusionHarnessFactory.FindHarnessTravelNpc();



            bool presentationSeen = false;

            IEnumerator travelRoutine = WorldTravelTestSupport.TriggerPartyTravelFromLeader(Runners[0], npc);

            while (travelRoutine.MoveNext())

            {

                if (!presentationSeen && WorldTravelTestSupport.IsTravelPresentationActive())

                    presentationSeen = true;



                yield return travelRoutine.Current;

            }



            Assert.That(presentationSeen, Is.True,

                "Shared world map presentation should be visible during leader travel.");



            yield return WorldTravelTestSupport.WaitForAllRunnersInScene(

                Runners,

                WorldTravelTestSupport.ActiveDestinationSceneName,

                WorldTravelTestSupport.ActiveTravelTimeoutSeconds,

                "Member should follow leader to the harness destination.",

                WorldTravelTestSupport.ActiveDestinationSpawnId);



            WorldTravelTestSupport.AssertAllRunnersShareSession(Runners, expectedSessionPrefix: "Travel_");

        }



        private IEnumerator NonLeaderBlockedBody()

        {

            yield return WorldTravelTestSupport.AttemptTravelAsNonLeader(Runners[1]);



            yield return WorldTravelTestSupport.WaitForAllRunnersInScene(

                Runners,

                WorldTravelTestSupport.ActiveOriginSceneName,

                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,

                "Non-leader travel attempt should leave party in the harness origin scene.",

                WorldTravelTestSupport.ActiveOriginSpawnId);

        }



        private IEnumerator MemberTravelStateClearsBody()

        {

            WorldMapNpcInteractable npc = FusionHarnessFactory.FindHarnessTravelNpc();



            yield return WorldTravelTestSupport.TriggerPartyTravelFromLeader(Runners[0], npc);

            yield return WorldTravelTestSupport.WaitForAllRunnersInScene(

                Runners,

                WorldTravelTestSupport.ActiveDestinationSceneName,

                WorldTravelTestSupport.ActiveTravelTimeoutSeconds,

                "Party should arrive in the harness destination.",

                WorldTravelTestSupport.ActiveDestinationSpawnId);



            yield return WorldTravelResilienceTestSupport.AssertAllRunnersNotTraveling(

                Runners,

                WorldTravelResilienceTestSupport.StateIdleTimeoutSeconds,

                "Member travel state should clear after leader travel completes.");



            Assert.That(

                WorldTravelResilienceTestSupport.TryGetTravelService(

                    Runners[1],

                    out IWorldTravelService memberTravel,

                    out PlayerInteraction memberInteraction),

                Is.True);

            Assert.That(memberTravel.IsTravelingFor(memberInteraction), Is.False);

        }



        private IEnumerator WorldMapClosesBody()

        {

            WorldMapNpcInteractable npc = FusionHarnessFactory.FindHarnessTravelNpc();



            yield return WorldTravelTestSupport.TriggerPartyTravelFromLeader(Runners[0], npc);

            yield return WorldTravelResilienceTestSupport.AssertAllRunnersNotTraveling(

                Runners,

                WorldTravelResilienceTestSupport.StateIdleTimeoutSeconds,

                "Travel should finish before asserting map UI state.");



            WorldMapUI mapUi = WorldTravelTestSupport.FindWorldMapUi();

            if (mapUi != null)

            {

                Assert.That(mapUi.IsInTravelMode, Is.False,

                    "Shared world map should exit travel mode after party travel.");

            }

        }



        private IEnumerator PresentationEpochBody()

        {

            int epoch = PartyTravelPresentationGate.BeginPresentation();

            Assert.That(epoch, Is.GreaterThan(0));



            UniTask waitTask = PartyTravelPresentationGate.WaitForPresentationCompleteAsync(epoch);

            yield return null;

            Assert.That(waitTask.GetAwaiter().IsCompleted, Is.False,

                "Follower gate should wait until leader signals the same presentation epoch.");



            PartyTravelPresentationGate.SignalPresentationComplete(epoch);

            yield return waitTask.ToCoroutine();

        }

    }

}


