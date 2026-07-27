#if UNITY_EDITOR

using Fusion;
using UnityEditor;
using UnityEngine;

namespace EnglishQuest.Tests.RunTime
{
    /// <summary>
    /// Spawns networked <see cref="PickableItem"/> instances for Play Mode network tests.
    /// Lives in <c>_Project</c> so test assemblies do not need a UnityEditor reference.
    /// </summary>
    public static class NetworkPickableTestSupport
    {
        public const string TestPickableObjectName = "NetworkTestPickable";
        private const string ColorPickupPrefabGuid = "6a0379b123bf4fd489dac7a7d113d458";

        public static bool TryResolveColorPickupPrefab(out NetworkObject prefab)
        {
            prefab = null;

            string path = AssetDatabase.GUIDToAssetPath(ColorPickupPrefabGuid);
            if (string.IsNullOrEmpty(path))
                return false;

            var prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabObject == null || !prefabObject.TryGetComponent(out NetworkObject networkObject))
                return false;

            prefab = networkObject;
            return true;
        }

        public static PickableItem SpawnTestPickable(NetworkRunner runner, Vector3 position)
        {
            if (runner == null || !runner.IsRunning)
                return null;

            if (!runner.SimulationUnityScene.IsValid())
                return null;

            if (!TryResolveColorPickupPrefab(out NetworkObject prefab))
                return null;

            NetworkObject spawnedObject = runner.Spawn(prefab, position, Quaternion.identity);
            if (spawnedObject == null)
                return null;

            spawnedObject.name = TestPickableObjectName;
            return spawnedObject.GetComponent<PickableItem>();
        }
    }
}

#endif

