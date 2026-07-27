using System.Collections;
using EnglishQuest.Tests;
using EnglishQuest.Tests.RunTime.Fixtures;
using Fusion;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EnglishQuest.Tests.RunTime
{
    [Category(TestCategories.Fast)]
    public class FusionPartyOperationsMultiPeerTest : SocialMultiPeerFixture
    {
        [UnityTest]
        public IEnumerator MultiPeer_Party_LeaveParty_ReturnsToSolo() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(LeavePartyBody());

        [UnityTest]
        public IEnumerator MultiPeer_Party_LeaderKick_RemovesMember() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(LeaderKickBody());

        [UnityTest]
        public IEnumerator MultiPeer_PartyInvite_DeclineInvite_ClearsPending() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(DeclineInviteBody());

        [UnityTest]
        public IEnumerator MultiPeer_PartyInvite_FullParty_RejectsInvite() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(FullPartyRejectsInviteBody());

        private IEnumerator LeavePartyBody()
        {
            PlayerRef leaderRef = LeaderRunner.LocalPlayer;
            Assert.That(
                FusionMultiPeerTestSupport.TryGetRemotePlayerRef(LeaderRunner, out PlayerRef memberRef),
                Is.True,
                "Leader runner should resolve the remote member PlayerRef.");

            yield return FusionMultiPeerTestSupport.FormPartyViaInvite(
                LeaderRunner, MemberRunner, leaderRef, memberRef);

            Assert.That(
                FusionMultiPeerTestSupport.TryGetPartyService(MemberRunner, out IPartyService memberParty),
                Is.True,
                "Member runner should resolve IPartyService.");

            memberParty.LeaveParty();

            yield return FusionMultiPeerTestSupport.WaitForPlayerSolo(
                LeaderRunner,
                memberRef,
                FusionMultiPeerTestSupport.NetworkSyncTimeoutSeconds,
                "Member should return to solo membership on the leader runner.");

            yield return FusionMultiPeerTestSupport.WaitForPlayerSolo(
                MemberRunner,
                memberRef,
                FusionMultiPeerTestSupport.NetworkSyncTimeoutSeconds,
                "Member should return to solo membership on the member runner.");

            Assert.That(
                FusionMultiPeerTestSupport.IsPlayerSolo(LeaderRunner, leaderRef),
                Is.True,
                "Leader should remain solo after the member leaves.");
        }

        private IEnumerator LeaderKickBody()
        {
            PlayerRef leaderRef = LeaderRunner.LocalPlayer;
            Assert.That(
                FusionMultiPeerTestSupport.TryGetRemotePlayerRef(LeaderRunner, out PlayerRef memberRef),
                Is.True,
                "Leader runner should resolve the remote member PlayerRef.");

            yield return FusionMultiPeerTestSupport.FormPartyViaInvite(
                LeaderRunner, MemberRunner, leaderRef, memberRef);

            Assert.That(
                FusionMultiPeerTestSupport.TryGetPartyService(LeaderRunner, out IPartyService leaderParty),
                Is.True,
                "Leader runner should resolve IPartyService.");

            leaderParty.Kick(memberRef);

            yield return FusionMultiPeerTestSupport.WaitForPlayerSolo(
                LeaderRunner,
                memberRef,
                FusionMultiPeerTestSupport.NetworkSyncTimeoutSeconds,
                "Kicked member should return to solo on the leader runner.");

            yield return FusionMultiPeerTestSupport.WaitForPlayerSolo(
                MemberRunner,
                memberRef,
                FusionMultiPeerTestSupport.NetworkSyncTimeoutSeconds,
                "Kicked member should return to solo on the member runner.");

            PlayerPartyMembership leaderMembership =
                PartyMembershipUtility.GetMembership(LeaderRunner, leaderRef);
            Assert.That(leaderMembership, Is.Not.Null);
            Assert.That(leaderMembership.IsPartyLeader, Is.True,
                "Leader should remain the party leader after kicking the only member.");
        }

        private IEnumerator DeclineInviteBody()
        {
            PlayerRef leaderRef = LeaderRunner.LocalPlayer;
            Assert.That(
                FusionMultiPeerTestSupport.TryGetRemotePlayerRef(LeaderRunner, out PlayerRef memberRef),
                Is.True,
                "Leader runner should resolve the remote member PlayerRef.");

            yield return FusionMultiPeerTestSupport.WaitUntilCanInvite(
                LeaderRunner,
                memberRef,
                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,
                "Leader should be able to invite the solo member.");

            Assert.That(
                FusionMultiPeerTestSupport.TryGetPartyInviteService(LeaderRunner, out IPartyInviteService leaderInvites),
                Is.True,
                "Leader runner should resolve IPartyInviteService.");

            FusionMultiPeerTestSupport.SetSoloInputRunner(LeaderRunner);
            yield return null;

            if (!FusionMultiPeerTestSupport.SendPartyInvite(LeaderRunner, memberRef))
                leaderInvites.SendInvite(memberRef);

            yield return FusionMultiPeerTestSupport.WaitForPendingInvite(
                MemberRunner,
                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,
                "Member should receive a pending invite.");

            Assert.That(
                FusionMultiPeerTestSupport.TryGetPartyInviteService(MemberRunner, out IPartyInviteService memberInvites),
                Is.True,
                "Member runner should resolve IPartyInviteService.");

            memberInvites.DeclineInvite();

            yield return FusionMultiPeerTestSupport.WaitForNoPendingInvite(
                MemberRunner,
                FusionMultiPeerTestSupport.NetworkSyncTimeoutSeconds,
                "Pending invite should clear after decline.");

            Assert.That(
                FusionMultiPeerTestSupport.IsPlayerSolo(MemberRunner, memberRef),
                Is.True,
                "Member should remain solo after declining.");

            Assert.That(
                FusionMultiPeerTestSupport.IsPlayerSolo(LeaderRunner, memberRef),
                Is.True,
                "Leader runner should still see the member as solo after decline.");
        }

        private IEnumerator FullPartyRejectsInviteBody()
        {
            PlayerRef leaderRef = LeaderRunner.LocalPlayer;
            Assert.That(
                FusionMultiPeerTestSupport.TryGetRemotePlayerRef(LeaderRunner, out PlayerRef memberRef),
                Is.True,
                "Leader runner should resolve the remote member PlayerRef.");

            yield return FusionMultiPeerTestSupport.FormPartyViaInvite(
                LeaderRunner, MemberRunner, leaderRef, memberRef);

            Assert.That(
                FusionMultiPeerTestSupport.TryGetPartyService(LeaderRunner, out IPartyService leaderParty),
                Is.True,
                "Leader runner should resolve IPartyService.");

            Assert.That(
                leaderParty.CanInvite(memberRef),
                Is.False,
                "Leader should not be able to invite a player who is already in the party.");

            Assert.That(
                FusionMultiPeerTestSupport.TryGetPartyInviteService(LeaderRunner, out IPartyInviteService leaderInvites),
                Is.True,
                "Leader runner should resolve IPartyInviteService.");

            leaderInvites.SendInvite(memberRef);

            yield return FusionMultiPeerTestSupport.WaitForNoPendingInvite(
                MemberRunner,
                FusionMultiPeerTestSupport.NetworkSyncTimeoutSeconds,
                "Re-inviting an existing party member should not create a pending invite.");

            Assert.That(
                leaderParty.GetPartySize(leaderRef),
                Is.LessThan(PartyConstants.MaxSize),
                "Two-peer setup cannot fill a party to MaxSize; this test validates rejection for non-solo targets.");
        }
    }
}

