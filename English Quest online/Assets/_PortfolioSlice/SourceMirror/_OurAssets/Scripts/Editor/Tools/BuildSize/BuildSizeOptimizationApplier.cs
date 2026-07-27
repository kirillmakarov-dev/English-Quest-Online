using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EnglishQuest.Editor.Tools.BuildSize
{
    /// <summary>
    /// Applies import and build-profile settings from the Build Size Reduction plan.
    /// Menu: Tools → English Kingdom → Optimization → Build Size → Apply All
    /// Batch: Unity -batchmode -quit -projectPath ... -executeMethod EnglishQuest.Editor.Tools.BuildSize.BuildSizeOptimizationApplier.ApplyAllFromCommandLine
    /// </summary>
    public static class BuildSizeOptimizationApplier
    {
        private const string BaselineLogPath = "Logs/BuildSizeBaseline.txt";
        private const string ReportLogPath = "Logs/BuildSizeOptimizationReport.txt";

        private const string SkyboxFolder = "Assets/_ThirdParty/8K Skybox Pack Free/Skyboxes/Texture";
        private const string OurSpritesRoot = "Assets/_OurAssets/Art/Sprites";

        private static readonly string[] ExtraSkyboxPaths =
        {
            "Assets/_ThirdParty/Yanshi - Stylized Rocky Island Environment/Textures/sky/sky_cloud.psd",
            "Assets/Polyart/PolyartStudio/DreamscapeCastle/Textures/Sky/T_SkyboxCastle_C.png",
            "Assets/_ThirdParty/Polytope Studio/Lowpoly_Environments/Sources/Textures/PT_Skybox_Texture_01.png",
        };

        private static readonly string[] EnvironmentTextureFolders =
        {
            "Assets/_ThirdParty/@PaulosCreations/RunesAndPortals",
            "Assets/Polyart/PolyartStudio/DreamscapeCastle/Textures",
            "Assets/_ThirdParty/Yanshi - Stylized Rocky Island Environment/Textures",
            "Assets/_ThirdParty/Sci-Fi Tomb/Textures",
        };

        private static readonly string[] AnimalFbxPaths =
        {
            "Assets/_ThirdParty/UnRealProject/Animals/owl.FBX",
            "Assets/_ThirdParty/UnRealProject/Animals/lion.FBX",
        };

        private static readonly string[] TerrainDataPaths =
        {
            "Assets/_OurAssets/Data/Terrain/CastleTerrain_OpenWorld.asset",
        };

        private const string FullGameBuildProfilePath = "Assets/Settings/Build Profiles/FullGame.asset";

        [MenuItem("Tools/English Kingdom/Optimization/Build Size/Record Baseline")]
        public static void RecordBaseline()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Build Size Baseline (pre-optimization reference)");
            sb.AppendLine($"Recorded: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine();
            sb.AppendLine("From prior FullGame build report:");
            sb.AppendLine("  Complete build size: 8.7 GB");
            sb.AppendLine("  Textures: 3.7 GB (59.3%)");
            sb.AppendLine("  Meshes: 2.1 GB (33.7%) — static batching kept for performance");
            sb.AppendLine("  Levels: 331 MB (5.1%)");
            sb.AppendLine();
            sb.AppendLine("Run Project Auditor: Window → Analysis → Project Auditor");
            sb.AppendLine("Capture 3–5 benchmark screenshots before comparing visuals.");

            WriteLog(BaselineLogPath, sb.ToString());
            Debug.Log($"[BuildSize] Baseline written to {BaselineLogPath}");
        }

        [MenuItem("Tools/English Kingdom/Optimization/Build Size/Apply All Optimizations")]
        public static void ApplyAll()
        {
            RecordBaseline();

            var report = new StringBuilder();
            report.AppendLine($"Build Size Optimization — {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            report.AppendLine();

            int skyboxes = ApplySkyboxTextures(report);
            int uiSprites = ApplyUISprites(report);
            int envTextures = ApplyEnvironmentTextures(report);
            int fbx = ApplyAnimalFbx(report);
            int terrain = ApplyTerrainTuning(report);
            bool packaging = ApplyBuildProfileLz4Hc(report);
            BuildSizeUnreferencedAssetScanner.ScanAndReport();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            report.AppendLine();
            report.AppendLine("Summary:");
            report.AppendLine($"  Skybox textures updated: {skyboxes}");
            report.AppendLine($"  UI sprites updated: {uiSprites}");
            report.AppendLine($"  Environment textures updated: {envTextures}");
            report.AppendLine($"  Animal FBX updated: {fbx}");
            report.AppendLine($"  Terrain assets tuned: {terrain}");
            report.AppendLine($"  FullGame LZ4HC: {(packaging ? "enabled" : "failed")}");
            report.AppendLine();
            report.AppendLine("Next: rebuild FullGame and compare folder size to 8.7 GB baseline.");

            WriteLog(ReportLogPath, report.ToString());
            Debug.Log($"[BuildSize] Done. Report: {ReportLogPath}\n{report}");
        }

        public static void ApplyAllFromCommandLine()
        {
            ApplyAll();
            EditorApplication.Exit(0);
        }

        private static int ApplySkyboxTextures(StringBuilder report)
        {
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SkyboxFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/Materials/"))
                    continue;

                if (TryConfigureSkybox(path))
                {
                    count++;
                    report.AppendLine($"  Skybox: {path}");
                }
            }

            foreach (string path in ExtraSkyboxPaths)
            {
                if (!File.Exists(path))
                    continue;
                if (TryConfigureSkybox(path))
                {
                    count++;
                    report.AppendLine($"  Skybox: {path}");
                }
            }

            report.AppendLine($"Skyboxes: {count} texture(s)");
            return count;
        }

        private static bool TryConfigureSkybox(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return false;

            importer.textureShape = TextureImporterShape.TextureCube;
            importer.mipmapEnabled = true;
            importer.sRGBTexture = true;
            importer.isReadable = false;
            importer.streamingMipmaps = true;

            ApplyStandaloneSettings(importer, maxSize: 2048, format: TextureImporterFormat.BC7, crunch: false, quality: 50);
            importer.SaveAndReimport();
            return true;
        }

        private static int ApplyUISprites(StringBuilder report)
        {
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { OurSpritesRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.sRGBTexture = true;
                importer.alphaIsTransparency = true;
                importer.isReadable = false;

                int maxSize = path.Contains("LineMatch") ? 1024 : 1024;
                if (path.Contains("Tutorials", StringComparison.OrdinalIgnoreCase))
                    maxSize = 512;

                ApplyStandaloneSettings(importer, maxSize, TextureImporterFormat.BC7, crunch: true, quality: 85);
                importer.SaveAndReimport();
                count++;
            }

            report.AppendLine($"UI sprites: {count} texture(s) under {OurSpritesRoot}");
            return count;
        }

        private static int ApplyEnvironmentTextures(StringBuilder report)
        {
            int count = 0;
            foreach (string folder in EnvironmentTextureFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                    continue;

                foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null)
                        continue;

                    if (importer.textureShape == TextureImporterShape.TextureCube)
                        continue;

                    EnvironmentTextureKind kind = ClassifyEnvironmentTexture(path);
                    ConfigureEnvironmentTexture(importer, kind, path);
                    importer.SaveAndReimport();
                    count++;
                }
            }

            report.AppendLine($"Environment textures: {count} in priority packs");
            return count;
        }

        private enum EnvironmentTextureKind
        {
            Albedo,
            Normal,
            Mask,
            Distant,
        }

        private static EnvironmentTextureKind ClassifyEnvironmentTexture(string path)
        {
            string file = Path.GetFileName(path).ToLowerInvariant();
            string full = path.ToLowerInvariant();

            if (full.Contains("/distant/") || full.Contains("/terrain/distant/"))
                return EnvironmentTextureKind.Distant;

            if (file.Contains("_n.") || file.Contains("_normal") || file.Contains("normal.")
                || file.EndsWith("_n.png") || file.EndsWith("_n.tga") || file.EndsWith("_n.psd"))
                return EnvironmentTextureKind.Normal;

            if (file.Contains("metallic") || file.Contains("metsmooth") || file.Contains("_mso")
                || file.Contains("_as.") || file.Contains("smoothness") || file.Contains("occlusion")
                || file.Contains("_mask") || file.Contains("emit"))
                return EnvironmentTextureKind.Mask;

            return EnvironmentTextureKind.Albedo;
        }

        private static void ConfigureEnvironmentTexture(TextureImporter importer, EnvironmentTextureKind kind, string path)
        {
            importer.isReadable = false;
            importer.streamingMipmaps = true;

            switch (kind)
            {
                case EnvironmentTextureKind.Normal:
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.sRGBTexture = false;
                    importer.mipmapEnabled = true;
                    ApplyStandaloneSettings(importer, 2048, TextureImporterFormat.BC5, crunch: false, quality: 50);
                    break;
                case EnvironmentTextureKind.Mask:
                    importer.textureType = TextureImporterType.Default;
                    importer.sRGBTexture = false;
                    importer.mipmapEnabled = true;
                    ApplyStandaloneSettings(importer, 1024, TextureImporterFormat.BC7, crunch: false, quality: 50);
                    break;
                case EnvironmentTextureKind.Distant:
                    importer.mipmapEnabled = true;
                    if (path.ToLowerInvariant().Contains("_n."))
                    {
                        importer.textureType = TextureImporterType.NormalMap;
                        importer.sRGBTexture = false;
                        ApplyStandaloneSettings(importer, 1024, TextureImporterFormat.BC5, crunch: false, quality: 50);
                    }
                    else
                    {
                        importer.textureType = TextureImporterType.Default;
                        importer.sRGBTexture = true;
                        ApplyStandaloneSettings(importer, 1024, TextureImporterFormat.BC7, crunch: true, quality: 85);
                    }
                    break;
                default:
                    importer.textureType = TextureImporterType.Default;
                    importer.sRGBTexture = true;
                    importer.mipmapEnabled = true;
                    ApplyStandaloneSettings(importer, 2048, TextureImporterFormat.BC7, crunch: true, quality: 85);
                    break;
            }
        }

        private static int ApplyAnimalFbx(StringBuilder report)
        {
            int count = 0;
            foreach (string path in AnimalFbxPaths)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                    continue;

                importer.animationCompression = ModelImporterAnimationCompression.Optimal;
                importer.meshCompression = ModelImporterMeshCompression.Medium;
                importer.importCameras = false;
                importer.importLights = false;
                importer.isReadable = false;
                importer.SaveAndReimport();
                count++;
                report.AppendLine($"  FBX: {path}");
            }

            report.AppendLine($"Animal FBX: {count} model(s)");
            return count;
        }

        private static int ApplyTerrainTuning(StringBuilder report)
        {
            int count = 0;
            var paths = new List<string>(TerrainDataPaths);
            foreach (string guid in AssetDatabase.FindAssets("t:TerrainData", new[] { "Assets/_OurAssets/Art/Prefabs/Levels" }))
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));

            foreach (string path in paths)
            {
                var terrain = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                if (terrain == null)
                    continue;

                bool changed = false;

                if (terrain.heightmapResolution > 2049)
                    report.AppendLine($"  Terrain {path}: heightmap {terrain.heightmapResolution} — review manually if needed");

                if (terrain.alphamapResolution > 1024)
                {
                    terrain.alphamapResolution = 1024;
                    changed = true;
                    report.AppendLine($"  Terrain {path}: alphamap → 1024");
                }

                if (terrain.baseMapResolution > 1024)
                {
                    terrain.baseMapResolution = 1024;
                    changed = true;
                    report.AppendLine($"  Terrain {path}: baseMap → 1024");
                }

                if (changed)
                {
                    EditorUtility.SetDirty(terrain);
                    count++;
                }
            }

            report.AppendLine($"Terrain tuned: {count} asset(s)");
            return count;
        }

        private static bool ApplyBuildProfileLz4Hc(StringBuilder report)
        {
            // LZ4HC = 2 in Windows Standalone build profile YAML.
            const int lz4Hc = 2;
            string profilePath = FullGameBuildProfilePath;

            if (!File.Exists(profilePath))
            {
                report.AppendLine("Build profile: FullGame.asset not found");
                return false;
            }

            string yaml = File.ReadAllText(profilePath);
            if (yaml.Contains($"m_CompressionType: {lz4Hc}"))
            {
                report.AppendLine("Build profile: FullGame already uses LZ4HC (m_CompressionType: 2)");
                return true;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(yaml, @"m_CompressionType: \d+"))
            {
                report.AppendLine("Build profile: m_CompressionType not found in FullGame.asset");
                return false;
            }

            yaml = System.Text.RegularExpressions.Regex.Replace(
                yaml,
                @"m_CompressionType: \d+",
                $"m_CompressionType: {lz4Hc}");
            File.WriteAllText(profilePath, yaml);
            AssetDatabase.ImportAsset(profilePath);
            report.AppendLine("Build profile: FullGame m_CompressionType → 2 (LZ4HC)");
            return true;
        }

        private static void ApplyStandaloneSettings(
            TextureImporter importer,
            int maxSize,
            TextureImporterFormat format,
            bool crunch,
            int quality)
        {
            var settings = importer.GetPlatformTextureSettings("Standalone");
            settings.overridden = true;
            settings.maxTextureSize = maxSize;
            settings.format = format;
            settings.textureCompression = TextureImporterCompression.Compressed;
            settings.compressionQuality = quality;
            settings.crunchedCompression = crunch;
            importer.SetPlatformTextureSettings(settings);

            var defaults = importer.GetDefaultPlatformTextureSettings();
            defaults.maxTextureSize = maxSize;
            defaults.textureCompression = TextureImporterCompression.Compressed;
            defaults.compressionQuality = quality;
            defaults.crunchedCompression = crunch;
            importer.SetPlatformTextureSettings(defaults);
        }

        private static void WriteLog(string relativePath, string content)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
            string dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(fullPath, content);
        }
    }
}

