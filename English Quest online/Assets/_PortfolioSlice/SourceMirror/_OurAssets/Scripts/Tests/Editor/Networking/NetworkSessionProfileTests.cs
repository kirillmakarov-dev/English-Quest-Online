using System.Reflection;
using EnglishKingdom.Tests;
using NUnit.Framework;
using UnityEngine;

namespace EnglishKingdom.Tests.Networking
{
    [Category(TestCategories.Fast)]
    public class NetworkSessionProfileTests
    {
        [Test]
        public void HasSessionProperty_IsFalseByDefault()
        {
            var profile = ScriptableObject.CreateInstance<NetworkSessionProfile>();
            Assert.IsFalse(profile.HasSessionProperty);
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void ResolveInitialSceneBuildIndex_ReturnsNegativeForMissingSceneName()
        {
            var profile = ScriptableObject.CreateInstance<NetworkSessionProfile>();
            typeof(NetworkSessionProfile)
                .GetField("_initialSceneName", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(profile, null);
            Assert.Less(profile.ResolveInitialSceneBuildIndex(), 0);
            Object.DestroyImmediate(profile);
        }
    }
}
