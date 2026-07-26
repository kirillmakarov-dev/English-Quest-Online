using System.Reflection;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures harness travel destinations have a spawn point after Fusion scene loads.
/// </summary>
public static class HarnessTravelSpawnUtility
{
    public const string HarnessMapId = "travel_harness_v1";
    public const string HarnessDestinationSceneName = "GameplayTestScene";
    public const string HarnessDestinationSpawnId = "gameplay_dest";

    public static bool IsHarnessMapActive() =>
        WorldMapDefinitionLoader.ResolveByMapId(HarnessMapId) != null;

    public static void TryEnsureDestinationSpawn(NetworkRunner runner, Scene scene)
    {
        if (!IsHarnessMapActive() || runner == null || !runner.IsRunning)
            return;

        if (!scene.IsValid() || !scene.isLoaded)
            return;

        if (!SceneLoadWaitUtility.TryResolveLoadedDestinationScene(
                ResolveDestinationBuildIndex(),
                HarnessDestinationSceneName,
                null,
                runner,
                out Scene destinationScene))
        {
            return;
        }

        EnsureDestinationSpawn(destinationScene);
    }

    public static void EnsureDestinationSpawn(Scene destinationScene)
    {
        if (!destinationScene.IsValid() || !destinationScene.isLoaded)
            return;

        if (PlayerSpawnPoint.TryGetSpawnForNode(
                HarnessDestinationSpawnId,
                destinationScene,
                out _,
                out _))
        {
            return;
        }

        var spawnRoot = new GameObject("Harness Destination Spawn");
        SceneManager.MoveGameObjectToScene(spawnRoot, destinationScene);
        PlayerSpawnPoint spawnPoint = spawnRoot.AddComponent<PlayerSpawnPoint>();

        FieldInfo travelNodeField = typeof(PlayerSpawnPoint).GetField(
            "_travelNodeId",
            BindingFlags.Instance | BindingFlags.NonPublic);
        travelNodeField?.SetValue(spawnPoint, HarnessDestinationSpawnId);

        AppLog.Info(
            $"[HarnessTravelSpawnUtility] Injected destination spawn '{HarnessDestinationSpawnId}' into '{destinationScene.name}'.");
    }

    private static int ResolveDestinationBuildIndex()
    {
        const string scenePath = "Assets/_OurAssets/Scenes/TestScenes/GameplayTestScene.unity";
        return SceneUtility.GetBuildIndexByScenePath(scenePath);
    }
}
