using System;
using System.Collections;
using EnglishQuest.Tests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EnglishQuest.Tests.RunTime
{
    /// <summary>
    /// Play Mode regression tests for Menu → networked gameplay scene transitions.
    /// Verifies the target gameplay scene stays loaded, Menu is unloaded, and the local player spawns.
    /// </summary>
    [Category(TestCategories.Integration)]
    public class MenuToGameplaySceneTransitionTest
    {
        [UnityTest]
        public IEnumerator JoinOpenWorld_FromMenu_UnloadsMenuAndKeepsOpenWorld() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(JoinOpenWorldTestBody());

        [UnityTest]
        public IEnumerator StartSharedSession_FromMenu_UnloadsMenuAndKeepsHostScene() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(StartSharedSessionTestBody());

        private static IEnumerator JoinOpenWorldTestBody()
        {
            yield return MenuSceneTransitionTestSupport.BootstrapMenuState();

            if (!TryGetNetworkSession(out INetworkSessionService network))
                yield break;

            int openWorldBuildIndex;
            Assert.That(
                MenuSceneTransitionTestSupport.TryResolveBuildIndex(
                    MenuSceneTransitionTestSupport.OpenWorldScenePath,
                    out openWorldBuildIndex),
                Is.True,
                $"Open World scene must be in the active build profile: {MenuSceneTransitionTestSupport.OpenWorldScenePath}");

            Exception error = null;
            yield return MenuSceneTransitionTestSupport.RunNetworkTransition(
                () => network.JoinOpenWorldAsync(MenuSceneTransitionTestSupport.OpenWorldScenePath),
                ex => error = ex);

            if (error != null)
            {
                Assert.Fail($"JoinOpenWorldAsync failed: {error.Message}");
                yield break;
            }

            MenuSceneTransitionTestSupport.AssertGameplaySceneAdopted(
                MenuSceneTransitionTestSupport.OpenWorldSceneName,
                MenuSceneTransitionTestSupport.MenuSceneName);

            yield return MenuSceneTransitionTestSupport.WaitForMenuSessionLocalPlayer(
                MenuSceneTransitionTestSupport.OpenWorldSceneName);
            MenuSceneTransitionTestSupport.AssertLocalPlayerSpawnedInScene(
                MenuSceneTransitionTestSupport.OpenWorldSceneName);

            yield return MenuSceneTransitionTestSupport.DisconnectNetworkSession();
        }

        private static IEnumerator StartSharedSessionTestBody()
        {
            yield return MenuSceneTransitionTestSupport.BootstrapMenuState();

            if (!TryGetNetworkSession(out INetworkSessionService network))
                yield break;

            int hostSceneBuildIndex;
            Assert.That(
                MenuSceneTransitionTestSupport.TryResolveBuildIndex(
                    MenuSceneTransitionTestSupport.HostSessionScenePath,
                    out hostSceneBuildIndex),
                Is.True,
                $"Host scene must be in the active build profile: {MenuSceneTransitionTestSupport.HostSessionScenePath}");

            string sessionName = $"RTMenuHost_{Guid.NewGuid():N}";
            Exception error = null;
            yield return MenuSceneTransitionTestSupport.RunNetworkTransition(
                () => network.StartSharedSession(sessionName, hostSceneBuildIndex),
                ex => error = ex);

            if (error != null)
            {
                Assert.Fail($"StartSharedSession failed: {error.Message}");
                yield break;
            }

            MenuSceneTransitionTestSupport.AssertGameplaySceneAdopted(
                MenuSceneTransitionTestSupport.HostSessionSceneName,
                MenuSceneTransitionTestSupport.MenuSceneName);

            yield return MenuSceneTransitionTestSupport.WaitForMenuSessionLocalPlayer(
                MenuSceneTransitionTestSupport.HostSessionSceneName);
            MenuSceneTransitionTestSupport.AssertLocalPlayerSpawnedInScene(
                MenuSceneTransitionTestSupport.HostSessionSceneName);

            yield return MenuSceneTransitionTestSupport.DisconnectNetworkSession();
        }

        private static bool TryGetNetworkSession(out INetworkSessionService network)
        {
            if (!MenuSceneTransitionTestSupport.TryGetNetworkSession(out network))
            {
                Assert.Fail("INetworkSessionService not available after PreLoad bootstrap.");
                return false;
            }

            return true;
        }
    }
}

