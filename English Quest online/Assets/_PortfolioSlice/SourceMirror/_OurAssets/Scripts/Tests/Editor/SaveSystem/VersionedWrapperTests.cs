using System;
using NUnit.Framework;
using EnglishKingdom.SaveSystem;
using EnglishKingdom.SaveSystem.Data;

namespace EnglishKingdom.Tests.SaveSystem
{
    [TestFixture]
    public class VersionedWrapperTests
    {
        [Test]
        public void Create_SetsVersionCorrectly()
        {
            var wrapper = VersionedWrapper<CoreSaveData>.Create(new CoreSaveData(), 3);

            Assert.AreEqual(3, wrapper.Version);
        }

        [Test]
        public void Create_SetsDataCorrectly()
        {
            var data = new CoreSaveData { Level = 7, DisplayName = "Hero" };

            var wrapper = VersionedWrapper<CoreSaveData>.Create(data, 1);

            Assert.AreSame(data, wrapper.Data);
        }

        [Test]
        public void Create_StampsSavedAtWithCurrentUtcTime()
        {
            long before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var wrapper = VersionedWrapper<CoreSaveData>.Create(new CoreSaveData(), 1);

            long after = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Assert.That(wrapper.SavedAt, Is.InRange(before, after));
        }

        [Test]
        public void Create_WithNullData_SetsDataToNull()
        {
            var wrapper = VersionedWrapper<CoreSaveData>.Create(null, 2);

            Assert.IsNull(wrapper.Data);
        }

        [Test]
        public void Create_VersionZero_SetsVersionToZero()
        {
            var wrapper = VersionedWrapper<CoreSaveData>.Create(new CoreSaveData(), 0);

            Assert.AreEqual(0, wrapper.Version);
        }
    }
}
