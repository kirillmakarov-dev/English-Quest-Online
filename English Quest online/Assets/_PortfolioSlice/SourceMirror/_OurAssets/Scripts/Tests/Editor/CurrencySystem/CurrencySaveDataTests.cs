using NUnit.Framework;
using EnglishKingdom.SaveSystem.Data;

namespace EnglishKingdom.Tests.CurrencySystem
{
    /// <summary>
    /// Pure unit tests for <see cref="CurrencySaveData"/>.
    /// No Unity runtime needed — just verifies the data model's constants and defaults.
    /// </summary>
    [TestFixture]
    public class CurrencySaveDataTests
    {
        [Test]
        public void Key_IsPlayerCurrency()
        {
            Assert.AreEqual("player_currency", CurrencySaveData.Key);
        }

        [Test]
        public void CurrentVersion_IsOne()
        {
            Assert.AreEqual(1, CurrencySaveData.CurrentVersion);
        }

        [Test]
        public void DefaultBalance_IsZero()
        {
            var data = new CurrencySaveData();
            Assert.AreEqual(0, data.Balance);
        }

        [Test]
        public void Balance_CanBeSetAndRetrieved()
        {
            var data = new CurrencySaveData { Balance = 99 };
            Assert.AreEqual(99, data.Balance);
        }
    }
}
