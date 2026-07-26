using EnglishKingdom.QuestSystem;
using NUnit.Framework;
using UnityEngine;

namespace EnglishKingdom.Tests.QuestSystem
{
    public class QuestWorldTargetRegistryTests
    {
        QuestWorldTargetRegistry _registry;
        Transform _transformA;
        Transform _transformB;

        [SetUp]
        public void SetUp()
        {
            _registry = new QuestWorldTargetRegistry();
            _transformA = new GameObject("TargetA").transform;
            _transformB = new GameObject("TargetB").transform;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_transformA.gameObject);
            Object.DestroyImmediate(_transformB.gameObject);
        }

        [Test]
        public void Register_TryGetTransform_ReturnsRegisteredTransform()
        {
            _registry.Register(QuestObjectiveType.EnterArea, "zone_a", _transformA);

            Assert.IsTrue(_registry.TryGetTransform(QuestObjectiveType.EnterArea, "zone_a", out Transform result));
            Assert.AreEqual(_transformA, result);
        }

        [Test]
        public void Register_OverwritesExistingTargetForSameKey()
        {
            _registry.Register(QuestObjectiveType.Collect, "item_1", _transformA);
            _registry.Register(QuestObjectiveType.Collect, "item_1", _transformB);

            Assert.IsTrue(_registry.TryGetTransform(QuestObjectiveType.Collect, "item_1", out Transform result));
            Assert.AreEqual(_transformB, result);
        }

        [Test]
        public void Unregister_RemovesOnlyMatchingTransform()
        {
            _registry.Register(QuestObjectiveType.TalkToNpc, "npc_1", _transformA);
            _registry.Unregister(QuestObjectiveType.TalkToNpc, "npc_1", _transformB);

            Assert.IsTrue(_registry.TryGetTransform(QuestObjectiveType.TalkToNpc, "npc_1", out _));

            _registry.Unregister(QuestObjectiveType.TalkToNpc, "npc_1", _transformA);
            Assert.IsFalse(_registry.TryGetTransform(QuestObjectiveType.TalkToNpc, "npc_1", out _));
        }

        [Test]
        public void TryGetTransform_IsolatesObjectiveTypes()
        {
            _registry.Register(QuestObjectiveType.EnterArea, "shared_id", _transformA);
            _registry.Register(QuestObjectiveType.Collect, "shared_id", _transformB);

            Assert.IsTrue(_registry.TryGetTransform(QuestObjectiveType.EnterArea, "shared_id", out Transform area));
            Assert.IsTrue(_registry.TryGetTransform(QuestObjectiveType.Collect, "shared_id", out Transform collect));
            Assert.AreEqual(_transformA, area);
            Assert.AreEqual(_transformB, collect);
        }

        [Test]
        public void TryGetTransform_ReturnsFalseForDestroyedTransform()
        {
            var temp = new GameObject("Temp").transform;
            _registry.Register(QuestObjectiveType.CompleteMiniGame, "game_1", temp);
            Object.DestroyImmediate(temp.gameObject);

            Assert.IsFalse(_registry.TryGetTransform(QuestObjectiveType.CompleteMiniGame, "game_1", out _));
        }
    }
}
