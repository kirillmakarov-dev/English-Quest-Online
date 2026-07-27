#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using EnglishQuest.QuestSystem;
using NPC;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Populates Open World <c>NPCs Location</c> with NPC_Base setups (Blacksmith pattern)
/// and registers matching entries in <c>NpcCatalog_OpenWorld</c>.
/// </summary>
[InitializeOnLoad]
public static class OpenWorldNpcLocationPopulator
{
    const string NpcsLocationPath =
        "Assets/_OurAssets/Art/Prefabs/Levels/00 Open World/NPCs Location.prefab";

    const string NpcBasePath =
        "Assets/_OurAssets/Art/Prefabs/Characters/00 Base/NPC_Base.prefab";

    const string NpcCatalogPath =
        "Assets/_OurAssets/Data/Quests/Lines/OpenWorld/Catalogs/NpcCatalog_OpenWorld.asset";

    const string CharactersRoot =
        "Assets/_OurAssets/Art/Prefabs/Characters/NPCs";

    const string AutoRunPrefsKey = "OpenWorldNpcLocationPopulator.AutoRunOnce.v1";

    static OpenWorldNpcLocationPopulator()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        // One-shot after this tool is imported while the editor already has the project open.
        if (EditorPrefs.GetBool(AutoRunPrefsKey, false))
            return;

        EditorApplication.delayCall += AutoRunOnceIfNeeded;
    }

    static void AutoRunOnceIfNeeded()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += AutoRunOnceIfNeeded;
            return;
        }

        if (EditorPrefs.GetBool(AutoRunPrefsKey, false))
            return;

        try
        {
            // Only auto-run while capsule markers still exist.
            if (!HasAnyMappedMarkerOnDisk())
            {
                EditorPrefs.SetBool(AutoRunPrefsKey, true);
                return;
            }

            int created = Populate();
            EditorPrefs.SetBool(AutoRunPrefsKey, true);
            Debug.Log(
                $"[OpenWorldNpcLocationPopulator] Auto-run complete. Created/updated {created} NPC_Base(s).");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OpenWorldNpcLocationPopulator] Auto-run failed: {ex}");
        }
    }

    static bool HasAnyMappedMarkerOnDisk()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(NpcsLocationPath);
        try
        {
            foreach (NpcSpec spec in Specs)
            {
                if (spec.IsLeftover)
                    continue;
                if (FindMarker(root.transform, spec.MarkerName) != null)
                    return true;
            }

            return false;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    struct NpcSpec
    {
        public string MarkerName;
        public string CharacterFolderAndPrefab;
        public string NpcBaseName;
        public string NpcId;
        public string DisplayName;
        public bool IsLeftover;
    }

    static readonly NpcSpec[] Specs =
    {
        // Marker replacements
        Marker("Construction Worker", "Benny the Builder/Benny the Builder.prefab",
            "Benny_the_Builder_NPC_Base", "Benny the Builder", "Benny the Builder"),
        Marker("Cannoneer ", "Soldier 4 - Cannoneer/Soldier 4 - Cannoneer.prefab",
            "Cannoneer_NPC_Base", "Cannoneer", "Cannoneer"),
        Marker("Cannoneer", "Soldier 4 - Cannoneer/Soldier 4 - Cannoneer.prefab",
            "Cannoneer_NPC_Base", "Cannoneer", "Cannoneer"),
        Marker("Secondery Gate Soldier", "Soldier 3/Soldier 3.prefab",
            "Secondary_Gate_Soldier_NPC_Base", "Secondary Gate Soldier", "Secondary Gate Soldier"),
        Marker("Secondery Gate Soldier 1", "Guard gate - Main/Guard gate - Main.prefab",
            "Secondary_Gate_Main_NPC_Base", "Secondary Gate Main", "Secondary Gate Main"),
        Marker("Palace Guard", "Old Guard/Old Guard.prefab",
            "Palace_Guard_NPC_Base", "Palace Guard", "Palace Guard"),
        Marker("Lone tower wizard", "DarkWizzard/DarkWizzard.prefab",
            "Lone_Tower_Wizard_NPC_Base", "Dark Wizard", "Dark Wizard"),
        Marker("Old man under the tree", "Old_Man/Old_Man.prefab",
            "Old_Man_Under_Tree_NPC_Base", "Old Man", "Old Man"),
        Marker("City Elder", "Village Head/Village Head.prefab",
            "City_Elder_NPC_Base", "Village Head", "Village Head"),
        Marker("Demolition worker", "Farmer V2/Farmer V2.prefab",
            "Farmer_V2_NPC_Base", "Farmer V2", "Farmer V2"),
        Marker("Dragon Warrior", "King Numbers/King Numbers.prefab",
            "King_Numbers_NPC_Base", "King Numbers", "King Numbers"),
        Marker(@"Priest\Rabbi", "Old_Woman/Old_Woman.prefab",
            "Old_Woman_NPC_Base", "Old Woman", "Old Woman"),
        Marker("Palace Guard (1)", "Professor Crazy/Professor Crazy.prefab",
            "Professor_Crazy_NPC_Base", "Professor Crazy", "Professor Crazy"),

        // Leftovers (no marker — place later)
        Leftover("ChefTest/ChefTest.prefab", "Chef_NPC_Base", "Chef", "Chef"),
        Leftover("Farmer/Farmer.prefab", "Farmer_NPC_Base", "Farmer", "Farmer"),
        Leftover("Flight Guide/Flight Guide.prefab", "Flight_Guide_NPC_Base", "Flight Guide", "Flight Guide"),
        Leftover("LittleBoy/LittleBoy.prefab", "Little_Boy_NPC_Base", "Little Boy", "Little Boy"),
        Leftover("LittleGirl/LittleGirl.prefab", "Little_Girl_NPC_Base", "Little Girl", "Little Girl"),
        Leftover("Mountain Guide/Mountain Guide.prefab", "Mountain_Guide_NPC_Base", "Mountain Guide", "Mountain Guide"),
        Leftover("Museum Clerk/Museum Clerk.prefab", "Museum_Clerk_NPC_Base", "Museum Clerk", "Museum Clerk"),
        Leftover("Painter/Painter.prefab", "Painter_NPC_Base", "Painter", "Painter"),
        Leftover("Riding Guide/Riding Guide.prefab", "Riding_Guide_NPC_Base", "Riding Guide", "Riding Guide"),
    };

    static NpcSpec Marker(string marker, string character, string baseName, string id, string display) =>
        new NpcSpec
        {
            MarkerName = marker,
            CharacterFolderAndPrefab = character,
            NpcBaseName = baseName,
            NpcId = id,
            DisplayName = display,
            IsLeftover = false
        };

    static NpcSpec Leftover(string character, string baseName, string id, string display) =>
        new NpcSpec
        {
            MarkerName = null,
            CharacterFolderAndPrefab = character,
            NpcBaseName = baseName,
            NpcId = id,
            DisplayName = display,
            IsLeftover = true
        };

    [MenuItem("Tools/English Kingdom/NPCs/Populate Open World NPC Locations")]
    public static void PopulateFromMenu()
    {
        int created = Populate();
        EditorUtility.DisplayDialog(
            "Open World NPCs",
            $"Populate finished. Created/updated {created} NPC_Base(s). Catalog synced.",
            "OK");
    }

    /// <summary>Batch / CI entry: Unity -executeMethod OpenWorldNpcLocationPopulator.PopulateBatch</summary>
    public static void PopulateBatch()
    {
        try
        {
            int created = Populate();
            Debug.Log($"[OpenWorldNpcLocationPopulator] Done. Created/updated {created} NPC_Base(s).");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OpenWorldNpcLocationPopulator] Failed: {ex}");
            EditorApplication.Exit(1);
        }
    }

    public static int Populate()
    {
        GameObject npcBasePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NpcBasePath);
        if (npcBasePrefab == null)
            throw new InvalidOperationException($"Missing NPC_Base at '{NpcBasePath}'.");

        NpcCatalogSO catalog = AssetDatabase.LoadAssetAtPath<NpcCatalogSO>(NpcCatalogPath);
        if (catalog == null)
            throw new InvalidOperationException($"Missing catalog at '{NpcCatalogPath}'.");

        GameObject root = PrefabUtility.LoadPrefabContents(NpcsLocationPath);
        if (root == null)
            throw new InvalidOperationException($"Failed to load '{NpcsLocationPath}'.");

        int created = 0;
        int leftoverIndex = 0;
        var processedBaseNames = new HashSet<string>(StringComparer.Ordinal);

        try
        {
            // Marker specs first (including Cannoneer trailing-space alias — only create once)
            foreach (NpcSpec spec in Specs)
            {
                if (processedBaseNames.Contains(spec.NpcBaseName))
                {
                    // Still remove duplicate marker aliases if present
                    if (!spec.IsLeftover)
                        DestroyMarkerIfPresent(root, spec.MarkerName);
                    continue;
                }

                if (FindChildByName(root.transform, spec.NpcBaseName) != null)
                {
                    processedBaseNames.Add(spec.NpcBaseName);
                    if (!spec.IsLeftover)
                        DestroyMarkerIfPresent(root, spec.MarkerName);
                    continue;
                }

                Vector3 localPos = Vector3.zero;
                Quaternion localRot = Quaternion.identity;

                if (!spec.IsLeftover)
                {
                    Transform marker = FindMarker(root.transform, spec.MarkerName);
                    if (marker == null)
                    {
                        Debug.LogWarning(
                            $"[OpenWorldNpcLocationPopulator] Marker '{spec.MarkerName}' not found; " +
                            $"creating '{spec.NpcBaseName}' as leftover offset.");
                        localPos = new Vector3(leftoverIndex * 2f, 0f, 0f);
                        leftoverIndex++;
                    }
                    else
                    {
                        localPos = marker.localPosition;
                        localRot = marker.localRotation;
                        DestroyMarker(marker.gameObject);
                    }
                }
                else
                {
                    localPos = new Vector3(leftoverIndex * 2f, 0f, 0f);
                    leftoverIndex++;
                }

                string characterPath = $"{CharactersRoot}/{spec.CharacterFolderAndPrefab}";
                GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(characterPath);
                if (characterPrefab == null)
                {
                    Debug.LogError($"[OpenWorldNpcLocationPopulator] Missing character '{characterPath}'.");
                    continue;
                }

                GameObject npcInstance = (GameObject)PrefabUtility.InstantiatePrefab(npcBasePrefab, root.transform);
                npcInstance.name = spec.NpcBaseName;
                npcInstance.transform.localPosition = localPos;
                npcInstance.transform.localRotation = localRot;
                npcInstance.transform.localScale = Vector3.one;

                SetupNpcBase(npcInstance, characterPrefab, spec);
                processedBaseNames.Add(spec.NpcBaseName);
                created++;
            }

            PrefabUtility.SaveAsPrefabAsset(root, NpcsLocationPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AppendCatalogEntries(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[OpenWorldNpcLocationPopulator] Created {created} NPC_Base(s) under NPCs Location.");
        return created;
    }

    static void SetupNpcBase(GameObject npcInstance, GameObject characterPrefab, NpcSpec spec)
    {
        Transform visual = npcInstance.transform.Find("Visual");
        if (visual == null)
            throw new InvalidOperationException($"NPC_Base missing Visual child on '{npcInstance.name}'.");

        GameObject character = (GameObject)PrefabUtility.InstantiatePrefab(characterPrefab, visual);
        character.transform.localPosition = Vector3.zero;
        character.transform.localRotation = Quaternion.identity;
        character.transform.localScale = Vector3.one;

        NPCController controller = npcInstance.GetComponent<NPCController>();
        if (controller != null)
        {
            controller.npcName = spec.DisplayName;
            EditorUtility.SetDirty(controller);
        }

        UI_NameTagView nameTag = npcInstance.GetComponentInChildren<UI_NameTagView>(true);
        if (nameTag != null)
            nameTag.SetName(spec.DisplayName);

        foreach (TextMeshProUGUI tmp in npcInstance.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (!tmp.transform.IsChildOf(npcInstance.transform))
                continue;

            string hierarchy = GetHierarchyPath(tmp.transform);
            if (hierarchy.IndexOf("QuestNpcIndicator", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            if (hierarchy.IndexOf("TagName", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hierarchy.IndexOf("NameTag", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                tmp.text = spec.DisplayName;
                EditorUtility.SetDirty(tmp);
            }
        }

        QuestNpcIndicator indicator = npcInstance.GetComponentInChildren<QuestNpcIndicator>(true);
        if (indicator != null)
            SetStringField(indicator, "npcId", spec.NpcId);

        NpcQuestGiver giver = npcInstance.GetComponent<NpcQuestGiver>();
        if (giver == null)
            giver = npcInstance.AddComponent<NpcQuestGiver>();
        SetStringField(giver, "npcId", spec.NpcId);
    }

    static void AppendCatalogEntries(NpcCatalogSO catalog)
    {
        var so = new SerializedObject(catalog);
        SerializedProperty entries = so.FindProperty("entries");
        if (entries == null)
        {
            Debug.LogError("[OpenWorldNpcLocationPopulator] Catalog 'entries' property not found.");
            return;
        }

        var existing = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty idProp = entries.GetArrayElementAtIndex(i).FindPropertyRelative("id");
            if (idProp != null && !string.IsNullOrEmpty(idProp.stringValue))
                existing.Add(idProp.stringValue);
        }

        // Unique specs by id (Cannoneer appears twice for marker alias)
        var uniqueById = new Dictionary<string, NpcSpec>(StringComparer.Ordinal);
        foreach (NpcSpec spec in Specs)
            uniqueById[spec.NpcId] = spec;

        int added = 0;
        foreach (KeyValuePair<string, NpcSpec> pair in uniqueById)
        {
            if (existing.Contains(pair.Key))
                continue;

            int index = entries.arraySize;
            entries.arraySize++;
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("id").stringValue = pair.Value.NpcId;
            entry.FindPropertyRelative("displayName").stringValue = pair.Value.DisplayName;
            existing.Add(pair.Key);
            added++;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        Debug.Log($"[OpenWorldNpcLocationPopulator] Catalog appended {added} entr(y/ies).");
    }

    static Transform FindMarker(Transform root, string markerName)
    {
        if (string.IsNullOrEmpty(markerName))
            return null;

        Transform exact = FindChildByName(root, markerName);
        if (exact != null)
            return exact;

        string trimmed = markerName.Trim();
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (string.Equals(child.name.Trim(), trimmed, StringComparison.Ordinal))
                return child;
        }

        return null;
    }

    static Transform FindChildByName(Transform root, string name)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == name)
                return child;
        }

        return null;
    }

    static void DestroyMarkerIfPresent(GameObject root, string markerName)
    {
        Transform marker = FindMarker(root.transform, markerName);
        if (marker != null)
            DestroyMarker(marker.gameObject);
    }

    static void DestroyMarker(GameObject marker)
    {
        UnityEngine.Object.DestroyImmediate(marker);
    }

    static void SetStringField(UnityEngine.Object target, string propertyName, string value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            Debug.LogWarning(
                $"[OpenWorldNpcLocationPopulator] Property '{propertyName}' not found on {target.GetType().Name}.");
            return;
        }

        prop.stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static string GetHierarchyPath(Transform t)
    {
        var parts = new List<string>();
        while (t != null)
        {
            parts.Add(t.name);
            t = t.parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }
}
#endif

