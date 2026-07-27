using System.Collections;

using System.Collections.Generic;

using EnglishQuest.Tests;

using Fusion;

using NUnit.Framework;

using UnityEngine.TestTools;



namespace EnglishQuest.Tests.RunTime

{

    /// <summary>

    /// Integration smoke for production OpenWorld world-travel with session switching.

    /// </summary>

    [Category(TestCategories.Integration)]

    public class FusionWorldTravelProductionSmokeTest

    {

        private readonly List<NetworkRunner> _runners = new();



        [UnityTest]

        public IEnumerator MultiPeer_RoundTrip_OpenWorldLesson1IntroOpenWorld() =>

            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(RoundTripBody());



        private IEnumerator RoundTripBody()

        {

            WorldTravelResilienceTestSupport.ResetTravelStaticState();

            WorldTravelResilienceTestSupport.EnsureRuntimeMapRegistry();



            FusionMultiPeerTestSupport.AssertMultiPeerMode();

            yield return WorldTravelTestSupport.BootstrapOpenWorldMultiPeer();

            yield return FusionMultiPeerTestSupport.WaitForRunnersWithLocalPlayers(2, _runners);

            yield return WorldTravelTestSupport.WaitForWorldTravelReady(

                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,

                "World travel systems should be ready in OpenWorld.");

            yield return WorldTravelResilienceTestSupport.PrepareParty(_runners[0], _runners[1]);



            WorldMapNpcInteractable npc = WorldTravelTestSupport.FindWorldMapNpcInOpenWorld();



            yield return WorldTravelResilienceTestSupport.TravelLeaderBetweenNodes(

                _runners[0],

                npc,

                WorldTravelTestSupport.TravelOriginNodeId,

                WorldTravelTestSupport.TravelDestinationNodeId);



            yield return WorldTravelTestSupport.WaitForAllRunnersInScene(

                _runners,

                WorldTravelTestSupport.TravelDestinationSceneName,

                WorldTravelTestSupport.TravelTimeoutSeconds,

                "All party members should arrive in Lesson 1 Introduction.",

                WorldTravelTestSupport.TravelDestinationSpawnId);



            WorldTravelTestSupport.AssertAllRunnersShareSession(_runners, expectedSessionPrefix: "Travel_");

            WorldTravelTestSupport.AssertSessionPlayerCountAtMost(_runners, maxExpected: 2);



            yield return WorldTravelResilienceTestSupport.TravelLeaderBetweenNodes(

                _runners[0],

                null,

                WorldTravelTestSupport.TravelDestinationNodeId,

                WorldTravelTestSupport.TravelOriginNodeId);



            yield return WorldTravelTestSupport.WaitForAllRunnersInScene(

                _runners,

                WorldTravelTestSupport.OpenWorldSceneName,

                WorldTravelTestSupport.TravelTimeoutSeconds,

                "All party members should return to OpenWorld.",

                WorldTravelTestSupport.OpenWorldSpawnId);



            WorldTravelTestSupport.AssertAllRunnersShareSession(

                _runners,

                expectedSessionName: WorldTravelSessionNaming.ResolveOpenWorldSessionName());



            yield return FusionMultiPeerTestSupport.ShutdownExistingRunners();

        }

    }

}



