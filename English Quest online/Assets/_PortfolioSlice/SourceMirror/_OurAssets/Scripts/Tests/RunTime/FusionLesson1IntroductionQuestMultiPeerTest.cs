using System.Collections;
using System.Collections.Generic;
using EnglishQuest.Tests;
using Fusion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EnglishQuest.Tests.RunTime
{
    /// <summary>
    /// Multi-peer quest test for Lesson 1 Introduction.
    /// The client runner performs all NPC talks; both runners assert the gate opens.
    /// </summary>
    [Category(TestCategories.Integration)]
    public class FusionLesson1IntroductionQuestMultiPeerTest
    {
        private readonly List<NetworkRunner> _runners = new();

        [UnityTest]
        public IEnumerator MultiPeer_Lesson1Introduction_ClientTalksToNpcs_OpensGate() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(TestBody());

        private IEnumerator TestBody()
        {
            FusionMultiPeerTestSupport.AssertMultiPeerMode();
            yield return FusionMultiPeerTestSupport.LoadSceneForMultiPeerTest(
                FusionMultiPeerTestSupport.Lesson1IntroductionSceneName);
            yield return FusionMultiPeerTestSupport.WaitForRunnersWithLocalPlayers(2, _runners);

            NetworkRunner hostRunner = FusionMultiPeerTestSupport.GetHostRunner(_runners);
            NetworkRunner clientRunner = FusionMultiPeerTestSupport.GetClientRunner(_runners);

            FusionMultiPeerTestSupport.SetSoloInputRunner(clientRunner);

            yield return FusionMultiPeerTestSupport.WaitForCoSessionPlayerCount(
                hostRunner,
                2,
                FusionMultiPeerTestSupport.NetworkSyncTimeoutSeconds,
                "Both peers should see two players in the shared session.");

            QuestInfo clientQuest = FusionMultiPeerTestSupport.FindQuestInfoById(
                clientRunner,
                FusionMultiPeerTestSupport.Lesson1IntroductionQuestId);
            Assert.That(clientQuest, Is.Not.Null,
                "Client runner should find the Lesson 1 Introduction quest info.");

            QuestInfo hostQuest = FusionMultiPeerTestSupport.FindQuestInfoById(
                hostRunner,
                FusionMultiPeerTestSupport.Lesson1IntroductionQuestId);
            Assert.That(hostQuest, Is.Not.Null, "Host runner should find the Lesson 1 Introduction quest info.");

            float clientGateBaselineY =
                FusionMultiPeerTestSupport.GetLesson1GateMoveTransform(clientRunner).position.y;
            float hostGateBaselineY =
                FusionMultiPeerTestSupport.GetLesson1GateMoveTransform(hostRunner).position.y;

            QuestTrigger_TalkToNPC questTrigger =
                FusionMultiPeerTestSupport.FindComponentInRunner<QuestTrigger_TalkToNPC>(clientRunner, "OldGurd");
            QuestStep_TalkToNPC headOfVillage =
                FusionMultiPeerTestSupport.FindComponentInRunner<QuestStep_TalkToNPC>(clientRunner, "HeadOfVillageNPC");
            QuestStep_TalkToNPC gateKeeper =
                FusionMultiPeerTestSupport.FindComponentInRunner<QuestStep_TalkToNPC>(clientRunner, "GateKeeper");

            Assert.That(questTrigger, Is.Not.Null, "OldGurd quest trigger should exist on the client scene.");
            Assert.That(headOfVillage, Is.Not.Null, "HeadOfVillageNPC should exist on the client scene.");
            Assert.That(gateKeeper, Is.Not.Null, "GateKeeper should exist on the client scene.");

            yield return FusionMultiPeerTestSupport.InteractAndSkipDialogue(clientRunner, questTrigger);
            yield return FusionMultiPeerTestSupport.WaitForQuestState(
                clientRunner,
                clientQuest,
                QuestState.IN_PROGRESS,
                FusionMultiPeerTestSupport.QuestFlowTimeoutSeconds,
                "Quest should start after talking to OldGurd.");

            yield return FusionMultiPeerTestSupport.InteractAndSkipDialogue(clientRunner, headOfVillage);
            yield return FusionMultiPeerTestSupport.WaitUntil(
                () => clientQuest.currentStepIndex == 1,
                FusionMultiPeerTestSupport.QuestFlowTimeoutSeconds,
                "Quest should advance to GateKeeper after talking to HeadOfVillageNPC.");

            yield return FusionMultiPeerTestSupport.InteractAndSkipDialogue(clientRunner, gateKeeper);
            yield return FusionMultiPeerTestSupport.WaitForQuestState(
                clientRunner,
                clientQuest,
                QuestState.FINISHED,
                FusionMultiPeerTestSupport.QuestFlowTimeoutSeconds,
                "Quest should finish after talking to GateKeeper.");

            yield return FusionMultiPeerTestSupport.WaitForGateOpened(
                clientRunner,
                clientGateBaselineY,
                FusionMultiPeerTestSupport.Lesson1GateSequenceGraceSeconds
                    + FusionMultiPeerTestSupport.QuestFlowTimeoutSeconds);

            yield return FusionMultiPeerTestSupport.WaitForQuestState(
                hostRunner,
                hostQuest,
                QuestState.FINISHED,
                FusionMultiPeerTestSupport.NetworkSyncTimeoutSeconds,
                "Host runner should observe the replicated finished quest state.");

            yield return FusionMultiPeerTestSupport.WaitForGateOpened(
                hostRunner,
                hostGateBaselineY,
                FusionMultiPeerTestSupport.Lesson1GateSequenceGraceSeconds
                    + FusionMultiPeerTestSupport.NetworkSyncTimeoutSeconds);
        }
    }
}

