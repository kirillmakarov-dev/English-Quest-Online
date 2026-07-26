using EnglishKingdom.Tests;
using NUnit.Framework;
using UnityEngine;

namespace EnglishKingdom.Tests.Authentication
{
    [Category(TestCategories.Fast)]
    public class PlayerRoleProfileTests
    {
        private const string PlayerPrefsKey = "playerRole";

        [SetUp]
        [TearDown]
        public void ClearRole()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void SetGuider_true_IsGuider_returns_true()
        {
            PlayerRoleProfile.SetGuider(true);
            Assert.IsTrue(PlayerRoleProfile.IsGuider);
        }

        [Test]
        public void SetGuider_false_IsGuider_returns_false()
        {
            PlayerRoleProfile.SetGuider(false);
            Assert.IsFalse(PlayerRoleProfile.IsGuider);
        }

        [Test]
        public void Clear_resets_to_student()
        {
            PlayerRoleProfile.SetGuider(true);
            PlayerRoleProfile.Clear();
            Assert.IsFalse(PlayerRoleProfile.IsGuider);
        }

        [Test]
        public void Legacy_teacher_playerPrefs_value_is_treated_as_guider()
        {
            PlayerPrefs.SetString("playerRole", "teacher");
            PlayerPrefs.Save();
            Assert.IsTrue(PlayerRoleProfile.IsGuider);
        }
    }
}
