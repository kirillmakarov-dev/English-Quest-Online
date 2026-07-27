using System.Collections.Generic;
using EnglishQuest.QuestSystem;
using NUnit.Framework;
using UnityEngine;
using UnityServiceLocator;

namespace EnglishQuest.Tests.QuestSystem
{
    [TestFixture]
    public class QuestWorldTargetRegistrationTimingTests
    {
        readonly List<GameObject> _created = new();
        GameObject _serviceLocatorGo;

        [SetUp]
        public void SetUp()
        {
            _serviceLocatorGo = new GameObject("ServiceLocator");
            _created.Add(_serviceLocatorGo);
            QuestSystemTestSupport.CreateServiceLocator(_serviceLocatorGo);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _created.Clear();
            QuestSystemTestSupport.ClearGlobalLocator();
        }

        [Test]
        public void TryRegister_WithoutRegistry_DoesNotRegisterTarget()
        {
            var markerGo = new GameObject("AreaMarker");
            _created.Add(markerGo);
            DummyHost host = markerGo.AddComponent<DummyHost>();

            QuestWorldTargetRegistration.TryRegister(
                host,
                QuestObjectiveType.EnterArea,
                "The Oathstone Bridge");

            Assert.IsFalse(
                ServiceLocator.For(host).TryGet(out IQuestWorldTargetRegistry _),
                "Registry should be missing before early bootstrap.");
        }

        [Test]
        public void TryRegister_AfterEarlyRegistrarAwake_RegistersTarget()
        {
            var hostGo = new GameObject("QuestHost");
            _created.Add(hostGo);
            QuestSystemTestSupport.CreateServiceLocator(hostGo);

            QuestWorldTargetRegistrar registrar = hostGo.AddComponent<QuestWorldTargetRegistrar>();
            QuestSystemTestSupport.InvokeAwake(registrar);

            Assert.IsTrue(
                ServiceLocator.For(registrar).TryGet(out IQuestWorldTargetRegistry registry));

            var markerGo = new GameObject("AreaMarker");
            _created.Add(markerGo);
            markerGo.transform.SetParent(hostGo.transform);
            DummyHost host = markerGo.AddComponent<DummyHost>();

            QuestWorldTargetRegistration.TryRegister(
                host,
                QuestObjectiveType.EnterArea,
                "The Oathstone Bridge");

            Assert.IsTrue(
                registry.TryGetTransform(
                    QuestObjectiveType.EnterArea,
                    "The Oathstone Bridge",
                    out Transform target));
            Assert.AreEqual(markerGo.transform, target);
        }

        [Test]
        public void SecondRegistrar_DoesNotReplaceOrDeregisterOwnedRegistry()
        {
            var hostGo = new GameObject("QuestHost");
            _created.Add(hostGo);
            QuestSystemTestSupport.CreateServiceLocator(hostGo);

            QuestWorldTargetRegistrar first = hostGo.AddComponent<QuestWorldTargetRegistrar>();
            QuestSystemTestSupport.InvokeAwake(first);

            Assert.IsTrue(
                ServiceLocator.For(first).TryGet(out IQuestWorldTargetRegistry firstRegistry));

            var secondGo = new GameObject("SecondRegistrar");
            _created.Add(secondGo);
            secondGo.transform.SetParent(hostGo.transform);
            QuestWorldTargetRegistrar second = secondGo.AddComponent<QuestWorldTargetRegistrar>();
            QuestSystemTestSupport.InvokeAwake(second);

            Assert.IsTrue(
                ServiceLocator.For(first).TryGet(out IQuestWorldTargetRegistry afterSecond));
            Assert.AreSame(firstRegistry, afterSecond);

            Object.DestroyImmediate(second);

            Assert.IsTrue(
                ServiceLocator.For(first).TryGet(out IQuestWorldTargetRegistry afterDestroy),
                "Destroying a non-owning registrar must not remove the registry.");
            Assert.AreSame(firstRegistry, afterDestroy);
        }

        class DummyHost : MonoBehaviour
        {
        }
    }
}

