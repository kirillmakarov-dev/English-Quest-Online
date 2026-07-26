using System.Collections.Generic;
using EnglishKingdom.Tests.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityServiceLocator;

namespace EnglishKingdom.Tests.ServiceLocatorLifecycle
{
    [TestFixture]
    public class ServiceLocatorSceneScopeTests
    {
        private readonly List<GameObject> _createdObjects = new();
        private readonly List<Scene> _createdScenes = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                    Object.DestroyImmediate(_createdObjects[i]);
            }

            _createdObjects.Clear();

            for (int i = _createdScenes.Count - 1; i >= 0; i--)
            {
                Scene scene = _createdScenes[i];
                EditorTestSceneUtility.CloseScene(scene);
            }

            _createdScenes.Clear();
        }

        [Test]
        public void For_OwnerHierarchy_ResolvesRegisteredService()
        {
            var locatorGo = CreateObject("Locator");
            var locator = locatorGo.AddComponent<ServiceLocator>();
            var ownerGo = CreateObject("Owner");
            ownerGo.transform.SetParent(locatorGo.transform);
            var owner = ownerGo.AddComponent<SceneScopeTestService>();

            locator.Register<SceneScopeTestService>(owner);

            Assert.That(ServiceLocator.For(owner).TryGet(out SceneScopeTestService resolved), Is.True);
            Assert.That(resolved, Is.SameAs(owner));
        }

        [Test]
        public void For_DifferentOwnersWithSeparateLocators_AreIsolated()
        {
            var locatorGoA = CreateObject("LocatorA");
            var locatorA = locatorGoA.AddComponent<ServiceLocator>();
            var ownerGoA = CreateObject("OwnerA");
            ownerGoA.transform.SetParent(locatorGoA.transform);
            var ownerA = ownerGoA.AddComponent<SceneScopeTestService>();
            ownerA.Label = "A";
            locatorA.Register<SceneScopeTestService>(ownerA);

            var locatorGoB = CreateObject("LocatorB");
            var locatorB = locatorGoB.AddComponent<ServiceLocator>();
            var ownerGoB = CreateObject("OwnerB");
            ownerGoB.transform.SetParent(locatorGoB.transform);
            var ownerB = ownerGoB.AddComponent<SceneScopeTestService>();
            ownerB.Label = "B";
            locatorB.Register<SceneScopeTestService>(ownerB);

            Assert.That(ServiceLocator.For(ownerA).TryGet(out SceneScopeTestService resolvedA), Is.True);
            Assert.That(ServiceLocator.For(ownerB).TryGet(out SceneScopeTestService resolvedB), Is.True);
            Assert.That(resolvedA.Label, Is.EqualTo("A"));
            Assert.That(resolvedB.Label, Is.EqualTo("B"));
        }

        [Test]
        public void ResetForPlayModeTests_ClearsPlayModeLocatorState()
        {
            var globalGo = CreateObject("GlobalLocator", typeof(ServiceLocatorGlobal));
            var globalLocator = globalGo.GetComponent<ServiceLocator>();
            var serviceGo = CreateObject("GlobalService");
            var service = serviceGo.AddComponent<SceneScopeTestService>();
            globalLocator.Register<SceneScopeTestService>(service);

            ServiceLocator.ResetForPlayModeTests();

            Assert.That(ServiceLocator.Global, Is.Null);
        }

        [Test]
        public void For_InteractorInDifferentScene_ResolvesSceneService()
        {
            Scene sceneA = CreateScene("SceneA");
            Scene sceneB = CreateScene("SceneB");

            var locatorGo = CreateObjectInScene(sceneA, "SceneLocator");
            var locator = locatorGo.AddComponent<ServiceLocator>();
            locatorGo.AddComponent<ServiceLocatorScene>().BootstrapOnDemand();

            var providerGo = CreateObjectInScene(sceneA, "LockProvider");
            var provider = providerGo.AddComponent<SceneScopeTestService>();
            provider.Label = "Locks";
            ServiceLocator.ForSceneOf(provider).Register<SceneScopeTestService>(provider);

            var uiGo = CreateObjectInScene(sceneB, "UI");
            var uiConsumer = uiGo.AddComponent<SceneScopeTestService>();
            var interactorGo = CreateObjectInScene(sceneA, "Interactor");
            var interactor = interactorGo.AddComponent<SceneScopeTestService>();

            ServiceLocator sceneBLocator = ServiceLocator.For(uiConsumer);
            Assert.That(
                sceneBLocator == null || !sceneBLocator.TryGet(out SceneScopeTestService _),
                Is.True);
            Assert.That(ServiceLocator.For(interactor).TryGet(out SceneScopeTestService resolved), Is.True);
            Assert.That(resolved.Label, Is.EqualTo("Locks"));
        }

        [Test]
        public void RefreshForScene_ChildLocator_MapsToDestinationScene()
        {
            Scene sourceScene = CreateScene("Source");
            Scene destinationScene = CreateScene("Destination");

            var rootGo = CreateObjectInScene(sourceScene, "Root");
            var childGo = new GameObject("ChildLocator");
            _createdObjects.Add(childGo);
            childGo.transform.SetParent(rootGo.transform);
            var locator = childGo.AddComponent<ServiceLocator>();
            childGo.AddComponent<ServiceLocatorScene>().BootstrapOnDemand();

            SceneManager.MoveGameObjectToScene(rootGo, destinationScene);
            ServiceLocator.RefreshForScene(destinationScene);

            var consumerGo = CreateObjectInScene(destinationScene, "Consumer");
            var consumer = consumerGo.AddComponent<SceneScopeTestService>();
            locator.Register<SceneScopeTestService>(consumer);

            Assert.That(ServiceLocator.ForSceneOf(consumer).TryGet(out SceneScopeTestService resolved), Is.True);
            Assert.That(resolved, Is.SameAs(consumer));
        }

        [Test]
        public void RefreshForScene_RegistersLocalPlayerReadinessProvider()
        {
            Scene scene = CreateScene("ReadinessRefresh");
            var locatorGo = new GameObject("ServiceLocator [Scene]");
            _createdObjects.Add(locatorGo);
            SceneManager.MoveGameObjectToScene(locatorGo, scene);
            _createdScenes.Add(scene);

            locatorGo.AddComponent<ServiceLocator>();
            locatorGo.AddComponent<ServiceLocatorScene>();

            ServiceLocator.RefreshForScene(scene);

            Assert.That(ServiceLocator.TryGetForScene(scene, out ILocalPlayerReadiness readiness), Is.True);
            Assert.That(readiness, Is.Not.Null);
        }

        [Test]
        public void RefreshForScene_SceneBootstrapper_OwnsSceneMapping()
        {
            Scene scene = CreateScene("BootstrapPriority");

            var plainGo = CreateObjectInScene(scene, "PlainLocator");
            plainGo.AddComponent<ServiceLocator>();

            var bootstrapGo = CreateObjectInScene(scene, "SceneLocator");
            bootstrapGo.AddComponent<ServiceLocator>();
            bootstrapGo.AddComponent<ServiceLocatorScene>();

            ServiceLocator.RefreshForScene(scene);

            Assert.That(ServiceLocator.TryGetContainerForScene(scene, out ServiceLocator container), Is.True);
            Assert.That(container, Is.SameAs(bootstrapGo.GetComponent<ServiceLocator>()));
            Assert.That(ServiceLocator.TryGetForScene(scene, out ILocalPlayerReadiness readiness), Is.True);
        }

        [Test]
        public void ForSceneOf_DeregisterIfRegistered_RemovesSceneRegistrationOnly()
        {
            var hierarchyLocatorGo = CreateObject("HierarchyLocator");
            var hierarchyLocator = hierarchyLocatorGo.AddComponent<ServiceLocator>();

            var providerGo = CreateObject("Provider");
            providerGo.transform.SetParent(hierarchyLocatorGo.transform);
            var provider = providerGo.AddComponent<SceneScopeTestService>();
            provider.Label = "Hierarchy";

            var sceneLocatorGo = CreateObject("SceneLocator");
            var sceneLocator = sceneLocatorGo.AddComponent<ServiceLocator>();
            sceneLocatorGo.AddComponent<ServiceLocatorScene>().BootstrapOnDemand();

            hierarchyLocator.Register<SceneScopeTestService>(provider);
            ServiceLocator.ForSceneOf(provider).Register<SceneScopeTestService>(provider);

            ServiceLocator.ForSceneOf(provider).DeregisterIfRegistered<SceneScopeTestService>();

            Assert.That(ServiceLocator.For(provider).TryGet(out SceneScopeTestService resolved), Is.True);
            Assert.That(resolved.Label, Is.EqualTo("Hierarchy"));
            Assert.That(ServiceLocator.ForSceneOf(provider).TryGet(out SceneScopeTestService _), Is.False);
        }

        [Test]
        public void TryGet_MultipleSceneLocators_DoesNotStackOverflow()
        {
            var hierarchyLocatorGo = CreateObject("HierarchyLocator");
            var hierarchyLocator = hierarchyLocatorGo.AddComponent<ServiceLocator>();

            var sceneLocatorGo = CreateObject("SceneLocator");
            sceneLocatorGo.AddComponent<ServiceLocator>();
            sceneLocatorGo.AddComponent<ServiceLocatorScene>().BootstrapOnDemand();
            var sceneLocator = sceneLocatorGo.GetComponent<ServiceLocator>();

            Assert.That(() => sceneLocator.TryGet(out SceneScopeTestService _), Throws.Nothing);
            Assert.That(() => hierarchyLocator.TryGet(out SceneScopeTestService __), Throws.Nothing);
        }

        [Test]
        public void ForSceneOf_WhenSceneContainerRegistered_ReturnsContainerForSameComponent()
        {
            var sceneLocatorGo = CreateObject("SceneLocator");
            var sceneLocator = sceneLocatorGo.AddComponent<ServiceLocator>();
            sceneLocatorGo.AddComponent<ServiceLocatorScene>().BootstrapOnDemand();

            Assert.That(ServiceLocator.ForSceneOf(sceneLocator), Is.SameAs(sceneLocator));
        }

        [Test]
        public void GlobalLocalPlayerService_DoesNotSatisfySceneScopedConsumer()
        {
            var globalGo = CreateObject("GlobalLocator", typeof(ServiceLocatorGlobal));
            globalGo.GetComponent<ServiceLocatorGlobal>().BootstrapOnDemand();
            var globalLocator = globalGo.GetComponent<ServiceLocator>();
            var globalServiceGo = CreateObject("GlobalLock");
            var globalService = globalServiceGo.AddComponent<SceneScopeTestService>();
            globalService.Label = "Global";
            globalLocator.Register<SceneScopeTestService>(globalService);

            var sceneLocatorGo = CreateObject("SceneLocator");
            sceneLocatorGo.AddComponent<ServiceLocator>();
            sceneLocatorGo.AddComponent<ServiceLocatorScene>().BootstrapOnDemand();

            var consumerGo = CreateObject("Consumer");
            var consumer = consumerGo.AddComponent<SceneScopeTestService>();

            Assert.That(ServiceLocator.For(consumer).TryGet(out SceneScopeTestService resolved), Is.True);
            Assert.That(resolved.Label, Is.EqualTo("Global"));

            var sceneServiceGo = CreateObject("SceneLock");
            var sceneService = sceneServiceGo.AddComponent<SceneScopeTestService>();
            sceneService.Label = "Scene";
            ServiceLocator.ForSceneOf(sceneService).Register<SceneScopeTestService>(sceneService);

            Assert.That(ServiceLocator.For(consumer).TryGet(out SceneScopeTestService sceneResolved), Is.True);
            Assert.That(sceneResolved.Label, Is.EqualTo("Scene"));
        }

        private GameObject CreateObject(string name, System.Type extraComponent = null)
        {
            var go = extraComponent == null
                ? new GameObject(name)
                : new GameObject(name, extraComponent);
            _createdObjects.Add(go);
            return go;
        }

        private Scene CreateScene(string name)
        {
            Scene scene = EditorTestSceneUtility.CreateEmptyScene(name);
            _createdScenes.Add(scene);
            return scene;
        }

        private GameObject CreateObjectInScene(Scene scene, string name)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            SceneManager.MoveGameObjectToScene(go, scene);
            if (!_createdScenes.Contains(scene))
                _createdScenes.Add(scene);
            return go;
        }

        private sealed class SceneScopeTestService : MonoBehaviour
        {
            public string Label;
        }
    }
}
