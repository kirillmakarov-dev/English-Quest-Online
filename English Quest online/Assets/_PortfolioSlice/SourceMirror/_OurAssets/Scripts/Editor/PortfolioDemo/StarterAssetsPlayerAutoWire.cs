using System;
using EnglishKingdom.PortfolioDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class StarterAssetsPlayerAutoWire
{
    private const string PlayerRootName = "PlayerArmature";
    private const string LegacyPlayerRootName = "Player Capsule";
    private const string InteractionZoneName = "Interaction Zone";
    private const string FollowCameraPrefabPath =
        "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerFollowCamera.prefab";

    static StarterAssetsPlayerAutoWire()
    {
        EditorApplication.delayCall += TryAutoWireCurrentScene;
    }

    private static void TryAutoWireCurrentScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        GameObject player = FindRoot(scene, PlayerRootName);
        if (player == null)
            return;

        EnsurePlayerComponents(player);
        EnsureFollowCamera(scene, player.transform);
        DisableLegacyPlayer(scene, player);

        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void EnsurePlayerComponents(GameObject player)
    {
        if (player == null)
            return;

        if (player.GetComponent<PlayerInteraction>() == null)
            Undo.AddComponent<PlayerInteraction>(player);

        if (player.GetComponent<PlayerInteractionController>() == null)
            Undo.AddComponent<PlayerInteractionController>(player);

        if (player.GetComponent<PortfolioPlayerLockService>() == null)
            Undo.AddComponent<PortfolioPlayerLockService>(player);

        EnsureInteractionZone(player);

        PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
        if (interaction != null && interaction.itemHolderPos == null)
        {
            Transform holdPoint = player.transform.Find("Hold Point");
            if (holdPoint == null)
            {
                GameObject holdPointObject = new("Hold Point");
                Undo.RegisterCreatedObjectUndo(holdPointObject, "Create Hold Point");
                holdPointObject.transform.SetParent(player.transform, false);
                holdPointObject.transform.localPosition = new Vector3(0f, 0.6f, 0.8f);
                holdPoint = holdPointObject.transform;
            }

            interaction.itemHolderPos = holdPoint;
            EditorUtility.SetDirty(interaction);
        }
    }

    private static void EnsureFollowCamera(Scene scene, Transform playerRoot)
    {
        if (playerRoot == null)
            return;

        Transform cameraTarget = playerRoot.Find("CinemachineCameraTarget");
        if (cameraTarget == null)
        {
            Debug.LogWarning("[StarterAssetsPlayerAutoWire] PlayerArmature is missing CinemachineCameraTarget.");
            return;
        }

        Component followCamera = FindExistingFollowCamera(scene);
        if (followCamera == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FollowCameraPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[StarterAssetsPlayerAutoWire] Missing prefab: {FollowCameraPrefabPath}");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "PlayerFollowCamera";
            followCamera = FindComponentByTypeName(instance, "CinemachineCamera");
        }

        if (followCamera == null)
        {
            Debug.LogWarning("[StarterAssetsPlayerAutoWire] Could not resolve CinemachineCamera on PlayerFollowCamera.");
            return;
        }

        SetIntMember(followCamera, "Priority", 10);
        SetObjectMember(followCamera, "Follow", cameraTarget);
        SetObjectMember(followCamera, "LookAt", cameraTarget);

        Camera outputCamera = Camera.main;
        if (outputCamera != null && outputCamera.GetComponent("CinemachineBrain") == null)
            AddComponentByTypeName(outputCamera.gameObject, "Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine");
    }

    private static Component FindExistingFollowCamera(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Component camera = FindComponentByTypeNameInChildren(roots[i].transform, "CinemachineCamera");
            if (camera != null)
                return camera;
        }

        return null;
    }

    private static void DisableLegacyPlayer(Scene scene, GameObject replacement)
    {
        GameObject legacy = FindRoot(scene, LegacyPlayerRootName);
        if (legacy == null || legacy == replacement)
            return;

        legacy.SetActive(false);
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        if (!scene.IsValid())
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && roots[i].name == name)
                return roots[i];
        }

        return null;
    }

    private static Component AddComponentByTypeName(GameObject target, string typeName)
    {
        if (target == null)
            return null;

        Type type = Type.GetType(typeName);
        if (type == null || !typeof(Component).IsAssignableFrom(type))
            return null;

        return target.AddComponent(type);
    }

    private static void SetIntMember(Component target, string memberName, int value)
    {
        if (target == null)
            return;

        const System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic;

        Type type = target.GetType();
        System.Reflection.PropertyInfo property = type.GetProperty(memberName, flags);
        if (property != null && property.CanWrite && property.PropertyType == typeof(int))
        {
            property.SetValue(target, value);
            return;
        }

        System.Reflection.FieldInfo field = type.GetField(memberName, flags);
        if (field != null && field.FieldType == typeof(int))
            field.SetValue(target, value);
    }

    private static void SetObjectMember(Component target, string memberName, object value)
    {
        if (target == null)
            return;

        const System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic;

        Type type = target.GetType();
        System.Reflection.PropertyInfo property = type.GetProperty(memberName, flags);
        if (property != null && property.CanWrite)
        {
            property.SetValue(target, value);
            return;
        }

        System.Reflection.FieldInfo field = type.GetField(memberName, flags);
        if (field != null)
            field.SetValue(target, value);
    }

    private static Component FindComponentByTypeName(GameObject target, string typeName)
    {
        if (target == null)
            return null;

        MonoBehaviour[] behaviours = target.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour != null && behaviour.GetType().Name == typeName)
                return behaviour;
        }

        return null;
    }

    private static Component FindComponentByTypeNameInChildren(Transform root, string typeName)
    {
        if (root == null)
            return null;

        MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour != null && behaviour.GetType().Name == typeName)
                return behaviour;
        }

        return null;
    }

    private static void EnsureInteractionZone(GameObject player)
    {
        if (player == null)
            return;

        Transform zone = player.transform.Find(InteractionZoneName);
        if (zone == null)
        {
            GameObject zoneObject = new(InteractionZoneName);
            Undo.RegisterCreatedObjectUndo(zoneObject, "Create Interaction Zone");
            zoneObject.transform.SetParent(player.transform, false);
            zoneObject.transform.localPosition = Vector3.zero;
            zone = zoneObject.transform;
        }

        SphereCollider collider = zone.GetComponent<SphereCollider>();
        if (collider == null)
            collider = Undo.AddComponent<SphereCollider>(zone.gameObject);

        zone.gameObject.layer = 0;
        collider.isTrigger = true;
        collider.radius = 2.4f;

        Rigidbody rigidbody = zone.GetComponent<Rigidbody>();
        if (rigidbody == null)
            rigidbody = Undo.AddComponent<Rigidbody>(zone.gameObject);

        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;
        rigidbody.constraints = RigidbodyConstraints.FreezeAll;

        if (zone.GetComponent<InteractionZoneTrigger>() == null)
            Undo.AddComponent<InteractionZoneTrigger>(zone.gameObject);

        InteractionZoneTrigger trigger = zone.GetComponent<InteractionZoneTrigger>();
        PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
        if (trigger != null && interaction != null)
            SetObjectMember(trigger, "playerInteractionScript", interaction);
    }
}
