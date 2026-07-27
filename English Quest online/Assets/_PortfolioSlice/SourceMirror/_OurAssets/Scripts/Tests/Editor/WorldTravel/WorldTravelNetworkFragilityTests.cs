using EnglishQuest.Tests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EnglishQuest.Tests.WorldTravel
{
    /// <summary>
    /// Edit Mode tests for world-travel safety rails not covered elsewhere.
    /// </summary>
    [Category(TestCategories.Fast)]
    public class WorldTravelNetworkFragilityTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerTravelArrival.ClearAll();
            PartyTravelPresentationGate.ClearAll();
            WorldMapDefinitionLoader.Initialize(null);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerTravelArrival.ClearAll();
            PartyTravelPresentationGate.ClearAll();
            WorldMapDefinitionLoader.Initialize(null);
        }

        [Test]
        public void RuntimePartyFollow_ResolvesRegisteredCampaignMap()
        {
            WorldMapDefinitionLoader.Initialize(null);
            WorldMapDefinitionSO map = WorldMapDefinitionLoader.LoadOpenWorldMap();
#if UNITY_EDITOR
            if (map == null)
                map = AssetDatabase.LoadAssetAtPath<WorldMapDefinitionSO>(WorldMapDefinitionLoader.OpenWorldMapAssetPath);
#endif
            Assert.That(map, Is.Not.Null, "OpenWorldMap must be loadable for registry coverage.");
            WorldMapDefinitionLoader.Initialize(map);

            WorldMapDefinitionSO resolved = WorldMapDefinitionLoader.ResolveByMapId("campaign_v1");
            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved.MapId, Is.EqualTo("campaign_v1"));
        }

        [Test]
        public void AbortedTravel_ClearsPendingArrival()
        {
            var destination = new WorldMapNodeData
            {
                id = "abort-node",
                destinationType = WorldMapDestinationType.LoadScene
            };

            PlayerTravelArrival.SetPending(null, "abort-node", 99998, in destination);
            Assert.That(PlayerTravelArrival.HasPending, Is.True);

            PlayerTravelArrival.Clear(null);
            Assert.That(PlayerTravelArrival.HasPending, Is.False);
        }

        [Test]
        public void PendingArrival_IsScopedPerRunnerSlot()
        {
            var destinationA = new WorldMapNodeData { id = "node-a" };
            var destinationB = new WorldMapNodeData { id = "node-b" };

            PlayerTravelArrival.SetPending(null, "offline-a", 10, in destinationA);
            Assert.That(PlayerTravelArrival.HasPendingFor(null), Is.True);

            PlayerTravelArrival.Clear(null);
            PlayerTravelArrival.SetPending(null, "offline-b", 20, in destinationB);

            Scene scene = SceneManager.GetActiveScene();
            Assert.That(
                PlayerTravelArrival.TryPeekForScene(null, scene, out PlayerTravelArrival.PendingArrival arrival),
                Is.EqualTo(scene.buildIndex == 20));
            if (scene.buildIndex == 20)
                Assert.That(arrival.SpawnNodeId, Is.EqualTo("offline-b"));
        }

        [Test]
        public void PresentationGate_ClearAll_DropsRegisteredEpochs()
        {
            int epoch = PartyTravelPresentationGate.BeginPresentation();
            Assert.That(epoch, Is.GreaterThan(0));

            PartyTravelPresentationGate.ClearAll();
            Assert.DoesNotThrow(() => PartyTravelPresentationGate.SignalPresentationComplete(epoch));
        }
    }
}

