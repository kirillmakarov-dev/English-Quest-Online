using Fusion;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace EnglishQuest.Tests.WorldTravel
{
    public class WorldTravelSessionResolverTests
    {
        [Test]
        public void Resolve_OpenWorldNode_UsesOpenWorldSession()
        {
            var destination = new WorldMapNodeData
            {
                id = WorldTravelSessionNaming.OpenWorldNodeId,
                destinationType = WorldMapDestinationType.LoadScene
            };

            TravelSessionPlan plan = WorldTravelSessionResolver.Resolve(
                in destination,
                interactor: null,
                presentationEpoch: 1,
                createsSession: true);

            Assert.That(plan.SessionName, Is.EqualTo(WorldTravelSessionNaming.ResolveOpenWorldSessionName()));
            Assert.That(plan.Mode, Is.EqualTo(WorldTravelSessionMode.SharedPool));
            Assert.That(plan.CreatesSession, Is.True);
        }

        [Test]
        public void Resolve_DefaultLessonNode_UsesPartyIsolatedMode()
        {
            var destination = new WorldMapNodeData
            {
                id = "lesson_1_intro",
                destinationType = WorldMapDestinationType.LoadScene
            };

            TravelSessionPlan plan = WorldTravelSessionResolver.Resolve(
                in destination,
                interactor: null,
                presentationEpoch: 3,
                createsSession: true);

            Assert.That(plan.Mode, Is.EqualTo(WorldTravelSessionMode.PartyIsolated));
            Assert.That(plan.SessionName, Does.StartWith("Travel_"));
            Assert.That(plan.MaxPlayers, Is.EqualTo(1));
        }

        [Test]
        public void Resolve_SharedPoolNode_UsesConfiguredSessionName()
        {
            var destination = new WorldMapNodeData
            {
                id = "lesson_1_intro",
                destinationType = WorldMapDestinationType.LoadScene,
                sessionMode = WorldTravelSessionMode.SharedPool,
                sharedSessionName = "Lesson1Intro",
                sharedSessionMaxPlayers = 12
            };

            TravelSessionPlan plan = WorldTravelSessionResolver.Resolve(
                in destination,
                interactor: null,
                presentationEpoch: 1,
                createsSession: true);

            Assert.That(plan.Mode, Is.EqualTo(WorldTravelSessionMode.SharedPool));
            Assert.That(plan.SessionName, Is.EqualTo("Lesson1Intro"));
            Assert.That(plan.MaxPlayers, Is.EqualTo(12));
        }
    }
}

