using System.Collections;
using EnglishKingdom.Tests.RunTime.Fixtures;
using Fusion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EnglishKingdom.Tests.RunTime.Fixtures
{
    public abstract class SocialMultiPeerFixture : CombatTestMultiPeerFixture
    {
        protected NetworkRunner LeaderRunner => Runners[0];
        protected NetworkRunner MemberRunner => Runners[1];

        [UnitySetUp]
        public IEnumerator PerTestSocialBootstrap() =>
            FusionMultiPeerTestSupport.RunIgnoringFailingLogs(SocialBootstrapBody());

        private IEnumerator SocialBootstrapBody()
        {
            yield return BootstrapCombatSceneIfNeeded();

            MonoBehaviour leaderContext =
                FusionMultiPeerTestSupport.FindSocialContextForRunner(LeaderRunner);
            MonoBehaviour memberContext =
                FusionMultiPeerTestSupport.FindSocialContextForRunner(MemberRunner);

            yield return FusionMultiPeerTestSupport.WaitForSocialServicesReady(
                LeaderRunner,
                leaderContext,
                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,
                "Leader social services should be ready.");
            yield return FusionMultiPeerTestSupport.WaitForSocialServicesReady(
                MemberRunner,
                memberContext,
                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,
                "Member social services should be ready.");

            yield return FusionMultiPeerTestSupport.WaitForRemotePlayersVisible(
                LeaderRunner,
                MemberRunner,
                FusionMultiPeerTestSupport.RunnerStartupTimeoutSeconds,
                "Both runners should see remote party membership before social assertions.");
        }
    }
}
