using NUnit.Framework;
using UnityEngine;

namespace EnglishKingdom.Tests.Combat
{
    [TestFixture]
    public class CombatKillCreditTests
    {
        [Test]
        public void IsLocalPlayerKill_ReturnsFalse_WhenNoKiller()
        {
            Assert.That(CombatKillCredit.IsLocalPlayerKill(DeathContext.None, null), Is.False);
        }

        [Test]
        public void IsLocalPlayerKill_ReturnsFalse_WhenKillerIsNotPlayer()
        {
            var monster = new GameObject("Monster");

            try
            {
                Assert.That(
                    CombatKillCredit.IsLocalPlayerKill(new DeathContext(monster), null),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(monster);
            }
        }

        [Test]
        public void IsLocalPlayerKill_ReturnsTrue_WhenOfflinePlayerKiller()
        {
            var player = new GameObject("Player");
            player.tag = "Player";

            try
            {
                Assert.That(
                    CombatKillCredit.IsLocalPlayerKill(new DeathContext(player), null),
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }
    }
}
