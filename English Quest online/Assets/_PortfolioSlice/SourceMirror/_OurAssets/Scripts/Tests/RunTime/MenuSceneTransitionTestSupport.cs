using System;
using System.Collections;
using System.Text;
using Cysharp.Threading.Tasks;
using Fusion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityServiceLocator;

namespace EnglishQuest.Tests.RunTime
{
    /// <summary>
    /// Helpers for Play Mode tests that verify Menu → gameplay scene transitions
    /// unload Menu while keeping the Fusion-loaded target scene active.
    /// </summary>
    internal static class MenuSceneTransitionTestSupport
    {
        public const string PreLoadSceneName = "PreLoad";
        public const string MenuSceneName = "Menu";
        public const string OpenWorldSceneName = "OpenWorld";
        public const string HostSessionSceneName = FusionMultiPeerTestSupport.CombatTestSceneName;
        public const string OpenWorldScenePath = "Assets/_OurAssets/Scenes/OpenWorld.unity";
        public const string HostSessionScenePath = "Assets/_OurAssets/Scenes/TestScenes/CombatTest.unity";
        public const float BootstrapGraceSeconds = 3f;
        public const float NetworkTransitionTimeoutSeconds = 90f;
        public const float LocalPlayerReadyTimeoutSeconds = 45f;

        public static IEnumerator BootstrapMenuState()
        {
            TestDebugLog.Info("MenuTransition", "BootstrapMenuState starting.");
            ServiceLocator.ResetForPlayModeTests();
            yield return FusionMultiPeerTestSupport.ShutdownExistingRunners();
            yield return null;

            AsyncOperation preloadOperation = SceneManager.LoadSceneAsync(PreLoadSceneName, LoadSceneMode.Single);
            Assert.That(preloadOperation, Is.Not.Null,
                $"Scene '{PreLoadSceneName}' must be in the active Unity build profile scene list.");

            while (!preloadOperation.isDone)
                yield return null;

            yield return new WaitForSeconds(BootstrapGraceSeconds);

            if (!IsSceneLoaded(MenuSceneName))
            {
                AsyncOperation menuOperation = SceneManager.LoadSceneAsync(MenuSceneName, LoadSceneMode.Additive);
                Assert.That(menuOperation, Is.Not.Null,
                    $"Scene '{MenuSceneName}' must be in the active Unity build profile scene list.");

                while (!menuOperation.isDone)
                    yield return null;
            }

            Assert.That(IsSceneLoaded(MenuSceneName), Is.True,
                $"Expected '{MenuSceneName}' to be loaded after bootstrap. Loaded scenes: {FormatLoadedScenes()}");

            Assert.That(TryGetNetworkSession(out _), Is.True,
                "INetworkSessionService must be available after loading PreLoad.");

            TestDebugLog.LoadedScenes("MenuTransition", "Bootstrap complete");
            TestDebugLog.AllRunners("MenuTransition");
        }

        public static IEnumerator RunNetworkTransition(Func<UniTask> transition, Action<Exception> onError)
        {
            TestDebugLog.Info("MenuTransition", "RunNetworkTransition starting.");
            UniTask task;
            try
            {
                task = transition();
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
                yield break;
            }

            float elapsed = 0f;
            float logInterval = 5f;
            float nextLogAt = 0f;
            while (!task.GetAwaiter().IsCompleted && elapsed < NetworkTransitionTimeoutSeconds)
            {
                if (elapsed >= nextLogAt)
                {
                    nextLogAt += logInterval;
                    TestDebugLog.LoadedScenes("MenuTransition", $"Transition in progress ({elapsed:0.0}s)");
                    TestDebugLog.AllRunners("MenuTransition");
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            TestDebugLog.Info("MenuTransition",
                $"RunNetworkTransition done: completed={task.GetAwaiter().IsCompleted}, elapsed={elapsed:0.1}s.");

            if (!task.GetAwaiter().IsCompleted)
            {
                onError?.Invoke(new TimeoutException(
                    $"Network transition timed out after {NetworkTransitionTimeoutSeconds:0}s."));
                yield break;
            }

            try
            {
                task.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }
        }

        public static IEnumerator DisconnectNetworkSession()
        {
            if (!TryGetNetworkSession(out INetworkSessionService network))
            {
                yield return FusionMultiPeerTestSupport.ShutdownExistingRunners();
                yield break;
            }

            Exception error = null;
            yield return RunNetworkTransition(network.Disconnect, ex => error = ex);

            if (error != null)
                Debug.LogWarning($"[MenuSceneTransitionTestSupport] Disconnect failed: {error.Message}");

            yield return FusionMultiPeerTestSupport.ShutdownExistingRunners();
        }

        public static bool IsSceneLoaded(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name == sceneName)
                    return true;
            }

            return false;
        }

        public static bool TryResolveBuildIndex(string scenePath, out int buildIndex)
        {
            buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);
            return buildIndex >= 0;
        }

        public static bool TryGetNetworkSession(out INetworkSessionService network)
        {
            GameNetworkManager manager = UnityEngine.Object.FindFirstObjectByType<GameNetworkManager>();
            network = manager;
            return network != null;
        }

        public static string FormatLoadedScenes()
        {
            var builder = new StringBuilder();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                if (builder.Length > 0)
                    builder.Append(", ");

                builder.Append(scene.name);
                if (SceneManager.GetActiveScene() == scene)
                    builder.Append(" (active)");
            }

            return builder.Length == 0 ? "<none>" : builder.ToString();
        }

        public static Scene ResolveGameplayScene(string gameplaySceneName)
        {
            Scene byName = SceneManager.GetSceneByName(gameplaySceneName);
            if (byName.IsValid() && byName.isLoaded)
                return byName;

            int buildIndex = ResolveGameplaySceneBuildIndex(gameplaySceneName);
            if (buildIndex >= 0)
            {
                Scene byBuildIndex = SceneManager.GetSceneByBuildIndex(buildIndex);
                if (byBuildIndex.IsValid() && byBuildIndex.isLoaded)
                    return byBuildIndex;
            }

            if (TryGetNetworkSession(out INetworkSessionService network))
            {
                NetworkRunner runner = network.Runner;
                if (runner != null
                    && runner.IsRunning
                    && FusionMultiPeerTestSupport.IsRunnerReadyForSpawn(runner))
                {
                    Scene simulationScene = runner.SimulationUnityScene;
                    if (simulationScene.IsValid() && simulationScene.isLoaded)
                        return simulationScene;
                }
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isLoaded
                && (activeScene.name == gameplaySceneName
                    || (buildIndex >= 0 && activeScene.buildIndex == buildIndex)))
            {
                return activeScene;
            }

            return default;
        }

        private static int ResolveGameplaySceneBuildIndex(string gameplaySceneName)
        {
            if (gameplaySceneName == MenuSceneTransitionTestSupport.HostSessionSceneName)
                return SceneUtility.GetBuildIndexByScenePath(MenuSceneTransitionTestSupport.HostSessionScenePath);

            if (gameplaySceneName == OpenWorldSceneName)
                return SceneUtility.GetBuildIndexByScenePath("Assets/_OurAssets/Scenes/OpenWorld.unity");

            return SceneUtility.GetBuildIndexByScenePath($"Assets/_OurAssets/Scenes/{gameplaySceneName}.unity");
        }

        public static void AssertGameplaySceneAdopted(string gameplaySceneName, string sceneThatMustBeUnloaded)
        {
            Assert.That(
                IsSceneLoaded(sceneThatMustBeUnloaded),
                Is.False,
                $"Scene '{sceneThatMustBeUnloaded}' should be unloaded. Loaded scenes: {FormatLoadedScenes()}");

            Scene gameplayScene = ResolveGameplayScene(gameplaySceneName);
            Assert.That(gameplayScene.IsValid() && gameplayScene.isLoaded, Is.True,
                $"Scene '{gameplaySceneName}' should be loaded. Loaded scenes: {FormatLoadedScenes()}");

            Assert.That(
                SceneManager.GetActiveScene().buildIndex == gameplayScene.buildIndex,
                Is.True,
                $"Gameplay scene '{gameplaySceneName}' should be active. Loaded scenes: {FormatLoadedScenes()}");
        }

        public static IEnumerator WaitForMenuSessionLocalPlayer(string gameplaySceneName)
        {
            TestDebugLog.Info("MenuTransition", $"WaitForMenuSessionLocalPlayer scene='{gameplaySceneName}'.");
            if (!TryGetNetworkSession(out INetworkSessionService network))
            {
                Assert.Fail("INetworkSessionService not available while waiting for local player spawn.");
                yield break;
            }

            NetworkRunner runner = network.Runner;
            Assert.That(runner, Is.Not.Null, "Menu session runner should exist after gameplay entry.");
            Assert.That(runner.IsRunning, Is.True, "Menu session runner should be running after gameplay entry.");

            float elapsed = 0f;
            float logInterval = 5f;
            float nextLogAt = 0f;
            while (elapsed < LocalPlayerReadyTimeoutSeconds)
            {
                Scene gameplayScene = ResolveGameplayScene(gameplaySceneName);
                if (gameplayScene.IsValid()
                    && FusionMultiPeerTestSupport.IsRunnerReadyForSpawn(runner))
                {
                    TestDebugLog.Info("MenuTransition",
                        $"Local player ready in '{gameplayScene.name}' after {elapsed:0.1}s.");
                    yield break;
                }

                if (elapsed >= nextLogAt)
                {
                    nextLogAt += logInterval;
                    TestDebugLog.RunnerState("MenuTransition", runner, "waiting for local player");
                    TestDebugLog.LoadedScenes("MenuTransition", $"waiting ({elapsed:0.0}s)");
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Scene resolvedScene = ResolveGameplayScene(gameplaySceneName);
            NetworkObject localPlayer = runner.GetPlayerObject(runner.LocalPlayer);
            Assert.That(localPlayer, Is.Not.Null,
                $"Local player object should spawn in '{gameplaySceneName}' after menu entry.");
            Assert.That(
                resolvedScene.IsValid() && LocalPlayerReadiness.IsReady(runner, resolvedScene),
                Is.True,
                $"Local player lifecycle should be ready in '{gameplaySceneName}' after menu entry.");
        }

        public static void AssertLocalPlayerSpawnedInScene(string gameplaySceneName)
        {
            if (!TryGetNetworkSession(out INetworkSessionService network))
            {
                Assert.Fail("INetworkSessionService not available while asserting local player spawn.");
                return;
            }

            NetworkRunner runner = network.Runner;
            Scene gameplayScene = ResolveGameplayScene(gameplaySceneName);

            NetworkObject localPlayer = runner.GetPlayerObject(runner.LocalPlayer);
            Assert.That(localPlayer, Is.Not.Null,
                $"Local player object should exist in '{gameplaySceneName}'.");

            Assert.That(
                PlayerArrivalUtility.IsUsablePlayerObject(localPlayer, gameplayScene),
                Is.True,
                $"Local player object should belong to active gameplay scene '{gameplaySceneName}'.");

            Assert.That(
                LocalPlayerReadiness.IsReady(runner, gameplayScene),
                Is.True,
                $"Local player lifecycle should be ready in '{gameplaySceneName}'.");
        }
    }
}

