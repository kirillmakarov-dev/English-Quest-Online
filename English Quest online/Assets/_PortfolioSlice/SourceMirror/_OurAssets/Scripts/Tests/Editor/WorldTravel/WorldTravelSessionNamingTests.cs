using Fusion;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace EnglishKingdom.Tests.WorldTravel
{
    public class WorldTravelSessionNamingTests
    {
        [Test]
        public void CreatePartyIsolatedSessionName_UsesLeaderNodeAndEpoch()
        {
            string sessionName = WorldTravelSessionNaming.CreatePartyIsolatedSessionName(
                PlayerRef.FromIndex(1),
                "lesson_1_intro",
                7);

            Assert.That(sessionName, Is.EqualTo("Travel_1_lesson_1_intro_7"));
        }

        [Test]
        public void ResolveSharedPoolName_UsesConfiguredName()
        {
            var node = new WorldMapNodeData
            {
                id = "lesson_1_intro",
                sharedSessionName = "Lesson1Intro"
            };

            Assert.That(
                WorldTravelSessionNaming.ResolveSharedPoolName(in node),
                Is.EqualTo("Lesson1Intro"));
        }

        [Test]
        public void ResolveSharedPoolName_FallsBackToNodeId()
        {
            var node = new WorldMapNodeData
            {
                id = "lesson-2_jump"
            };

            Assert.That(
                WorldTravelSessionNaming.ResolveSharedPoolName(in node),
                Is.EqualTo("lesson_2_jump"));
        }

        [Test]
        public void ResolveOpenWorldSessionName_UsesDevRoomOutsideProduction()
        {
            string sessionName = WorldTravelSessionNaming.ResolveOpenWorldSessionName();

            if (WorldTravelSessionNaming.IsNonProductionEnvironment())
                Assert.That(sessionName, Is.EqualTo(WorldTravelSessionNaming.OpenWorldDevelopmentSessionName));
            else
                Assert.That(sessionName, Is.EqualTo(WorldTravelSessionNaming.OpenWorldSessionName));
        }
    }
}
