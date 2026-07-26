#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class MonsterDefinitionIdBackfillTool
{
    private const string DefinitionsFolder = "Assets/_OurAssets/Data/NPC/Monsters/Definitions";

    [MenuItem("Tools/English Kingdom/Monsters/Backfill Monster Ids From Asset Names")]
    public static void BackfillFromMenu()
    {
        int updated = BackfillAll();
        Debug.Log($"[MonsterDefinitionIdBackfillTool] Updated {updated} monster definition(s).");
    }

    public static int BackfillAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:MonsterDefinitionSO", new[] { DefinitionsFolder });
        int updated = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var definition = AssetDatabase.LoadAssetAtPath<MonsterDefinitionSO>(path);
            if (definition == null)
                continue;

            string derivedId = MonsterDefinitionIdUtility.DeriveIdFromAssetName(definition.name);
            if (string.IsNullOrEmpty(derivedId))
                continue;

            SerializedObject serialized = new SerializedObject(definition);
            SerializedProperty idProperty = serialized.FindProperty("_monsterId");
            if (idProperty == null)
                continue;

            if (!string.IsNullOrWhiteSpace(idProperty.stringValue))
                continue;

            idProperty.stringValue = derivedId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            updated++;
        }

        if (updated > 0)
            AssetDatabase.SaveAssets();

        return updated;
    }
}
#endif
