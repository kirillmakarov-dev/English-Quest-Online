using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EnglishQuest.Tests.Player
{
    [TestFixture]
    public class PlayerSpawnPointTests
    {
        private readonly List<GameObject> _createdObjects = new();

        [SetUp]
        public void SetUp()
        {
            PlayerSpawnPoint.SpawnPoints.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
                Object.DestroyImmediate(_createdObjects[i]);

            _createdObjects.Clear();
            PlayerSpawnPoint.SpawnPoints.Clear();
        }

        [Test]
        public void TryGetNearestSpawnPoint_ReturnsClosestMarker()
        {
            CreateSpawnPoint("Far", new Vector3(100f, 0f, 0f), Quaternion.identity);
            CreateSpawnPoint("Near", new Vector3(2f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));
            CreateSpawnPoint("Mid", new Vector3(20f, 0f, 0f), Quaternion.identity);

            bool found = PlayerSpawnPoint.TryGetNearestSpawnPoint(Vector3.zero, out Vector3 position, out Quaternion rotation);

            Assert.IsTrue(found);
            Assert.AreEqual(new Vector3(2f, 0f, 0f), position);
            Assert.AreEqual(Quaternion.Euler(0f, 90f, 0f).eulerAngles, rotation.eulerAngles);
        }

        [Test]
        public void TryGetRandomSpawnPoint_ReturnsRegisteredPoint()
        {
            CreateSpawnPoint("Only", new Vector3(4f, 1f, 2f), Quaternion.identity);

            bool found = PlayerSpawnPoint.TryGetRandomSpawnPoint(out Vector3 position, out _);

            Assert.IsTrue(found);
            Assert.AreEqual(new Vector3(4f, 1f, 2f), position);
        }

        [Test]
        public void TryGetNearestSpawnPoint_WhenEmpty_ReturnsFalse()
        {
            bool found = PlayerSpawnPoint.TryGetNearestSpawnPoint(Vector3.zero, out Vector3 position, out Quaternion rotation);

            Assert.IsFalse(found);
            Assert.AreEqual(Vector3.zero, position);
            Assert.AreEqual(Quaternion.identity, rotation);
        }

        [Test]
        public void TryGetSpawnForNode_MatchingTravelNodeId_ReturnsPoint()
        {
            Scene scene = CreateSpawnPoint("FarmSpawn", new Vector3(8f, 0f, 2f), Quaternion.identity, "farm").gameObject.scene;

            bool found = PlayerSpawnPoint.TryGetSpawnForNode("farm", scene, out Vector3 position, out _);

            Assert.IsTrue(found);
            Assert.AreEqual(new Vector3(8f, 0f, 2f), position);
        }

        [Test]
        public void TryResolveTravelSpawn_FallsBackToRandomSceneSpawn()
        {
            Scene scene = CreateSpawnPoint("Fallback", new Vector3(3f, 0f, 1f), Quaternion.identity).gameObject.scene;

            bool found = PlayerSpawnPoint.TryResolveTravelSpawn("unknown-node", scene, out Vector3 position, out _);

            Assert.IsTrue(found);
            Assert.AreEqual(new Vector3(3f, 0f, 1f), position);
        }

        [Test]
        public void TryGetSpawnPointForPlayer_UsesStableSortedOrderPerScene()
        {
            Scene scene = CreateSpawnPoint("Spawn Point - Player Two", new Vector3(6f, 0f, 0f), Quaternion.identity).gameObject.scene;
            CreateSpawnPoint("Spawn Point - Player One", new Vector3(2f, 0f, 0f), Quaternion.identity);

            bool foundFirst = PlayerSpawnPoint.TryGetSpawnPointForPlayer(scene, 1, out Vector3 firstPosition, out _);
            bool foundSecond = PlayerSpawnPoint.TryGetSpawnPointForPlayer(scene, 2, out Vector3 secondPosition, out _);

            Assert.IsTrue(foundFirst);
            Assert.IsTrue(foundSecond);
            Assert.AreEqual(new Vector3(2f, 0f, 0f), firstPosition);
            Assert.AreEqual(new Vector3(6f, 0f, 0f), secondPosition);
        }

        private PlayerSpawnPoint CreateSpawnPoint(string name, Vector3 position, Quaternion rotation, string travelNodeId = null)
        {
            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(position, rotation);
            var point = go.AddComponent<PlayerSpawnPoint>();
            if (!string.IsNullOrEmpty(travelNodeId))
                SetTravelNodeId(point, travelNodeId);

            PlayerSpawnPoint.SpawnPoints.Add(point);
            _createdObjects.Add(go);
            return point;
        }

        private static void SetTravelNodeId(PlayerSpawnPoint point, string travelNodeId)
        {
            FieldInfo field = typeof(PlayerSpawnPoint).GetField(
                "_travelNodeId",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(point, travelNodeId);
        }
    }
}

