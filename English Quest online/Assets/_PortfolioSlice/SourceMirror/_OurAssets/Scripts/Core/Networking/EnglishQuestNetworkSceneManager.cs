using System.Collections;
using System.Collections.Generic;
using System.Text;
using Fusion;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityServiceLocator;

/// <summary>
/// Fusion scene manager that preserves the loaded scene's own baked lightmaps and RenderSettings when
/// scenes merge in multi-peer mode. Fusion multi-peer always loads additively and merges into
/// <see cref="NetworkRunner.SimulationUnityScene"/>, even when <see cref="LoadSceneMode.Single"/> is
/// requested. Unity only applies a level's lighting when that scene is active, so we briefly activate the
/// newly loaded scene to capture its own lightmaps/RenderSettings before the merge, then restore that
/// snapshot onto the simulation scene afterward.
/// </summary>
public class EnglishQuestNetworkSceneManager : NetworkSceneManagerDefault
{
    const string LogPrefix = "[EnglishQuestNetworkSceneManager]";

    SceneEnvironmentSnapshot _pendingEnvironmentSnapshot;
    LightmapSnapshot _pendingLightmapSnapshot;
    bool _hasPendingEnvironmentSnapshot;
    string _pendingSourceSceneName;

    protected override IEnumerator OnSceneLoaded(SceneRef sceneRef, Scene scene, NetworkLoadSceneParameters sceneParams)
    {
        bool captureEnvironment = FusionPeerModeUtility.IsMultiplePeer
                                  && scene.IsValid()
                                  && scene.isLoaded;
        SceneEnvironmentSnapshot environmentSnapshot = default;
        LightmapSnapshot lightmapSnapshot = default;

        LogSceneLoadStart(sceneRef, scene, sceneParams, captureEnvironment);

        if (captureEnvironment)
        {
            Scene activeSceneBefore = SceneManager.GetActiveScene();
            if (activeSceneBefore != scene)
            {
                AppLog.Info($"{LogPrefix} Setting active scene from '{activeSceneBefore.name}' to '{scene.name}' before capture.");
                SceneManager.SetActiveScene(scene);
            }

            // Unity may apply a scene's Lighting Settings on the next frame after SetActiveScene.
            yield return null;

            LogRenderSettingsDiagnostics("BeforeCapture", scene);
            LogLightmapDiagnostics("BeforeCapture", scene, LightmapSettings.lightmaps, LightmapSettings.lightmapsMode);

            environmentSnapshot = SceneEnvironmentSnapshot.Capture();
            lightmapSnapshot = LightmapSnapshot.Capture();

            LogRenderSettingsDiagnostics("AfterCapture", scene, environmentSnapshot);
            LogLightmapDiagnostics("AfterCapture", scene, lightmapSnapshot.Lightmaps, lightmapSnapshot.LightmapsMode, lightmapSnapshot);
        }
        else
        {
            AppLog.Info(
                $"{LogPrefix} Skipping environment capture. peerMode={Runner.Config.PeerMode}, " +
                $"sceneValid={scene.IsValid()}, sceneLoaded={scene.isLoaded}.");
        }

        LightmapData[] lightmapsAfterMerge = LightmapSettings.lightmaps;
        LightmapsMode lightmapsModeAfterMerge = LightmapSettings.lightmapsMode;
        string sourceSceneName = scene.IsValid() ? scene.name : string.Empty;

        yield return base.OnSceneLoaded(sceneRef, scene, sceneParams);

        lightmapsAfterMerge = LightmapSettings.lightmaps;
        lightmapsModeAfterMerge = LightmapSettings.lightmapsMode;

        Scene simulationScene = Runner.SimulationUnityScene;
        if (simulationScene.IsValid() && simulationScene.isLoaded)
            ServiceLocator.RefreshForScene(simulationScene);

        LogLightmapDiagnostics(
            "AfterMerge",
            simulationScene.IsValid() ? simulationScene : scene,
            lightmapsAfterMerge,
            lightmapsModeAfterMerge);

        if (!captureEnvironment)
            yield break;

        if (SceneManager.GetActiveScene() != simulationScene)
        {
            AppLog.Info($"{LogPrefix} Setting active scene to simulation scene '{simulationScene.name}' before restore.");
            SceneManager.SetActiveScene(simulationScene);
        }

        LogRenderSettingsDiagnostics("AfterMergeBeforeRestore", simulationScene);

        _pendingEnvironmentSnapshot = environmentSnapshot;
        _pendingLightmapSnapshot = lightmapSnapshot;
        _pendingSourceSceneName = sourceSceneName;
        _hasPendingEnvironmentSnapshot = true;

        ApplyPendingEnvironment("AfterMergeRestore", simulationScene);
    }

    /// <summary>
    /// Re-applies the last captured gameplay scene environment onto global RenderSettings.
    /// Call after any post-load <see cref="SceneManager.SetActiveScene"/> that can overwrite them
    /// (e.g. menu unload in <see cref="GameNetworkManager.AdoptFusionGameplaySceneAsync"/>).
    /// </summary>
    public void ReapplyCapturedEnvironment()
    {
        if (!_hasPendingEnvironmentSnapshot)
            return;

        StartCoroutine(ReapplyCapturedEnvironmentRoutine());
    }

    IEnumerator ReapplyCapturedEnvironmentRoutine()
    {
        Scene simulationScene = ResolveSimulationScene();
        ApplyPendingEnvironment("AfterGameplayAdopt", simulationScene);

        // RenderSettings are global, but renderer probe SH can stay stale for a frame after menu unload.
        yield return null;
        RefreshGlobalIllumination(simulationScene, refreshRendererProbes: true);
        LogRenderSettingsDiagnostics("AfterGameplayAdoptNextFrame", simulationScene, _pendingEnvironmentSnapshot);
    }

    Scene ResolveSimulationScene()
    {
        Scene simulationScene = Runner != null ? Runner.SimulationUnityScene : default;
        if (!simulationScene.IsValid() || !simulationScene.isLoaded)
            simulationScene = SceneManager.GetActiveScene();

        return simulationScene;
    }

    void ApplyPendingEnvironment(string stage, Scene simulationScene)
    {
        _pendingLightmapSnapshot.Apply();
        _pendingEnvironmentSnapshot.Apply();

        AppLog.Info(
            $"{LogPrefix} {stage}: restored environment for '{_pendingSourceSceneName}'. " +
            DescribeLightmapArray(_pendingLightmapSnapshot.Lightmaps, _pendingLightmapSnapshot.LightmapsMode));

        LogRenderSettingsDiagnostics(stage, simulationScene, _pendingEnvironmentSnapshot);
        RefreshGlobalIllumination(simulationScene, refreshRendererProbes: false);

        LogLightmapDiagnostics(
            stage,
            simulationScene,
            LightmapSettings.lightmaps,
            LightmapSettings.lightmapsMode,
            _pendingLightmapSnapshot);
    }

    static void RefreshGlobalIllumination(Scene scene, bool refreshRendererProbes)
    {
        LightProbes.Tetrahedralize();
        DynamicGI.UpdateEnvironment();

        if (!refreshRendererProbes || !scene.IsValid() || !scene.isLoaded)
            return;

        var roots = new List<GameObject>();
        scene.GetRootGameObjects(roots);

        for (int i = 0; i < roots.Count; i++)
        {
            Renderer[] renderers = roots[i].GetComponentsInChildren<Renderer>(true);
            for (int j = 0; j < renderers.Length; j++)
            {
                Renderer renderer = renderers[j];
                LightProbeUsage usage = renderer.lightProbeUsage;
                if (usage == LightProbeUsage.Off)
                    continue;

                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.lightProbeUsage = usage;
            }
        }
    }

    static void LogRenderSettingsDiagnostics(
        string stage,
        Scene scene,
        SceneEnvironmentSnapshot capturedSnapshot = default)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        AppLog.Info(
            $"{LogPrefix} {stage}: scene='{scene.name}' activeScene='{activeScene.name}' " +
            DescribeRenderSettings(RenderSettings.skybox, RenderSettings.ambientMode, RenderSettings.fog, RenderSettings.sun));

        if (activeScene != scene)
        {
            AppLog.Warning(
                $"{LogPrefix} {stage}: active scene '{activeScene.name}' does not match target scene '{scene.name}'. " +
                "RenderSettings may still reflect the previous scene (e.g. Menu).");
        }

        if (capturedSnapshot.HasValue)
        {
            AppLog.Info(
                $"{LogPrefix} {stage}: capturedSnapshot " +
                DescribeRenderSettings(
                    capturedSnapshot.Skybox,
                    capturedSnapshot.AmbientMode,
                    capturedSnapshot.FogEnabled,
                    capturedSnapshot.Sun));
        }
    }

    static string DescribeRenderSettings(Material skybox, AmbientMode ambientMode, bool fog, Light sun)
    {
        return
            $"skybox={(skybox != null ? skybox.name : "null")}, " +
            $"ambientMode={ambientMode}, fog={fog}, sun={(sun != null ? sun.name : "null")}";
    }

    static void LogSceneLoadStart(
        SceneRef sceneRef,
        Scene scene,
        NetworkLoadSceneParameters sceneParams,
        bool captureEnvironment)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        AppLog.Info(
            $"{LogPrefix} OnSceneLoaded sceneRef={sceneRef} scene='{scene.name}' " +
            $"buildIndex={scene.buildIndex} loadMode={sceneParams.LoadSceneMode} " +
            $"activeScene='{activeScene.name}' captureEnvironment={captureEnvironment}.");
    }

    static void LogLightmapDiagnostics(
        string stage,
        Scene scene,
        LightmapData[] lightmaps,
        LightmapsMode lightmapsMode,
        LightmapSnapshot capturedSnapshot = default)
    {
        CountBakedRenderersInScene(scene, out int rendererCount, out int bakedRendererCount, out int maxRendererLightmapIndex);

        AppLog.Info(
            $"{LogPrefix} {stage}: scene='{scene.name}' activeScene='{SceneManager.GetActiveScene().name}' " +
            DescribeLightmapArray(lightmaps, lightmapsMode) +
            $", renderers={rendererCount}, bakedRenderers={bakedRendererCount}, maxRendererLightmapIndex={maxRendererLightmapIndex}.");

        if (capturedSnapshot.Lightmaps != null)
        {
            AppLog.Info(
                $"{LogPrefix} {stage}: capturedSnapshot has {capturedSnapshot.Lightmaps.Length} lightmap(s), " +
                $"lightProbes={(capturedSnapshot.LightProbes != null ? capturedSnapshot.LightProbes.name : "null")}.");
        }

        if (bakedRendererCount > 0 && (lightmaps == null || lightmaps.Length == 0))
        {
            AppLog.Warning(
                $"{LogPrefix} {stage}: scene '{scene.name}' has {bakedRendererCount} baked renderer(s) " +
                $"up to lightmap index {maxRendererLightmapIndex}, but LightmapSettings.lightmaps is empty.");
        }

        if (lightmaps != null && lightmaps.Length > 0)
        {
            int missingColorMaps = 0;
            int missingDirectionMaps = 0;
            for (int i = 0; i < lightmaps.Length; i++)
            {
                if (lightmaps[i].lightmapColor == null)
                    missingColorMaps++;

                if (lightmaps[i].lightmapDir == null)
                    missingDirectionMaps++;
            }

            if (missingColorMaps > 0 || missingDirectionMaps > 0)
            {
                AppLog.Warning(
                    $"{LogPrefix} {stage}: lightmap textures missing. " +
                    $"missingColorMaps={missingColorMaps}, missingDirectionMaps={missingDirectionMaps}.");
            }
        }
    }

    static string DescribeLightmapArray(LightmapData[] lightmaps, LightmapsMode lightmapsMode)
    {
        if (lightmaps == null || lightmaps.Length == 0)
            return $"lightmaps=0 mode={lightmapsMode}";

        var builder = new StringBuilder();
        builder.Append($"lightmaps={lightmaps.Length} mode={lightmapsMode}");

        int previewCount = Mathf.Min(lightmaps.Length, 4);
        for (int i = 0; i < previewCount; i++)
        {
            Texture2D color = lightmaps[i].lightmapColor;
            Texture2D direction = lightmaps[i].lightmapDir;
            builder.Append(
                $", [{i}] color={(color != null ? color.name : "null")} dir={(direction != null ? direction.name : "null")}");
        }

        if (lightmaps.Length > previewCount)
            builder.Append($", ... +{lightmaps.Length - previewCount} more");

        return builder.ToString();
    }

    static void CountBakedRenderersInScene(
        Scene scene,
        out int rendererCount,
        out int bakedRendererCount,
        out int maxRendererLightmapIndex)
    {
        rendererCount = 0;
        bakedRendererCount = 0;
        maxRendererLightmapIndex = -1;

        if (!scene.IsValid() || !scene.isLoaded)
            return;

        var roots = new List<GameObject>();
        scene.GetRootGameObjects(roots);

        for (int i = 0; i < roots.Count; i++)
        {
            Renderer[] renderers = roots[i].GetComponentsInChildren<Renderer>(true);
            for (int j = 0; j < renderers.Length; j++)
            {
                Renderer renderer = renderers[j];
                rendererCount++;

                int lightmapIndex = renderer.lightmapIndex;
                if (lightmapIndex < 0)
                    continue;

                bakedRendererCount++;
                if (lightmapIndex > maxRendererLightmapIndex)
                    maxRendererLightmapIndex = lightmapIndex;
            }
        }
    }

    readonly struct LightmapSnapshot
    {
        public LightmapData[] Lightmaps { get; }
        public LightmapsMode LightmapsMode { get; }
        public LightProbes LightProbes { get; }

        LightmapSnapshot(LightmapData[] lightmaps, LightmapsMode lightmapsMode, LightProbes lightProbes)
        {
            Lightmaps = lightmaps;
            LightmapsMode = lightmapsMode;
            LightProbes = lightProbes;
        }

        public static LightmapSnapshot Capture()
        {
            LightmapData[] sourceLightmaps = LightmapSettings.lightmaps;
            LightmapData[] copiedLightmaps = sourceLightmaps != null && sourceLightmaps.Length > 0
                ? (LightmapData[])sourceLightmaps.Clone()
                : sourceLightmaps;

            return new LightmapSnapshot(
                copiedLightmaps,
                LightmapSettings.lightmapsMode,
                LightmapSettings.lightProbes);
        }

        public void Apply()
        {
            LightmapSettings.lightmapsMode = LightmapsMode;
            LightmapSettings.lightProbes = LightProbes;
            LightmapSettings.lightmaps = Lightmaps;
        }
    }

    private readonly struct SceneEnvironmentSnapshot
    {
        readonly bool _wasCaptured;
        readonly Material _skybox;
        readonly Light _sun;
        readonly AmbientMode _ambientMode;
        readonly Color _ambientLight;
        readonly float _ambientIntensity;
        readonly Color _ambientSkyColor;
        readonly Color _ambientEquatorColor;
        readonly Color _ambientGroundColor;
        readonly bool _fog;
        readonly Color _fogColor;
        readonly FogMode _fogMode;
        readonly float _fogDensity;
        readonly float _fogStartDistance;
        readonly float _fogEndDistance;
        readonly DefaultReflectionMode _defaultReflectionMode;
        readonly int _defaultReflectionResolution;
        readonly float _reflectionIntensity;
        readonly int _reflectionBounces;
        readonly Texture _customReflection;

        SceneEnvironmentSnapshot(
            bool wasCaptured,
            Material skybox,
            Light sun,
            AmbientMode ambientMode,
            Color ambientLight,
            float ambientIntensity,
            Color ambientSkyColor,
            Color ambientEquatorColor,
            Color ambientGroundColor,
            bool fog,
            Color fogColor,
            FogMode fogMode,
            float fogDensity,
            float fogStartDistance,
            float fogEndDistance,
            DefaultReflectionMode defaultReflectionMode,
            int defaultReflectionResolution,
            float reflectionIntensity,
            int reflectionBounces,
            Texture customReflection)
        {
            _wasCaptured = wasCaptured;
            _skybox = skybox;
            _sun = sun;
            _ambientMode = ambientMode;
            _ambientLight = ambientLight;
            _ambientIntensity = ambientIntensity;
            _ambientSkyColor = ambientSkyColor;
            _ambientEquatorColor = ambientEquatorColor;
            _ambientGroundColor = ambientGroundColor;
            _fog = fog;
            _fogColor = fogColor;
            _fogMode = fogMode;
            _fogDensity = fogDensity;
            _fogStartDistance = fogStartDistance;
            _fogEndDistance = fogEndDistance;
            _defaultReflectionMode = defaultReflectionMode;
            _defaultReflectionResolution = defaultReflectionResolution;
            _reflectionIntensity = reflectionIntensity;
            _reflectionBounces = reflectionBounces;
            _customReflection = customReflection;
        }

        public bool HasValue => _wasCaptured;

        public Material Skybox => _skybox;
        public AmbientMode AmbientMode => _ambientMode;
        public bool FogEnabled => _fog;
        public Light Sun => _sun;

        public static SceneEnvironmentSnapshot Capture()
        {
            return new SceneEnvironmentSnapshot(
                true,
                RenderSettings.skybox,
                RenderSettings.sun,
                RenderSettings.ambientMode,
                RenderSettings.ambientLight,
                RenderSettings.ambientIntensity,
                RenderSettings.ambientSkyColor,
                RenderSettings.ambientEquatorColor,
                RenderSettings.ambientGroundColor,
                RenderSettings.fog,
                RenderSettings.fogColor,
                RenderSettings.fogMode,
                RenderSettings.fogDensity,
                RenderSettings.fogStartDistance,
                RenderSettings.fogEndDistance,
                RenderSettings.defaultReflectionMode,
                RenderSettings.defaultReflectionResolution,
                RenderSettings.reflectionIntensity,
                RenderSettings.reflectionBounces,
                RenderSettings.customReflectionTexture);
        }

        public void Apply()
        {
            RenderSettings.skybox = _skybox;
            RenderSettings.sun = _sun;
            RenderSettings.ambientMode = _ambientMode;
            RenderSettings.ambientLight = _ambientLight;
            RenderSettings.ambientIntensity = _ambientIntensity;
            RenderSettings.ambientSkyColor = _ambientSkyColor;
            RenderSettings.ambientEquatorColor = _ambientEquatorColor;
            RenderSettings.ambientGroundColor = _ambientGroundColor;
            RenderSettings.fog = _fog;
            RenderSettings.fogColor = _fogColor;
            RenderSettings.fogMode = _fogMode;
            RenderSettings.fogDensity = _fogDensity;
            RenderSettings.fogStartDistance = _fogStartDistance;
            RenderSettings.fogEndDistance = _fogEndDistance;
            RenderSettings.defaultReflectionMode = _defaultReflectionMode;
            RenderSettings.defaultReflectionResolution = _defaultReflectionResolution;
            RenderSettings.reflectionIntensity = _reflectionIntensity;
            RenderSettings.reflectionBounces = _reflectionBounces;
            RenderSettings.customReflectionTexture = _customReflection;
        }
    }
}

