using EnglishQuest.Tests;
using NUnit.Framework;

namespace EnglishQuest.Tests.Authentication
{
    [Category(TestCategories.Fast)]
    public class WhitelistAccessResultMapperTests
    {
        [TestCase("GRANTED", WhitelistAccessOutcome.Student)]
        [TestCase("GRANTED_NEW", WhitelistAccessOutcome.Student)]
        [TestCase("GRANTED_TEACHER", WhitelistAccessOutcome.Guider)]
        [TestCase("GRANTED_NEW_TEACHER", WhitelistAccessOutcome.Guider)]
        [TestCase("NOT_FOUND", WhitelistAccessOutcome.NotFound)]
        [TestCase("DENIED", WhitelistAccessOutcome.Denied)]
        [TestCase(null, WhitelistAccessOutcome.Denied)]
        [TestCase("", WhitelistAccessOutcome.Denied)]
        [TestCase("UNKNOWN", WhitelistAccessOutcome.Denied)]
        public void Map_ReturnsExpectedOutcome(string response, WhitelistAccessOutcome expected)
        {
            Assert.That(WhitelistAccessResultMapper.Map(response), Is.EqualTo(expected));
        }
    }
}

