using NUnit.Framework;

namespace EnglishKingdom.Tests.MonsterSpawn
{
    [TestFixture]
    public class MonsterDefinitionIdUtilityTests
    {
        [Test]
        public void DeriveIdFromAssetName_ConvertsMonsterDefinitionName()
        {
            Assert.That(
                MonsterDefinitionIdUtility.DeriveIdFromAssetName("Monster_ChestMonster_Definition"),
                Is.EqualTo("chest_monster"));
        }

        [Test]
        public void DeriveIdFromAssetName_ReturnsEmpty_WhenBlank()
        {
            Assert.That(MonsterDefinitionIdUtility.DeriveIdFromAssetName("   "), Is.EqualTo(string.Empty));
        }
    }
}
