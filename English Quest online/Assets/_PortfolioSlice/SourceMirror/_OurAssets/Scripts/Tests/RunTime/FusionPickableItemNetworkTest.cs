using System.Collections;
using EnglishQuest.Tests;
using EnglishQuest.Tests.RunTime.Fixtures;
using Fusion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EnglishQuest.Tests.RunTime
{
    [Category(TestCategories.Fast)]
    public class FusionPickableItemNetworkTest : CombatTestMultiPeerFixture
    {
        [UnitySetUp]
        public IEnumerator EnsureBootstrap() => BootstrapCombatSceneIfNeededIgnoringLogs();

        [UnityTest]
        public IEnumerator MultiPeer_PickableItem_SecondClientCannotInteractWhileHeld() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(TestBody());

        private IEnumerator TestBody()
        {
            NetworkRunner firstRunner = Runners[0];
            NetworkRunner secondRunner = Runners[1];

            PlayerInteraction firstPlayerInteraction =
                FusionMultiPeerTestSupport.GetLocalPlayerInteraction(firstRunner);
            PlayerInteraction secondPlayerInteraction =
                FusionMultiPeerTestSupport.GetLocalPlayerInteraction(secondRunner);

            Assert.That(firstPlayerInteraction, Is.Not.Null, "First runner should have a local PlayerInteraction.");
            Assert.That(secondPlayerInteraction, Is.Not.Null, "Second runner should have a local PlayerInteraction.");

            NetworkObject firstPlayerObject = firstRunner.GetPlayerObject(firstRunner.LocalPlayer);
            Vector3 spawnPosition = firstPlayerObject.transform.position
                                      + firstPlayerObject.transform.forward * 2f
                                      + Vector3.up * 0.5f;

            PickableItem spawnedPickable = NetworkPickableTestSupport.SpawnTestPickable(firstRunner, spawnPosition);
            Assert.That(spawnedPickable, Is.Not.Null,
                "Failed to spawn a networked ColorPickup test item. Ensure the prefab is registered as a Fusion prefab.");

            NetworkId pickableId = spawnedPickable.Object.Id;
            Assert.That(pickableId.IsValid, Is.True, "Spawned pickable should have a valid NetworkId.");

            yield return FusionMultiPeerTestSupport.WaitUntil(
                () => FusionMultiPeerTestSupport.FindPickableInRunner(firstRunner, pickableId) != null
                      && FusionMultiPeerTestSupport.FindPickableInRunner(secondRunner, pickableId) != null,
                FusionMultiPeerTestSupport.NetworkSyncTimeoutSeconds,
                "Timed out waiting for the test pickable to appear on both runners.");

            PickableItem firstRunnerPickable =
                FusionMultiPeerTestSupport.FindPickableInRunner(firstRunner, pickableId);
            PickableItem secondRunnerPickable =
                FusionMultiPeerTestSupport.FindPickableInRunner(secondRunner, pickableId);

            Assert.That(firstRunnerPickable, Is.Not.Null);
            Assert.That(secondRunnerPickable, Is.Not.Null);

            PlayerRef firstPlayer = firstRunner.LocalPlayer;
            PlayerInteraction firstPlayerOnSecondRunner =
                FusionMultiPeerTestSupport.GetPlayerInteraction(secondRunner, firstPlayer);

            Assert.That(firstPlayerOnSecondRunner, Is.Not.Null,
                "Second runner should replicate the first player's interaction component.");

            bool firstPickupAccepted = firstRunnerPickable.Interact(firstPlayerInteraction);
            Assert.That(firstPickupAccepted, Is.True, "First player pickup request should be accepted.");

            yield return FusionMultiPeerTestSupport.WaitUntil(
                () => firstRunnerPickable.IsHeld
                      && firstRunnerPickable.CurrentHolder == firstPlayerInteraction,
                FusionMultiPeerTestSupport.NetworkSyncTimeoutSeconds,
                "Timed out waiting for the first player to hold the pickable item.");

            yield return FusionMultiPeerTestSupport.WaitUntil(
                () => secondRunnerPickable.IsHeld
                      && secondRunnerPickable.CurrentHolder == firstPlayerOnSecondRunner,
                FusionMultiPeerTestSupport.NetworkSyncTimeoutSeconds,
                "Timed out waiting for held state to replicate to the second runner.");

            Assert.That(
                secondRunnerPickable.CanInteract,
                Is.False,
                "Second client should not be allowed to interact while another player holds the item.");

            bool secondPickupAccepted = secondRunnerPickable.Interact(secondPlayerInteraction);
            Assert.That(
                secondPickupAccepted,
                Is.False,
                "Second client interaction should fail while the item is held by the first player.");

            Assert.That(secondRunnerPickable.IsHeld, Is.True, "Item should remain held after the rejected interaction.");
            Assert.That(
                secondRunnerPickable.CurrentHolder,
                Is.EqualTo(firstPlayerOnSecondRunner),
                "Held-by reference should remain the first player after the rejected interaction.");
        }
    }
}

