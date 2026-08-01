using System.Collections.Generic;
using EnglishQuest.Tests.Editor;
using Fusion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityServiceLocator;
using Assert = NUnit.Framework.Assert;

namespace EnglishQuest.Tests.Player
{
    [TestFixture]
    public class LocalPlayerReadinessTests
    {
        private readonly List<GameObject> _createdObjects = new();
        private readonly List<NetworkRunner> _runners = new();
        private readonly List<Scene> _createdScenes = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _runners.Count - 1; i >= 0; i--)
                LocalPlayerReadiness.Clear(_runners[i]);

            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                    UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
            }

            _createdObjects.Clear();
            _runners.Clear();

            for (int i = _createdScenes.Count - 1; i >= 0; i--)
            {
                Scene scene = _createdScenes[i];
                EditorTestSceneUtility.CloseScene(scene);
            }

            _createdScenes.Clear();
        }

        [Test]
        public void SceneService_StoresArgs_ForRunnerScene()
        {
            Scene scene = CreateScene("ReadinessScene");
            LocalPlayerReadinessProvider provider = CreateSceneReadinessProvider(scene);
            NetworkRunner runner = CreateRunner();
            var playerGo = new GameObject("Player");
            _createdObjects.Add(playerGo);

            provider.NotifyReadyForTests(runner, playerGo.AddComponent<NetworkObject>());

            Assert.That(provider.TryGet(out LocalPlayerReadyArgs args), Is.True);
            Assert.That(args.Runner, Is.SameAs(runner));
            Assert.That(args.Scene, Is.EqualTo(scene));
            Assert.That(ServiceLocator.TryGetForScene(scene, out ILocalPlayerReadiness resolved), Is.True);
            Assert.That(resolved, Is.SameAs(provider));
        }

        [Test]
        public void IsReady_IsFalse_WhenPlayerIsNotFusionSpawned()
        {
            Scene scene = CreateScene("ReadinessScene");
            LocalPlayerReadinessProvider provider = CreateSceneReadinessProvider(scene);
            NetworkRunner runner = CreateRunner();
            var playerGo = new GameObject("Player");
            _createdObjects.Add(playerGo);

            provider.NotifyReadyForTests(runner, playerGo.AddComponent<NetworkObject>());

            Assert.That(provider.TryGet(out _), Is.True);
            Assert.That(provider.IsReady, Is.False);
        }

        [Test]
        public void DifferentScenes_AreIsolated()
        {
            Scene sceneA = CreateScene("SceneA");
            Scene sceneB = CreateScene("SceneB");
            LocalPlayerReadinessProvider providerA = CreateSceneReadinessProvider(sceneA);
            LocalPlayerReadinessProvider providerB = CreateSceneReadinessProvider(sceneB);
            NetworkRunner runnerA = CreateRunner("RunnerA");
            NetworkRunner runnerB = CreateRunner("RunnerB");

            providerA.NotifyReadyForTests(runnerA);
            providerB.NotifyReadyForTests(runnerB);

            Assert.That(ServiceLocator.TryGetForScene(sceneA, out ILocalPlayerReadiness fromA), Is.True);
            Assert.That(ServiceLocator.TryGetForScene(sceneB, out ILocalPlayerReadiness fromB), Is.True);
            Assert.That(fromA.TryGet(out _), Is.True);
            Assert.That(fromB.TryGet(out _), Is.True);
            Assert.That(ServiceLocator.TryGetForScene(sceneA, out ILocalPlayerReadiness _), Is.True);
            Assert.That(ServiceLocator.TryGetForScene(sceneB, out ILocalPlayerReadiness __), Is.True);
            Assert.That(fromA, Is.Not.SameAs(fromB));
        }

        [Test]
        public void Clear_RemovesStoredStateForRunner()
        {
            Scene scene = CreateScene("ReadinessScene");
            LocalPlayerReadinessProvider provider = CreateSceneReadinessProvider(scene);
            NetworkRunner runner = CreateRunner();

            provider.NotifyReadyForTests(runner);
            LocalPlayerReadiness.Clear(runner);

            Assert.That(provider.TryGet(out _), Is.False);
        }

        [Test]
        public void Clear_OneRunner_DoesNotAffectDifferentSceneProvider()
        {
            Scene sceneA = CreateScene("SceneA");
            Scene sceneB = CreateScene("SceneB");
            LocalPlayerReadinessProvider providerA = CreateSceneReadinessProvider(sceneA);
            LocalPlayerReadinessProvider providerB = CreateSceneReadinessProvider(sceneB);
            NetworkRunner runnerA = CreateRunner("RunnerA");
            NetworkRunner runnerB = CreateRunner("RunnerB");

            providerA.NotifyReadyForTests(runnerA);
            providerB.NotifyReadyForTests(runnerB);

            LocalPlayerReadiness.Clear(runnerA);

            Assert.That(providerA.TryGet(out _), Is.False);
            Assert.That(providerB.TryGet(out _), Is.True);
        }

        private NetworkRunner CreateRunner(string name = "TestRunner")
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            var runner = go.AddComponent<NetworkRunner>();
            _runners.Add(runner);
            return runner;
        }

        private static Scene CreateScene(string name) =>
            EditorTestSceneUtility.CreateEmptyScene(name);

        private LocalPlayerReadinessProvider CreateSceneReadinessProvider(Scene scene)
        {
            var locatorGo = new GameObject("ServiceLocator [Scene]");
            _createdObjects.Add(locatorGo);
            SceneManager.MoveGameObjectToScene(locatorGo, scene);
            _createdScenes.Add(scene);

            locatorGo.AddComponent<ServiceLocator>();
            locatorGo.AddComponent<ServiceLocatorScene>().BootstrapOnDemand();

            Assert.That(ServiceLocator.TryGetForScene(scene, out ILocalPlayerReadiness readiness), Is.True);
            return (LocalPlayerReadinessProvider)readiness;
        }
    }
}

