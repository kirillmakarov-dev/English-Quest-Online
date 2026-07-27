using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace EnglishQuest.Tests.WorldTravel
{
    [TestFixture]
    public class WorldMapDefinitionSOTests
    {
        private WorldMapDefinitionSO _map;

        [SetUp]
        public void SetUp()
        {
            _map = ScriptableObject.CreateInstance<WorldMapDefinitionSO>();
            SetNodes(new List<WorldMapNodeData>
            {
                new() { id = "hub", displayName = "Hub" },
                new() { id = "farm", displayName = "Farm" }
            });
            SetRoutes(new List<WorldMapRouteData>
            {
                new() { fromNodeId = "hub", toNodeId = "farm", travelDuration = 3f },
                new() { fromNodeId = "farm", toNodeId = "hub", travelDuration = 2f }
            });
        }

        [TearDown]
        public void TearDown()
        {
            if (_map != null)
                Object.DestroyImmediate(_map);
        }

        [Test]
        public void TryGetNode_ExistingId_ReturnsNode()
        {
            bool found = _map.TryGetNode("farm", out WorldMapNodeData node);

            Assert.That(found, Is.True);
            Assert.That(node.id, Is.EqualTo("farm"));
            Assert.That(node.displayName, Is.EqualTo("Farm"));
        }

        [Test]
        public void TryGetNode_MissingId_ReturnsFalse()
        {
            bool found = _map.TryGetNode("missing", out WorldMapNodeData node);

            Assert.That(found, Is.False);
            Assert.That(node.id, Is.Null);
        }

        [Test]
        public void TryGetRoute_ExistingRoute_ReturnsRoute()
        {
            bool found = _map.TryGetRoute("hub", "farm", out WorldMapRouteData route);

            Assert.That(found, Is.True);
            Assert.That(route.fromNodeId, Is.EqualTo("hub"));
            Assert.That(route.toNodeId, Is.EqualTo("farm"));
            Assert.That(route.travelDuration, Is.EqualTo(3f));
        }

        [Test]
        public void GetReachableNodeIds_ReturnsOutgoingRoutes()
        {
            var reachable = new List<string>(_map.GetReachableNodeIds("hub"));

            Assert.That(reachable, Has.Count.EqualTo(1));
            Assert.That(reachable[0], Is.EqualTo("farm"));
        }

        [Test]
        public void RouteData_DurationOrDefault_UsesFallbackWhenZero()
        {
            var route = new WorldMapRouteData { travelDuration = 0f };

            Assert.That(route.DurationOrDefault, Is.EqualTo(2f));
        }

        private void SetNodes(List<WorldMapNodeData> nodes)
        {
            typeof(WorldMapDefinitionSO)
                .GetField("_nodes", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_map, nodes);
        }

        private void SetRoutes(List<WorldMapRouteData> routes)
        {
            typeof(WorldMapDefinitionSO)
                .GetField("_routes", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(_map, routes);
        }
    }
}

