using NUnit.Framework;
using UnityEngine;

namespace EnglishKingdom.Tests.MonsterSpawn
{
    [TestFixture]
    public class MonsterCatalogIdTests
    {
        [Test]
        public void TryGetById_ReturnsFalse_WhenIdMissing()
        {
            var catalog = ScriptableObject.CreateInstance<MonsterCatalogSO>();
            try
            {
                Assert.That(catalog.TryGetById(null, out _), Is.False);
                Assert.That(catalog.TryGetById(string.Empty, out _), Is.False);
                Assert.That(catalog.TryGetById("missing", out MonsterDefinitionSO result), Is.False);
                Assert.That(result, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void TryGetById_ReturnsMatchingDefinition()
        {
            var catalog = ScriptableObject.CreateInstance<MonsterCatalogSO>();
            var definition = ScriptableObject.CreateInstance<MonsterDefinitionSO>();

            SetMonsterId(definition, "test_brute");

            SerializedCatalogBuilder.AddMonster(catalog, definition);

            try
            {
                Assert.That(catalog.TryGetById("test_brute", out MonsterDefinitionSO found), Is.True);
                Assert.That(found, Is.SameAs(definition));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(catalog);
            }
        }

        private static void SetMonsterId(MonsterDefinitionSO definition, string monsterId)
        {
            var serialized = new UnityEditor.SerializedObject(definition);
            serialized.FindProperty("_monsterId").stringValue = monsterId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    internal static class SerializedCatalogBuilder
    {
        public static void AddMonster(MonsterCatalogSO catalog, MonsterDefinitionSO definition)
        {
            var serialized = new UnityEditor.SerializedObject(catalog);
            UnityEditor.SerializedProperty list = serialized.FindProperty("_monsters");
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = definition;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
