using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EnglishQuest.Tests.WorldTravel
{
    [TestFixture]
    public class PlayerTravelArrivalTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerTravelArrival.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerTravelArrival.ClearAll();
        }

        [Test]
        public void SetPending_TryConsumeForScene_MatchingBuildIndex_ReturnsAndClears()
        {
            Scene scene = SceneManager.GetActiveScene();
            var destination = new WorldMapNodeData
            {
                id = "farm",
                worldPosition = new Vector3(10f, 0f, 5f),
                worldRotationEuler = new Vector3(0f, 90f, 0f)
            };

            PlayerTravelArrival.SetPending(null, "farm", scene.buildIndex, in destination);

            bool consumed = PlayerTravelArrival.TryConsumeForScene(
                null,
                scene,
                out PlayerTravelArrival.PendingArrival arrival);

            Assert.That(consumed, Is.True);
            Assert.That(arrival.SpawnNodeId, Is.EqualTo("farm"));
            Assert.That(arrival.TargetSceneBuildIndex, Is.EqualTo(scene.buildIndex));
            Assert.That(arrival.WorldPosition, Is.EqualTo(new Vector3(10f, 0f, 5f)));
            Assert.That(PlayerTravelArrival.HasPending, Is.False);
        }

        [Test]
        public void TryConsumeForScene_WrongBuildIndex_ReturnsFalse()
        {
            Scene scene = SceneManager.GetActiveScene();
            var destination = new WorldMapNodeData { id = "farm" };
            PlayerTravelArrival.SetPending(null, "farm", 99999, in destination);

            bool consumed = PlayerTravelArrival.TryConsumeForScene(null, scene, out _);

            Assert.That(consumed, Is.False);
            Assert.That(PlayerTravelArrival.HasPending, Is.True);
        }

        [Test]
        public void TryPeekForScene_DoesNotClearPending()
        {
            Scene scene = SceneManager.GetActiveScene();
            var destination = new WorldMapNodeData { id = "hub" };
            PlayerTravelArrival.SetPending(null, "hub", scene.buildIndex, in destination);

            bool peeked = PlayerTravelArrival.TryPeekForScene(
                null,
                scene,
                out PlayerTravelArrival.PendingArrival arrival);

            Assert.That(peeked, Is.True);
            Assert.That(arrival.SpawnNodeId, Is.EqualTo("hub"));
            Assert.That(PlayerTravelArrival.HasPending, Is.True);
        }

        [Test]
        public void Clear_RemovesPendingState()
        {
            var destination = new WorldMapNodeData { id = "hub" };
            PlayerTravelArrival.SetPending(null, "hub", 1, in destination);

            PlayerTravelArrival.Clear(null);

            Assert.That(PlayerTravelArrival.HasPending, Is.False);
        }

        [Test]
        public void ClearAll_RemovesAllPendingState()
        {
            var destination = new WorldMapNodeData { id = "hub" };
            PlayerTravelArrival.SetPending(null, "hub", 1, in destination);

            PlayerTravelArrival.ClearAll();

            Assert.That(PlayerTravelArrival.HasPending, Is.False);
        }

        [Test]
        public void TryResolveTransform_UsesWorldFallbackWhenNoSpawnPoint()
        {
            Scene scene = SceneManager.GetActiveScene();
            var arrival = new PlayerTravelArrival.PendingArrival
            {
                SpawnNodeId = "missing-node",
                WorldPosition = new Vector3(1f, 2f, 3f),
                WorldRotationEuler = new Vector3(0f, 45f, 0f),
                HasWorldFallback = true
            };

            bool resolved = PlayerTravelArrival.TryResolveTransform(
                in arrival,
                scene,
                out Vector3 position,
                out Quaternion rotation);

            Assert.That(resolved, Is.True);
            Assert.That(position, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(rotation.eulerAngles.y, Is.EqualTo(45f).Within(0.1f));
        }
    }
}

