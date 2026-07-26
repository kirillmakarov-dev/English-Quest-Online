using System.Text;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EnglishKingdom.Tests.RunTime
{
    /// <summary>
    /// Verbose diagnostics for Play Mode tests. Logs are prefixed so they are easy to grep in Unity console.
    /// </summary>
    internal static class TestDebugLog
    {
        public const string Prefix = "[PlayModeTest]";

        public static void Info(string tag, string message) =>
            Debug.Log($"{Prefix}[{tag}] {message}");

        public static void Warn(string tag, string message) =>
            Debug.LogWarning($"{Prefix}[{tag}] {message}");

        public static void RunnerState(string tag, NetworkRunner runner, string context = null)
        {
            if (runner == null)
            {
                Warn(tag, $"{context ?? "runner"} is null.");
                return;
            }

            Scene simulationScene = runner.SimulationUnityScene;
            string sceneLabel = simulationScene.IsValid()
                ? simulationScene.name
                : "<invalid>";
            int buildIndex = simulationScene.IsValid() ? simulationScene.buildIndex : -1;

            Info(tag,
                $"{context ?? runner.name}: running={runner.IsRunning}, " +
                $"localPlayer={runner.LocalPlayer}, scene={sceneLabel}, buildIndex={buildIndex}, " +
                $"sceneAuthority={runner.IsSceneAuthority}, masterClient={runner.IsSharedModeMasterClient}, " +
                $"session={(runner.SessionInfo.IsValid ? runner.SessionInfo.Name : "<none>")}");
        }

        public static void AllRunners(string tag)
        {
            NetworkRunner[] runners = Object.FindObjectsByType<NetworkRunner>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            Info(tag, $"Found {runners.Length} NetworkRunner(s). Loaded scenes: {FormatLoadedScenes()}");

            for (int i = 0; i < runners.Length; i++)
                RunnerState(tag, runners[i], $"runner[{i}]={runners[i]?.name}");
        }

        public static void LoadedScenes(string tag, string context = null)
        {
            Info(tag, $"{context ?? "Scenes"}: {FormatLoadedScenes()}");
        }

        public static string FormatLoadedScenes()
        {
            var builder = new StringBuilder();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                if (builder.Length > 0)
                    builder.Append(", ");

                builder.Append(scene.name);
                if (SceneManager.GetActiveScene() == scene)
                    builder.Append("(active)");
            }

            return builder.Length == 0 ? "<none>" : builder.ToString();
        }

        public static void TravelPresentation(string tag)
        {
            WorldMapUI mapUi = Object.FindFirstObjectByType<WorldMapUI>(FindObjectsInactive.Include);
            if (mapUi == null)
            {
                Warn(tag, "WorldMapUI not found.");
                return;
            }

            Info(tag,
                $"WorldMapUI: open={mapUi.IsOpen}, travelMode={mapUi.IsInTravelMode}, " +
                $"vehicleVisible={mapUi.IsVehicleVisible}");
        }
    }
}
