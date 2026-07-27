using EnglishQuest.Tests;
using Fusion;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace EnglishQuest.Tests.Networking
{
    [Category(TestCategories.Fast)]
    public class NetworkAuthorityServiceTests
    {
        [Test]
        public void CanReclaimAuthority_AllowsOverridableObjects()
        {
            NetworkObjectFlags flags = NetworkObjectFlags.AllowStateAuthorityOverride;
            Assert.IsTrue(NetworkAuthorityService.CanReclaimAuthority(flags));
        }

        [Test]
        public void CanReclaimAuthority_RejectsMasterClientObjects()
        {
            NetworkObjectFlags flags = NetworkObjectFlags.AllowStateAuthorityOverride
                | NetworkObjectFlags.MasterClientObject;

            Assert.IsFalse(NetworkAuthorityService.CanReclaimAuthority(flags));
        }

        [Test]
        public void CanReclaimAuthority_RejectsDestroyWhenAuthorityLeaves()
        {
            NetworkObjectFlags flags = NetworkObjectFlags.AllowStateAuthorityOverride
                | NetworkObjectFlags.DestroyWhenStateAuthorityLeaves;

            Assert.IsFalse(NetworkAuthorityService.CanReclaimAuthority(flags));
        }

        [Test]
        public void CanReclaimAuthority_RejectsNonOverridableObjects()
        {
            Assert.IsFalse(NetworkAuthorityService.CanReclaimAuthority(NetworkObjectFlags.None));
        }
    }
}

