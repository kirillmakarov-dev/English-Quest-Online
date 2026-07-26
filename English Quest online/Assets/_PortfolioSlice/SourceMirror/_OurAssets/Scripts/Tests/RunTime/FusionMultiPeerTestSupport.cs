using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Fusion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityServiceLocator;

namespace EnglishKingdom.Tests.RunTime
{
    /// <summary>
    /// Shared helpers for Fusion Multi-Peer Play Mode tests.
    /// </summary>
    internal static class FusionMultiPeerTestSupport
    {
        public const string CombatTestSceneName = "CombatTest";
        public const string Lesson1IntroductionSceneName = "Lesson 1 Introduction";
        public const string Lesson1IntroductionQuestId = "1";
        public const string Lesson1CutsceneCameraName = "CutSceneCamera";
        public const float Lesson1GateOpenMinDeltaY = 7f;
        public const float Lesson1GateSequenceGraceSeconds = 4f;
        public const float QuestFlowTimeoutSeconds = 45f;
        public const float RunnerStartupTimeoutSeconds = 45f;
        public const float AutoStartGraceSeconds = 2f;
        public const float NetworkSyncTimeoutSeconds = 10f;

        public static IEnumerator RunIgnoringFailingLogs(IEnumerator testRoutine)
        {
            bool previous = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            try
            {
                while (testRoutine.MoveNext())
                    yield return testRoutine.Current;
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previous;
            }
        }

        public static void AssertMultiPeerMode()
        {
            Assert.That(
                NetworkProjectConfig.Global.PeerMode,
                Is.EqualTo(NetworkProjectConfig.PeerModes.Multiple),
                "NetworkProjectConfig.PeerMode must be Multiple for Multi-Peer testing.");
        }

        public static IEnumerator LoadCombatTestScene() =>
            LoadSceneForMultiPeerTest(CombatTestSceneName);

        public static IEnumerator LoadSceneForMultiPeerTest(string sceneName)
        {
            yield return ShutdownExistingRunners();
            yield return null;

            ServiceLocator.ResetForPlayModeTests();
            WorldMapDefinitionLoader.ResetForPlayModeTests();
            yield return null;

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null,
                $"Scene '{sceneName}' must be in the active Unity 6 build profile scene list.");

            while (!loadOperation.isDone)
                yield return null;

            yield return new WaitForSeconds(AutoStartGraceSeconds);
        }

        public static NetworkRunner GetClientRunner(IReadOnlyList<NetworkRunner> runners)
        {
            Assert.That(runners, Is.Not.Null);
            Assert.That(
                runners.Count,
                Is.GreaterThanOrEqualTo(2),
                "Expected at least two runners so the second peer can act as the client.");

            return runners[1];
        }

        public static NetworkRunner GetHostRunner(IReadOnlyList<NetworkRunner> runners)
        {
            Assert.That(runners, Is.Not.Null);
            Assert.That(runners.Count, Is.GreaterThanOrEqualTo(1), "Expected at least one runner for the host peer.");
            return runners[0];
        }

        public static bool TryGetQuestService(NetworkRunner runner, out IQuestService questService)
        {
            questService = null;
            if (runner == null || !runner.IsRunning)
                return false;

            PlayerInteraction interaction = GetLocalPlayerInteraction(runner);
            if (interaction != null && ServiceLocator.For(interaction).TryGet(out questService))
                return true;

            Scene simulationScene = runner.SimulationUnityScene;
            if (!simulationScene.IsValid())
                return false;

            QuestManager manager = FindComponentInRunner<QuestManager>(runner, includeInactive: true);
            if (manager != null)
            {
                questService = manager;
                return true;
            }

            return false;
        }

        public static QuestInfo FindQuestInfoById(NetworkRunner runner, string questId)
        {
            if (runner == null || !runner.IsRunning || string.IsNullOrEmpty(questId))
                return null;

            Scene simulationScene = runner.SimulationUnityScene;
            if (!simulationScene.IsValid())
                return null;

            var roots = new List<GameObject>();
            simulationScene.GetRootGameObjects(roots);

            for (int i = 0; i < roots.Count; i++)
            {
                QuestInfo[] questInfos = roots[i].GetComponentsInChildren<QuestInfo>(true);
                for (int j = 0; j < questInfos.Length; j++)
                {
                    QuestInfo questInfo = questInfos[j];
                    if (questInfo != null && questInfo.id == questId)
                        return questInfo;
                }
            }

            return null;
        }

        public static T FindComponentInRunner<T>(NetworkRunner runner, string objectName = null, bool includeInactive = false)
            where T : Component
        {
            GameObject gameObject = string.IsNullOrEmpty(objectName)
                ? null
                : FindGameObjectInRunner(runner, objectName);

            if (gameObject != null)
                return gameObject.GetComponent<T>();

            if (runner == null || !runner.IsRunning)
                return null;

            Scene simulationScene = runner.SimulationUnityScene;
            if (!simulationScene.IsValid())
                return null;

            var roots = new List<GameObject>();
            simulationScene.GetRootGameObjects(roots);

            for (int i = 0; i < roots.Count; i++)
            {
                T[] components = roots[i].GetComponentsInChildren<T>(includeInactive);
                if (components.Length > 0)
                    return components[0];
            }

            return null;
        }

        public static GameObject FindGameObjectInRunner(NetworkRunner runner, string objectName)
        {
            if (runner == null || !runner.IsRunning || string.IsNullOrEmpty(objectName))
                return null;

            Scene simulationScene = runner.SimulationUnityScene;
            if (!simulationScene.IsValid())
                return null;

            var roots = new List<GameObject>();
            simulationScene.GetRootGameObjects(roots);

            for (int i = 0; i < roots.Count; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    Transform transform = transforms[j];
                    if (transform != null && transform.name == objectName)
                        return transform.gameObject;
                }
            }

            return null;
        }

        public static bool TryGetDialogueService(NetworkRunner runner, out IDialogueService dialogueService)
        {
            dialogueService = null;
            if (runner == null || !runner.IsRunning)
                return false;

            PlayerInteraction interaction = GetLocalPlayerInteraction(runner);
            if (interaction != null && ServiceLocator.For(interaction).TryGet(out dialogueService))
                return true;

            CinematicDialogueManager manager = FindComponentInRunner<CinematicDialogueManager>(runner, includeInactive: true);
            if (manager != null)
            {
                dialogueService = manager;
                return true;
            }

            return false;
        }

        private static readonly FieldInfo MoveObjectTargetField = typeof(Action_MoveObject).GetField(
            "_target",
            BindingFlags.Instance | BindingFlags.NonPublic);

        public static IEnumerator InteractAndSkipDialogue(NetworkRunner runner, IInteractable interactable)
        {
            Assert.That(runner, Is.Not.Null, "Runner must be assigned for NPC interaction.");
            Assert.That(interactable, Is.Not.Null, "Interactable must be assigned for NPC interaction.");

            PlayerInteraction interaction = GetLocalPlayerInteraction(runner);
            Assert.That(interaction, Is.Not.Null, $"Runner '{runner.name}' should have a local PlayerInteraction.");

            interactable.Interact(interaction);

            yield return WaitUntil(
                () =>
                {
                    if (!TryGetDialogueService(runner, out IDialogueService dialogueService))
                        return true;

                    if (!dialogueService.IsDialogueActive)
                        return true;

                    SkipActiveDialogue(runner);
                    return false;
                },
                NetworkSyncTimeoutSeconds,
                $"Timed out waiting for dialogue to end on runner '{runner.name}'.");
        }

        public static IEnumerator WaitForQuestState(
            NetworkRunner runner,
            QuestInfo quest,
            QuestState expectedState,
            float timeoutSeconds,
            string failureMessage)
        {
            yield return WaitUntil(
                () => quest != null && quest.state == expectedState,
                timeoutSeconds,
                failureMessage);
        }

        public static bool TryGetLesson1GateMoveTransform(NetworkRunner runner, out Transform gateTransform)
        {
            gateTransform = null;
            if (runner == null || !runner.IsRunning)
                return false;

            GameObject cutSceneCamera = FindGameObjectInRunner(runner, Lesson1CutsceneCameraName);
            if (cutSceneCamera == null)
                return false;

            Action_MoveObject[] moveActions = cutSceneCamera.GetComponentsInChildren<Action_MoveObject>(true);
            for (int i = 0; i < moveActions.Length; i++)
            {
                Transform target = GetMoveObjectTarget(moveActions[i]);
                if (target == null)
                    continue;

                gateTransform = target;
                return true;
            }

            return false;
        }

        public static Transform GetLesson1GateMoveTransform(NetworkRunner runner)
        {
            Assert.That(
                TryGetLesson1GateMoveTransform(runner, out Transform gateTransform),
                Is.True,
                $"Could not resolve Lesson 1 gate move target from '{Lesson1CutsceneCameraName}'.");

            return gateTransform;
        }

        public static IEnumerator WaitForGateOpened(
            NetworkRunner runner,
            float baselineY,
            float timeoutSeconds)
        {
            yield return WaitUntil(
                () => TryGetLesson1GateMoveTransform(runner, out Transform gateTransform)
                      && gateTransform.position.y >= baselineY + Lesson1GateOpenMinDeltaY,
                timeoutSeconds,
                $"Lesson 1 gate did not open on runner '{runner.name}' within {timeoutSeconds:0.#}s.");
        }

        public static IEnumerator WaitForCoSessionPlayerCount(
            NetworkRunner runner,
            int minimumCount,
            float timeoutSeconds,
            string failureMessage)
        {
            yield return WaitUntil(
                () => CountCoSessionPlayers(runner) >= minimumCount,
                timeoutSeconds,
                failureMessage);
        }

        private static void SkipActiveDialogue(NetworkRunner runner)
        {
            CinematicDialogueManager dialogueManager =
                FindComponentInRunner<CinematicDialogueManager>(runner, includeInactive: true);

            if (dialogueManager == null || !dialogueManager.IsDialogueActive)
                return;

            MethodInfo skipMethod = typeof(CinematicDialogueManager).GetMethod(
                "SkipDialogue",
                BindingFlags.Instance | BindingFlags.NonPublic);

            skipMethod?.Invoke(dialogueManager, null);
        }

        private static Transform GetMoveObjectTarget(Action_MoveObject moveAction)
        {
            if (moveAction == null || MoveObjectTargetField == null)
                return null;

            return MoveObjectTargetField.GetValue(moveAction) as Transform;
        }

        public static IEnumerator WaitForRunnersWithLocalPlayers(
            int minimumRunnerCount,
            List<NetworkRunner> results)
        {
            results.Clear();

            float elapsed = 0f;
            while (elapsed < RunnerStartupTimeoutSeconds)
            {
                CollectReadyRunnersWithLocalPlayers(results);
                if (results.Count >= minimumRunnerCount)
                    break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.That(
                results.Count,
                Is.GreaterThanOrEqualTo(minimumRunnerCount),
                DescribeRunnerReadinessFailure(minimumRunnerCount, results));
        }

        public static bool IsRunnerReadyForSpawn(NetworkRunner runner)
        {
            if (runner == null || !runner.IsRunning)
                return false;

            Scene simulationScene = runner.SimulationUnityScene;
            if (!simulationScene.IsValid())
                return false;

            NetworkObject playerObject = runner.GetPlayerObject(runner.LocalPlayer);
            if (!PlayerArrivalUtility.IsUsablePlayerObject(playerObject, simulationScene))
                return false;

            return LocalPlayerReadiness.IsReady(runner, simulationScene);
        }

        public static IEnumerator WaitUntil(
            Func<bool> condition,
            float timeoutSeconds,
            string failureMessage)
        {
            float elapsed = 0f;
            while (!condition() && elapsed < timeoutSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.That(condition(), Is.True, failureMessage);
        }

        public static PlayerInteraction GetLocalPlayerInteraction(NetworkRunner runner) =>
            GetPlayerInteraction(runner, runner != null ? runner.LocalPlayer : default);

        public static PlayerInteraction GetPlayerInteraction(NetworkRunner runner, PlayerRef player)
        {
            if (runner == null || !runner.IsRunning || !player.IsRealPlayer)
                return null;

            NetworkObject playerObject = runner.GetPlayerObject(player);
            return playerObject != null
                ? PlayerRoot.Resolve<PlayerInteraction>(playerObject)
                : null;
        }

        public static PickableItem FindPickableInRunner(NetworkRunner runner, NetworkId networkId)
        {
            if (runner == null || !runner.IsRunning || !networkId.IsValid)
                return null;

            if (!runner.TryFindObject(networkId, out NetworkObject networkObject) || networkObject == null)
                return null;

            return networkObject.GetComponent<PickableItem>();
        }

        public static SocialPanelController FindSocialPanelForRunner(NetworkRunner runner) =>
            FindSocialContextForRunner(runner) as SocialPanelController;

        public static MonoBehaviour FindSocialContextForRunner(NetworkRunner runner)
        {
            if (runner == null || !runner.IsRunning)
                return null;

            SocialPanelController panel = FindSocialPanelForRunnerInternal(runner);
            if (panel != null)
                return panel;

            return GetLocalPlayerInteraction(runner);
        }

        private static SocialPanelController FindSocialPanelForRunnerInternal(NetworkRunner runner)
        {
            if (runner == null || !runner.IsRunning)
                return null;

            Scene simulationScene = runner.SimulationUnityScene;
            SocialPanelController panel = FindSocialPanelInScene(simulationScene);
            if (panel != null)
                return panel;

            SocialPanelController[] panels = FindAllSocialPanels();
            for (int i = 0; i < panels.Length; i++)
            {
                SocialPanelController candidate = panels[i];
                if (candidate == null)
                    continue;

                if (SceneNetworkRunner.TryGetForScene(candidate.gameObject.scene, out NetworkRunner sceneRunner)
                    && sceneRunner == runner)
                {
                    return candidate;
                }
            }

            return null;
        }

        public static SocialPanelController FindSocialPanelInScene(Scene scene)
        {
            if (!scene.IsValid())
                return null;

            var roots = new List<GameObject>();
            scene.GetRootGameObjects(roots);

            for (int i = 0; i < roots.Count; i++)
            {
                SocialPanelController panel = roots[i].GetComponentInChildren<SocialPanelController>(true);
                if (panel != null)
                    return panel;
            }

            return null;
        }

        public static SocialPanelController[] FindAllSocialPanels() =>
            UnityEngine.Object.FindObjectsByType<SocialPanelController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        public static bool AreSocialServicesReady(MonoBehaviour context)
        {
            return AreSocialServicesReady(context, null);
        }

        public static bool AreSocialServicesReady(MonoBehaviour context, NetworkRunner runner)
        {
            if (runner != null && runner.IsRunning
                && TryGetPartyService(runner, out IPartyService runnerParty)
                && TryGetSessionPlayerRegistry(runner, out _))
            {
                return runnerParty.GetPartySize(runner.LocalPlayer) >= 1;
            }

            if (context == null)
                return false;

            if (!TryGetSocialServices(context, out IPartyService party, out _))
                return false;

            PlayerInteraction interaction = runner != null ? GetLocalPlayerInteraction(runner) : null;
            if (!ServiceLocator.For(context).TryGet<ISessionPlayerRegistry>(out _)
                && (interaction == null
                    || !ServiceLocator.For(interaction).TryGet(out ISessionPlayerRegistry _)))
            {
                return false;
            }

            if (runner == null
                && !SceneNetworkRunner.TryGetForScene(context.gameObject.scene, out runner))
            {
                return false;
            }

            if (runner == null || !runner.IsRunning)
                return false;

            return party.GetPartySize(runner.LocalPlayer) >= 1;
        }

        public static bool TryGetSessionPlayerCount(MonoBehaviour context, out int playerCount)
        {
            playerCount = 0;
            if (context == null)
                return false;

            if (!ServiceLocator.For(context).TryGet(out ISessionPlayerRegistry registry))
                return false;

            registry.Refresh();
            playerCount = registry.Players.Count;
            return true;
        }

        public static bool TryGetSocialServices(
            MonoBehaviour context,
            out IPartyService party,
            out IPartyInviteService invites)
        {
            party = null;
            invites = null;

            if (context == null)
                return false;

            return ServiceLocator.For(context).TryGet(out party)
                   && ServiceLocator.For(context).TryGet(out invites);
        }

        public static bool TryFindRemotePlayerRef(NetworkRunner runner, out PlayerRef remotePlayer) =>
            TryGetRemotePlayerRef(runner, out remotePlayer);

        public static bool ArePlayersInSameParty(
            NetworkRunner leaderRunner,
            NetworkRunner memberRunner,
            PlayerRef leaderRef)
        {
            if (leaderRunner == null || memberRunner == null || !leaderRef.IsRealPlayer)
                return false;

            PlayerPartyMembership leaderMembership =
                PartyMembershipUtility.GetMembership(leaderRunner, leaderRef);
            PlayerPartyMembership memberMembership =
                PartyMembershipUtility.GetMembership(memberRunner, memberRunner.LocalPlayer);

            if (leaderMembership == null || memberMembership == null)
                return false;

            return leaderMembership.PartyLeader == leaderRef
                   && memberMembership.PartyLeader == leaderRef;
        }

        public static bool AreRemotePlayersReady(NetworkRunner runner)
        {
            if (runner == null || !runner.IsRunning)
                return false;

            PlayerPartyMembership localMembership =
                PartyMembershipUtility.GetMembership(runner, runner.LocalPlayer);
            if (localMembership == null || !localMembership.IsPartyLeader)
                return false;

            if (!TryGetRemotePlayerRef(runner, out PlayerRef remotePlayer))
                return false;

            PlayerPartyMembership remoteMembership =
                PartyMembershipUtility.GetMembership(runner, remotePlayer);

            return remoteMembership != null
                   && remoteMembership.IsSolo
                   && runner.GetPlayerObject(remotePlayer) != null;
        }

        public static IEnumerator WaitForRemotePlayersVisible(
            NetworkRunner firstRunner,
            NetworkRunner secondRunner,
            float timeoutSeconds,
            string failureMessage)
        {
            yield return WaitUntil(
                () => AreRemotePlayersReady(firstRunner) && AreRemotePlayersReady(secondRunner),
                timeoutSeconds,
                failureMessage);
        }

        public static IEnumerator WaitForSessionPlayerCount(
            MonoBehaviour context,
            int minimumCount,
            float timeoutSeconds,
            string failureMessage)
        {
            yield return WaitUntil(
                () => TryGetSessionPlayerCount(context, out int count) && count >= minimumCount,
                timeoutSeconds,
                failureMessage);
        }

        public static void EnsureSocialServicesForRunners(IReadOnlyList<NetworkRunner> runners)
        {
            if (runners == null)
                return;

            for (int i = 0; i < runners.Count; i++)
                TryEnsureSocialServicesForRunner(runners[i]);
        }

        public static void TryEnsureSocialServicesForRunner(NetworkRunner runner)
        {
            if (runner == null || !runner.IsRunning)
                return;

            NetworkObject playerObject = runner.GetPlayerObject(runner.LocalPlayer);
            if (playerObject == null)
                return;

            Scene simulationScene = runner.SimulationUnityScene;
            if (!simulationScene.IsValid())
                return;

            SocialSystemsBootstrap.EnsureForLocalPlayer(runner, simulationScene, playerObject);
        }

        public static IEnumerator WaitForSocialServicesReady(
            MonoBehaviour context,
            float timeoutSeconds,
            string failureMessage)
        {
            yield return WaitUntil(
                () =>
                {
                    if (context != null
                        && SceneNetworkRunner.TryGetForScene(context.gameObject.scene, out NetworkRunner runner))
                    {
                        TryEnsureSocialServicesForRunner(runner);
                    }

                    return AreSocialServicesReady(context);
                },
                timeoutSeconds,
                failureMessage);
        }

        public static IEnumerator WaitForSocialServicesReady(
            NetworkRunner runner,
            MonoBehaviour context,
            float timeoutSeconds,
            string failureMessage)
        {
            yield return WaitUntil(
                () =>
                {
                    TryEnsureSocialServicesForRunner(runner);
                    PlayerSpawnCoordinator.EnsureLocalPlayerAfterSceneLoad(runner);
                    SyncRunnerSocialMemberships(runner);
                    return AreSocialServicesReady(context, runner);
                },
                timeoutSeconds,
                failureMessage);
        }

        public static bool TryGetPartyInviteService(NetworkRunner runner, out IPartyInviteService inviteService)
        {
            inviteService = null;
            if (runner == null || !runner.IsRunning)
                return false;

            if (RunnerSocialServiceRegistry.TryGet(runner, out inviteService))
                return true;

            PlayerInteraction interaction = GetLocalPlayerInteraction(runner);
            if (interaction != null && ServiceLocator.For(interaction).TryGet(out inviteService))
                return true;

            if (IsMultiPeerMode())
                return false;

            SocialPanelController panel = FindSocialPanelForRunner(runner);
            if (panel != null && TryGetSocialServices(panel, out _, out inviteService))
                return true;

            return false;
        }

        public static bool TryGetPartyService(NetworkRunner runner, out IPartyService partyService)
        {
            partyService = null;
            if (runner == null || !runner.IsRunning)
                return false;

            if (RunnerSocialServiceRegistry.TryGet(runner, out partyService))
                return true;

            PlayerInteraction interaction = GetLocalPlayerInteraction(runner);
            if (interaction != null && ServiceLocator.For(interaction).TryGet(out partyService))
                return true;

            if (IsMultiPeerMode())
                return false;

            SocialPanelController panel = FindSocialPanelForRunner(runner);
            if (panel != null && TryGetSocialServices(panel, out partyService, out _))
                return true;

            return false;
        }

        private static bool IsMultiPeerMode() =>
            NetworkProjectConfig.Global.PeerMode == NetworkProjectConfig.PeerModes.Multiple;

        public static bool TryGetRemotePlayerRef(NetworkRunner runner, out PlayerRef remotePlayer)
        {
            remotePlayer = default;
            if (runner == null || !runner.IsRunning)
                return false;

            foreach (PlayerRef player in FusionCoSessionRunners.EnumeratePlayers(runner))
            {
                if (!player.IsRealPlayer || player == runner.LocalPlayer)
                    continue;

                remotePlayer = player;
                return true;
            }

            return false;
        }

        public static bool ArePlayersInSameParty(NetworkRunner runner, PlayerRef leader, PlayerRef member)
        {
            if (runner == null || !runner.IsRunning || !leader.IsRealPlayer || !member.IsRealPlayer)
                return false;

            PlayerPartyMembership leaderMembership = PartyMembershipUtility.GetMembership(runner, leader);
            PlayerPartyMembership memberMembership = PartyMembershipUtility.GetMembership(runner, member);

            if (leaderMembership == null || memberMembership == null)
                return false;

            return leaderMembership.IsPartyLeader
                   && memberMembership.PartyLeader == leader
                   && !memberMembership.IsSolo;
        }

        public static IEnumerator WaitForPendingInvite(
            NetworkRunner runner,
            float timeoutSeconds,
            string failureMessage)
        {
            yield return WaitUntil(
                () => TryGetPartyInviteService(runner, out IPartyInviteService invites)
                      && invites.PendingInvite != null,
                timeoutSeconds,
                failureMessage);
        }

        public static IEnumerator WaitForNoPendingInvite(
            NetworkRunner runner,
            float timeoutSeconds,
            string failureMessage)
        {
            yield return WaitUntil(
                () => TryGetPartyInviteService(runner, out IPartyInviteService invites)
                      && invites.PendingInvite == null,
                timeoutSeconds,
                failureMessage);
        }

        public static bool IsPlayerSolo(NetworkRunner runner, PlayerRef player)
        {
            PlayerPartyMembership membership = PartyMembershipUtility.GetMembership(runner, player);
            return membership != null && membership.IsSolo;
        }

        public static IEnumerator WaitForPlayerSolo(
            NetworkRunner runner,
            PlayerRef player,
            float timeoutSeconds,
            string failureMessage)
        {
            yield return WaitUntil(
                () => IsPlayerSolo(runner, player),
                timeoutSeconds,
                failureMessage);
        }

        public static IEnumerator WaitForPartySize(
            NetworkRunner runner,
            PlayerRef leader,
            int expectedSize,
            float timeoutSeconds,
            string failureMessage)
        {
            yield return WaitUntil(
                () => TryGetPartyService(runner, out IPartyService party)
                      && party.GetPartySize(leader) == expectedSize,
                timeoutSeconds,
                failureMessage);
        }

        public static bool TryGetSessionPlayerRegistry(
            NetworkRunner runner,
            out ISessionPlayerRegistry registry)
        {
            registry = null;
            if (runner == null || !runner.IsRunning)
                return false;

            if (RunnerSocialServiceRegistry.TryGet(runner, out registry))
                return true;

            PlayerInteraction interaction = GetLocalPlayerInteraction(runner);
            return interaction != null
                   && ServiceLocator.For(interaction).TryGet(out registry);
        }

        public static int CountCoSessionRunners(NetworkRunner contextRunner)
        {
            int count = 0;
            foreach (NetworkRunner _ in FusionCoSessionRunners.Enumerate(contextRunner))
                count++;

            return count;
        }

        public static int CountCoSessionPlayers(NetworkRunner contextRunner)
        {
            var playerRefs = new HashSet<PlayerRef>();
            foreach (PlayerRef player in FusionCoSessionRunners.EnumeratePlayers(contextRunner))
                playerRefs.Add(player);

            return playerRefs.Count;
        }

        public static IEnumerator FormPartyViaInvite(
            NetworkRunner leaderRunner,
            NetworkRunner memberRunner,
            PlayerRef leaderRef,
            PlayerRef memberRef)
        {
            yield return WaitUntilCanInvite(
                leaderRunner,
                memberRef,
                RunnerStartupTimeoutSeconds,
                "Leader should be able to invite the solo member.");

            yield return WaitForPartyInviteService(
                leaderRunner,
                RunnerStartupTimeoutSeconds,
                "Leader runner should resolve IPartyInviteService.");

            yield return WaitForPartyInviteService(
                memberRunner,
                RunnerStartupTimeoutSeconds,
                "Member runner should resolve IPartyInviteService.");

            yield return WaitUntil(
                () => leaderRunner.GetPlayerObject(memberRef) != null,
                RunnerStartupTimeoutSeconds,
                $"Leader runner should resolve player object for invite target {memberRef}.");

            SetSoloInputRunner(leaderRunner);
            yield return null;

            Assert.That(
                TryGetPartyInviteService(leaderRunner, out IPartyInviteService leaderInvites),
                Is.True,
                "Leader runner should resolve IPartyInviteService.");

            SyncRunnerSocialMemberships(leaderRunner);
            SyncRunnerSocialMemberships(memberRunner);
            yield return null;

            if (!SendPartyInvite(leaderRunner, memberRef))
                leaderInvites.SendInvite(memberRef);

            yield return null;

            yield return WaitForPendingInvite(
                memberRunner,
                RunnerStartupTimeoutSeconds,
                "Member should receive the leader invite.");

            Assert.That(
                TryGetPartyInviteService(memberRunner, out IPartyInviteService memberInvites),
                Is.True,
                "Member runner should resolve IPartyInviteService.");

            memberInvites.AcceptInvite();

            yield return WaitForPlayersInSameParty(
                leaderRunner,
                leaderRef,
                memberRef,
                RunnerStartupTimeoutSeconds,
                "Players should share party membership after accept.");

            yield return WaitForPlayersInSameParty(
                memberRunner,
                leaderRef,
                memberRef,
                RunnerStartupTimeoutSeconds,
                "Party membership should replicate to the member runner.");

            yield return WaitForNoPendingInvite(
                leaderRunner,
                RunnerStartupTimeoutSeconds,
                "Leader runner should have no pending invites after party formation.");
            yield return WaitForNoPendingInvite(
                memberRunner,
                RunnerStartupTimeoutSeconds,
                "Member runner should have no pending invites after party formation.");
        }

        public static void SyncRunnerSocialMemberships(NetworkRunner runner)
        {
            if (runner == null || !runner.IsRunning)
                return;

            foreach (PlayerRef player in FusionCoSessionRunners.EnumeratePlayers(runner))
            {
                PlayerPartyMembership membership = PartyMembershipUtility.GetMembership(runner, player);
                if (membership != null)
                    RunnerSocialServiceRegistry.TrackMembership(runner, membership);
            }
        }

        public static IEnumerator WaitForPlayersInSameParty(
            NetworkRunner runner,
            PlayerRef leader,
            PlayerRef member,
            float timeoutSeconds,
            string failureMessage)
        {
            yield return WaitUntil(
                () => ArePlayersInSameParty(runner, leader, member),
                timeoutSeconds,
                failureMessage);
        }

        public static bool CanInviteOnRunner(NetworkRunner runner, PlayerRef target)
        {
            if (runner == null || !runner.IsRunning || !target.IsRealPlayer || target == runner.LocalPlayer)
                return false;

            PlayerPartyMembership localMembership =
                PartyMembershipUtility.GetMembership(runner, runner.LocalPlayer);
            PlayerPartyMembership targetMembership =
                PartyMembershipUtility.GetMembership(runner, target);

            if (localMembership == null || targetMembership == null)
                return false;

            return localMembership.IsPartyLeader
                   && targetMembership.IsSolo;
        }

        public static bool SendPartyInvite(NetworkRunner inviterRunner, PlayerRef inviteeRef)
        {
            if (inviterRunner == null || !inviterRunner.IsRunning || !inviteeRef.IsRealPlayer)
                return false;

            PlayerRef inviterRef = inviterRunner.LocalPlayer;
            PlayerPartyMembership inviterMembership =
                PartyMembershipUtility.GetMembership(inviterRunner, inviterRef);

            if (inviterMembership == null)
                return false;

            inviterMembership.SendInviteTo(inviteeRef, inviterRef);
            return true;
        }

        public static IEnumerator WaitUntilRunnerCanInvite(
            NetworkRunner runner,
            PlayerRef target,
            float timeoutSeconds,
            string failureMessage)
        {
            yield return WaitUntil(
                () => CanInviteOnRunner(runner, target),
                timeoutSeconds,
                failureMessage);
        }

        public static IEnumerator WaitForPartyInviteService(
            NetworkRunner runner,
            float timeoutSeconds,
            string failureMessage)
        {
            yield return WaitUntil(
                () => TryGetPartyInviteService(runner, out _),
                timeoutSeconds,
                failureMessage);
        }

        public static IEnumerator WaitUntilCanInvite(
            NetworkRunner runner,
            PlayerRef target,
            float timeoutSeconds,
            string failureMessage)
        {
            yield return WaitUntil(
                () => (IsMultiPeerMode()
                       ? CanInviteOnRunner(runner, target)
                       : TryGetPartyService(runner, out IPartyService party) && party.CanInvite(target)),
                timeoutSeconds,
                $"{failureMessage} {DescribeInviteEligibility(runner, target)}");
        }

        public static string DescribeInviteEligibility(NetworkRunner runner, PlayerRef target)
        {
            var builder = new StringBuilder();
            builder.Append("[InviteDiag] ");

            if (runner == null)
            {
                builder.Append("runner=null");
                return builder.ToString();
            }

            builder.Append($"runner='{runner.name}' running={runner.IsRunning} local={runner.LocalPlayer}; ");

            if (!TryGetPartyService(runner, out IPartyService party))
            {
                builder.Append("partyService=missing");
                return builder.ToString();
            }

            PlayerRef localPlayer = runner.LocalPlayer;
            PlayerPartyMembership localMembership = PartyMembershipUtility.GetMembership(runner, localPlayer);
            PlayerPartyMembership targetMembership = PartyMembershipUtility.GetMembership(runner, target);

            builder.Append($"target={target}; ");
            builder.Append($"localMembership={(localMembership != null ? "yes" : "no")} ");
            if (localMembership != null)
            {
                builder.Append($"localLeader={localMembership.PartyLeader} isLeader={localMembership.IsPartyLeader}; ");
            }

            builder.Append($"targetMembership={(targetMembership != null ? "yes" : "no")} ");
            if (targetMembership != null)
            {
                builder.Append($"targetLeader={targetMembership.PartyLeader} isSolo={targetMembership.IsSolo}; ");
            }

            builder.Append($"partySize={party.GetPartySize(localPlayer)} canInvite={party.CanInvite(target)}");
            return builder.ToString();
        }

        public static void SetSoloInputRunner(NetworkRunner focusedRunner)
        {
            foreach (NetworkRunner runner in NetworkRunner.Instances)
            {
                if (runner == null)
                    continue;

                runner.ProvideInput = runner == focusedRunner;
            }
        }

        public static string DescribeSocialPanels()
        {
            SocialPanelController[] panels = FindAllSocialPanels();
            if (panels.Length == 0)
                return "No SocialPanelController instances found.";

            var builder = new StringBuilder();
            builder.Append("Social panels: ");

            for (int i = 0; i < panels.Length; i++)
            {
                SocialPanelController panel = panels[i];
                if (panel == null)
                    continue;

                Scene scene = panel.gameObject.scene;
                bool hasRunner = SceneNetworkRunner.TryGetForScene(scene, out NetworkRunner runner);
                builder.Append('[')
                    .Append(panel.name)
                    .Append(" scene=")
                    .Append(scene.name)
                    .Append(hasRunner ? $" runner={runner.name}" : " runner=none")
                    .Append("] ");
            }

            return builder.ToString().TrimEnd();
        }

        public static PickableItem FindPickableInRunner(NetworkRunner runner, string objectName)
        {
            if (runner == null || !runner.IsRunning)
                return null;

            Scene scene = runner.SimulationUnityScene;
            if (!scene.IsValid())
                return null;

            var roots = new List<GameObject>();
            scene.GetRootGameObjects(roots);

            for (int i = 0; i < roots.Count; i++)
            {
                PickableItem[] pickables = roots[i].GetComponentsInChildren<PickableItem>(true);
                for (int j = 0; j < pickables.Length; j++)
                {
                    PickableItem pickable = pickables[j];
                    if (pickable == null || pickable.name != objectName)
                        continue;

                    if (pickable.Object != null && pickable.Object.Runner == runner)
                        return pickable;
                }
            }

            return null;
        }

        private static void CollectReadyRunnersWithLocalPlayers(List<NetworkRunner> results)
        {
            results.Clear();

            foreach (NetworkRunner runner in NetworkRunner.Instances)
            {
                if (!IsRunnerReadyForSpawn(runner))
                    continue;

                results.Add(runner);
            }

            results.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        }

        private static string DescribeRunnerReadinessFailure(int minimumRunnerCount, IReadOnlyList<NetworkRunner> readyRunners)
        {
            var builder = new StringBuilder();
            builder.Append("Expected at least ")
                .Append(minimumRunnerCount)
                .Append(" running NetworkRunner instances with ready local players. Ready=")
                .Append(readyRunners.Count)
                .Append(", total=")
                .Append(NetworkRunner.Instances.Count)
                .Append(". ");

            foreach (NetworkRunner runner in NetworkRunner.Instances)
            {
                if (runner == null)
                    continue;

                builder.Append('[').Append(runner.name).Append(": ");
                if (!runner.IsRunning)
                {
                    builder.Append("not running");
                }
                else
                {
                    Scene simulationScene = runner.SimulationUnityScene;
                    NetworkObject playerObject = runner.GetPlayerObject(runner.LocalPlayer);
                    bool hasUsablePlayer = simulationScene.IsValid()
                                           && PlayerArrivalUtility.IsUsablePlayerObject(playerObject, simulationScene);
                    ILocalPlayerReadiness readiness = null;
                    bool readinessRegistered = simulationScene.IsValid()
                                               && LocalPlayerReadiness.TryGet(runner, simulationScene, out readiness);
                    bool readinessReady = readinessRegistered && readiness != null && readiness.IsReady;

                    builder.Append("scene=")
                        .Append(simulationScene.IsValid() ? simulationScene.name : "<invalid>")
                        .Append(", player=")
                        .Append(playerObject != null ? playerObject.name : "<null>")
                        .Append(hasUsablePlayer ? "(usable)" : "(missing/stale)")
                        .Append(", readiness=")
                        .Append(readinessRegistered
                            ? readinessReady ? "ready" : "registered-not-ready"
                            : "missing");
                }

                builder.Append("] ");
            }

            return builder.ToString();
        }

        public static IEnumerator ShutdownExistingRunners()
        {
            var runners = new List<NetworkRunner>();
            foreach (NetworkRunner runner in NetworkRunner.Instances)
            {
                if (runner != null && runner.IsRunning)
                    runners.Add(runner);
            }

            for (int i = 0; i < runners.Count; i++)
            {
                RunnerSocialServiceRegistry.Teardown(runners[i]);
                LocalPlayerReadiness.Clear(runners[i]);
                System.Threading.Tasks.Task shutdownTask = runners[i].Shutdown();
                while (!shutdownTask.IsCompleted)
                    yield return null;
            }

            if (runners.Count > 0)
            {
                for (int frame = 0; frame < 5; frame++)
                    yield return null;
            }

            RunnerSocialServiceRegistry.TeardownAll();
            WorldTravelResilienceTestSupport.DestroyStaleHarnessBootstrapForShutdown();
            ResetFusionBootstrapStages();
            CleanupStaleFusionBootstraps();
        }

        private static void CleanupStaleFusionBootstraps()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            FusionBootstrap[] bootstraps = UnityEngine.Object.FindObjectsByType<FusionBootstrap>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < bootstraps.Length; i++)
            {
                FusionBootstrap bootstrap = bootstraps[i];
                if (bootstrap == null)
                    continue;

                if (bootstrap.gameObject.scene == activeScene)
                    continue;

                UnityEngine.Object.Destroy(bootstrap.gameObject);
            }
        }

        private static void ResetFusionBootstrapStages()
        {
            PropertyInfo stageProperty = typeof(FusionBootstrap).GetProperty(
                nameof(FusionBootstrap.CurrentStage),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (stageProperty == null || !stageProperty.CanWrite)
                return;

            FusionBootstrap[] bootstraps = UnityEngine.Object.FindObjectsByType<FusionBootstrap>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < bootstraps.Length; i++)
            {
                FusionBootstrap bootstrap = bootstraps[i];
                if (bootstrap == null)
                    continue;

                stageProperty.SetValue(bootstrap, FusionBootstrap.Stage.Disconnected);
            }
        }
    }
}
