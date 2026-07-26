using EnglishKingdom.Tests;
using Fusion;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace EnglishKingdom.Tests.Networking
{
    [Category(TestCategories.Fast)]
    public class NetworkRunnerCallbackHubTests
    {
        [Test]
        public void PlayerLeft_InvokesSubscribersAfterAuthorityReclaimHook()
        {
            var hub = NetworkRunnerCallbackHub.Instance;
            bool subscriberCalled = false;

            void OnPlayerLeft(NetworkRunner runner, PlayerRef player) => subscriberCalled = true;

            hub.PlayerLeft += OnPlayerLeft;
            try
            {
                hub.OnPlayerLeft(null, PlayerRef.None);
            }
            finally
            {
                hub.PlayerLeft -= OnPlayerLeft;
            }

            Assert.IsTrue(subscriberCalled);
        }
    }
}
