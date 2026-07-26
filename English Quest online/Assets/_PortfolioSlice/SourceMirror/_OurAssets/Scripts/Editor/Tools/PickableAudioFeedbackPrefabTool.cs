#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PickableAudioFeedbackPrefabTool
{
    private const string DefaultPickupSoundPath =
        "Assets/_OurAssets/Art/Audio/GamePlay/PickUpItem/PickupBubbleSound.wav";

    [MenuItem("Tools/Pickables/Add Audio Feedback To Pickable Prefabs")] // Adds a button to Unity's top menu
    private static void AddAudioFeedbackToPickablePrefabs()
    {
        AudioClip defaultPickupSound =
            AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultPickupSoundPath);

        if (defaultPickupSound == null)
        {
            AppLog.Error($"Default pickup sound not found at: {DefaultPickupSoundPath}");
            return;
        }

        // Finds all prefab assets in the project, avoids scanning Assets/_ThirdParty, Photon, Feel, etc.
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_OurAssets" }); 

        int changedCount = 0;

        foreach (string guid in prefabGuids)
        {
            // Each GUID is converted to an asset path
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Opens the prefab contents in memory so the tool can edit it safely
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);

            bool changed = false;

            // It looks inside that prefab for every PickableItem, including inactive children
            PickableItem[] pickables = prefabRoot.GetComponentsInChildren<PickableItem>(true);

            foreach (PickableItem pickable in pickables)
            {
                // For each pickable object, it checks whether the PickableItemAudioFeedback script already exists
                PickableItemAudioFeedback feedback =
                    pickable.GetComponent<PickableItemAudioFeedback>();

                // If missing, it adds it
                if (feedback == null)
                {
                    feedback = pickable.gameObject.AddComponent<PickableItemAudioFeedback>();
                    changed = true;
                }

                // if default sound assignment is included, it uses SerializedObject because pickupSound is a private serialized field
                SerializedObject serializedFeedback = new SerializedObject(feedback);
                SerializedProperty pickupSoundProp =
                    serializedFeedback.FindProperty("pickupSound");

                // Assigns the clip only if the field is empty:
                if (pickupSoundProp != null &&
                    pickupSoundProp.objectReferenceValue == null)
                {
                    pickupSoundProp.objectReferenceValue = defaultPickupSound;
                    serializedFeedback.ApplyModifiedProperties();
                    changed = true;
                }
            }

            // If anything changed in that prefab, the tool saves it
            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                changedCount++;
            }

            // Unloads the temporary prefab copy from memory
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        // Saves all changed assets and prints a summary
        AssetDatabase.SaveAssets();
        AppLog.Info($"Updated {changedCount} pickable prefab assets.");
    }
}
#endif