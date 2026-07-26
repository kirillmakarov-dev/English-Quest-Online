using System.Collections;
using EnglishKingdom.Tests;
using EnglishKingdom.Tests.RunTime.Fixtures;
using Fusion;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EnglishKingdom.Tests.RunTime
{
    [Category(TestCategories.Fast)]
    public class FusionPartyInviteMultiPeerTest : SocialMultiPeerFixture
    {
        [UnityTest]
        public IEnumerator MultiPeer_PartyInvite_SendAccept_PutsPlayersTogether() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(SendAcceptBody());

        [UnityTest]
        public IEnumerator MultiPeer_PartyInvite_CannotInviteWhileNotLeader() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(NotLeaderBody());

        private IEnumerator SendAcceptBody()
        {
            SocialPanelController inviterPanel =
                FusionMultiPeerTestSupport.FindSocialPanelForRunner(LeaderRunner);
            SocialPanelController inviteePanel =
                FusionMultiPeerTestSupport.FindSocialPanelForRunner(MemberRunner);

            Assert.That(inviterPanel, Is.Not.Null,
                $"Inviter runner should have a social panel. {FusionMultiPeerTestSupport.DescribeSocialPanels()}");
            Assert.That(inviteePanel, Is.Not.Null,
                $"Invitee runner should have a social panel. {FusionMultiPeerTestSupport.DescribeSocialPanels()}");

            PlayerRef inviterRef = LeaderRunner.LocalPlayer;
            Assert.That(
                FusionMultiPeerTestSupport.TryGetRemotePlayerRef(LeaderRunner, out PlayerRef inviteeRef),
                Is.True,
                "Inviter runner should resolve the remote invitee PlayerRef in the shared session.");

            yield return FusionMultiPeerTestSupport.FormPartyViaInvite(
                LeaderRunner, MemberRunner, inviterRef, inviteeRef);

            Assert.That(
                FusionMultiPeerTestSupport.TryGetPartyService(MemberRunner, out IPartyService inviteeParty),
                Is.True,
                "Invitee runner should resolve IPartyService after joining.");

            Assert.That(
                FusionMultiPeerTestSupport.TryGetPartyService(LeaderRunner, out IPartyService inviterParty),
                Is.True,
                "Inviter runner should resolve IPartyService after joining.");

            PartySnapshot inviterSnapshot = inviterParty.GetMyParty();
            PartySnapshot inviteeSnapshot = inviteeParty.GetMyParty();

            Assert.That(inviterSnapshot.IsInMultiMemberParty, Is.True,
                "Inviter should report being in a multi-member party.");
            Assert.That(inviteeSnapshot.IsInMultiMemberParty, Is.True,
                "Invitee should report being in a multi-member party.");
            Assert.That(inviterSnapshot.Members.Count, Is.GreaterThanOrEqualTo(2),
                "Inviter party snapshot should list at least two members.");
            Assert.That(inviteeSnapshot.Members.Count, Is.GreaterThanOrEqualTo(2),
                "Invitee party snapshot should list at least two members.");
            Assert.That(
                FusionMultiPeerTestSupport.TryGetPartyInviteService(MemberRunner, out IPartyInviteService inviteeInvites),
                Is.True,
                "Invitee runner should resolve IPartyInviteService.");
            Assert.That(inviteeInvites.PendingInvite, Is.Null,
                "Pending invite should clear after accept.");
        }

        private IEnumerator NotLeaderBody()
        {
            PlayerRef leaderRef = LeaderRunner.LocalPlayer;
            Assert.That(
                FusionMultiPeerTestSupport.TryGetRemotePlayerRef(LeaderRunner, out PlayerRef memberRef),
                Is.True,
                "Leader runner should resolve the remote member PlayerRef in the shared session.");

            yield return FusionMultiPeerTestSupport.FormPartyViaInvite(
                LeaderRunner, MemberRunner, leaderRef, memberRef);

            Assert.That(
                FusionMultiPeerTestSupport.TryGetPartyService(MemberRunner, out IPartyService memberParty),
                Is.True,
                "Member runner should resolve IPartyService.");

            Assert.That(
                memberParty.CanInvite(leaderRef),
                Is.False,
                "A party member who is not the leader should not be able to invite others.");

            Assert.That(
                FusionMultiPeerTestSupport.TryGetPartyInviteService(MemberRunner, out IPartyInviteService memberInvites),
                Is.True,
                "Member runner should resolve IPartyInviteService.");

            memberInvites.SendInvite(leaderRef);

            Assert.That(
                FusionMultiPeerTestSupport.TryGetPartyInviteService(LeaderRunner, out IPartyInviteService leaderInviteService)
                    && leaderInviteService.PendingInvite == null,
                Is.True,
                "Non-leader invite attempts should not create a pending invite on the leader.");
        }
    }
}
