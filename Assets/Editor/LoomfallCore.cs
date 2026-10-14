using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Loomfall.EditorTools
{
    /// <summary>
    /// Ядро сборки «Ткача Ярусов»: настройки проекта, свет, постобработка,
    /// сборка плеера WebGL и проверка кадра. Сцены собирает LoomfallModels.
    /// </summary>
    public static class LoomfallCore
    {
        public const string ScenePath = "Assets/Scenes/LowerYardModels.unity";
        public const string BuildFolder = "C:/Unity/build/webgl";
        public const string MaterialsFolder = "Assets/Art/Materials";
        public const string ProfilesFolder = "Assets/Art/Profiles";

        /// <summary>Точка входа: настройки проекта и сборка плеера WebGL.</summary>
        public static void SetupAndBuild()
        {
            ConfigureProject();
            BuildWebGL(new[] { ScenePath });
        }

        /// <summary>Точка входа для пакетного режима: только сборка готовой сцены.</summary>
        public static void BuildOnly()
        {
            ConfigureProject();
            BuildWebGL(new[] { ScenePath });
        }

        /// <summary>Настройки проекта: качество, звук, плеер, параметры WebGL.</summary>
        public static void ConfigureProject()
        {
            EnsureFolder("Assets/Scenes");

            PlayerSettings.companyName = "Loomfall";
            PlayerSettings.productName = "Ткач Ярусов";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.WebGL, ApiCompatibilityLevel.NET_Standard);

            // Мобильный веб — основная площадка, поэтому качество и объём
            // подбираем так, чтобы сборка осталась лёгкой.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Medium);
            PlayerSettings.stripEngineCode = true;

            // PlayerSettings.WebGL — статический класс, поэтому обращаемся к типу.
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.initialMemorySize = 512;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.HighPerformance;

            // Перечисления теней у URP свои, поэтому уточняем пространство имён.
            QualitySettings.shadows = UnityEngine.ShadowQuality.All;
            QualitySettings.shadowResolution = UnityEngine.ShadowResolution.High;
            QualitySettings.shadowDistance = 60f;
            QualitySettings.antiAliasing = 0; // сглаживание делает URP (SMAA)
            QualitySettings.vSyncCount = 0;

            AssetDatabase.SaveAssets();
            Debug.Log("[Loomfall] Настройки проекта применены");
        }

        /// <summary>Свет подземного яруса: холодный верхний свет, тёплые лампы, туман.</summary>
        public static void BuildLighting()
        {
            // Свет, пробивающийся сверху: рисует силуэты и даёт шахту в тумане.
            var shaftObject = new GameObject("СветСверху");
            var shaft = shaftObject.AddComponent<Light>();
            shaft.type = LightType.Directional;
            shaft.color = new Color(0.62f, 0.74f, 0.95f);
            shaft.intensity = 1.5f;
            shaft.shadows = LightShadows.Soft;
            shaft.shadowStrength = 0.85f;
            shaftObject.transform.rotation = Quaternion.Euler(58f, -35f, 0f);

            // Тёплая подсветка снизу-спереди: вытаскивает силуэты из темноты.
            var fillObject = new GameObject("ТёплаяПодсветка");
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.72f, 0.5f, 0.3f);
            fill.intensity = 0.28f;
            fill.shadows = LightShadows.None;
            fillObject.transform.rotation = Quaternion.Euler(-12f, 150f, 0f);

            // Туман — главный инструмент глубины в подземном ярусе.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.075f, 0.085f, 0.115f);
            RenderSettings.fogDensity = 0.02f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.13f, 0.15f, 0.19f);
            RenderSettings.ambientEquatorColor = new Color(0.07f, 0.075f, 0.09f);
            RenderSettings.ambientGroundColor = new Color(0.025f, 0.025f, 0.03f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        }

        /// <summary>Постобработка: тонмаппинг, свечение, грейд, виньетка и зерно.</summary>
        public static void BuildVolume()
        {
            EnsureFolder("Assets/Art");
            EnsureFolder(ProfilesFolder);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            string profilePath = $"{ProfilesFolder}/LoomfallProfile.asset";
            AssetDatabase.CreateAsset(profile, profilePath);

            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(1.05f);
            bloom.scatter.Override(0.72f);
            bloom.tint.Override(new Color(1f, 0.86f, 0.68f));

            var grade = profile.Add<ColorAdjustments>(true);
            grade.postExposure.Override(0.45f);
            grade.contrast.Override(14f);
            grade.saturation.Override(-12f);
            grade.colorFilter.Override(new Color(1f, 0.96f, 0.9f));

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.38f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(new Color(0.02f, 0.02f, 0.03f));

            var grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Medium1);
            grain.intensity.Override(0.28f);
            grain.response.Override(0.7f);

            AssetDatabase.SaveAssets();

            var volumeObject = new GameObject("Постобработка");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;
        }

        /// <summary>Собирает WebGL-плеер; в пакетном режиме завершает работу с кодом ошибки.</summary>
        public static void BuildWebGL(string[] scenes)
        {
            var list = new List<EditorBuildSettingsScene>();
            foreach (string scene in scenes)
            {
                list.Add(new EditorBuildSettingsScene(scene, true));
            }

            EditorBuildSettings.scenes = list.ToArray();
            Directory.CreateDirectory(BuildFolder);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = BuildFolder,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"[Loomfall] Сборка: {summary.result}, размер {summary.totalSize / (1024f * 1024f):F1} МБ, ошибок {summary.totalErrors}");

            if (summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Кадр текущей сцены в PNG вместе с постобработкой и без неё.</summary>
        public static void RenderPreview()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                Debug.LogError("[Loomfall] В сцене нет камеры");
                EditorApplication.Exit(1);
                return;
            }

            var volume = UnityEngine.Object.FindFirstObjectByType<Volume>();
            FrameStats withPost = RenderShot(camera, "C:/Unity/shots/preview.png", 1600, 900);
            Debug.Log($"[Loomfall] Кадр с постобработкой: {withPost}");

            if (volume != null)
            {
                volume.enabled = false;
                FrameStats without = RenderShot(camera, "C:/Unity/shots/preview-nopost.png", 1600, 900);
                volume.enabled = true;
                Debug.Log($"[Loomfall] Кадр без постобработки: {without}");
            }
        }

        /// <summary>Один кадр: рендер в текстуру, сохранение PNG и замер.</summary>
        public static FrameStats RenderShot(Camera camera, string path, int width, int height)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            camera.targetTexture = target;
            camera.Render();

            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;

            byte[] png = texture.EncodeToPNG();
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            File.WriteAllBytes(path, png);

            FrameStats stats = FrameStats.Measure(texture.GetPixels32());
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(target);
            return stats;
        }

        /// <summary>Простые метрики кадра: по ним видно, живой он или пустой.</summary>
        public readonly struct FrameStats
        {
            public readonly double Mean;
            public readonly double Deviation;
            public readonly double BrightPercent;
            public readonly int Colors;

            private FrameStats(double mean, double deviation, double brightPercent, int colors)
            {
                Mean = mean;
                Deviation = deviation;
                BrightPercent = brightPercent;
                Colors = colors;
            }

            public static FrameStats Measure(Color32[] pixels)
            {
                double sum = 0;
                double sumSquares = 0;
                int bright = 0;
                var colors = new HashSet<int>();
                foreach (Color32 pixel in pixels)
                {
                    double luma = 0.2126 * pixel.r + 0.7152 * pixel.g + 0.0722 * pixel.b;
                    sum += luma;
                    sumSquares += luma * luma;
                    if (luma > 12) bright++;
                    colors.Add((pixel.r >> 3) * 4096 + (pixel.g >> 3) * 64 + (pixel.b >> 3));
                }

                double mean = sum / pixels.Length;
                double deviation = Math.Sqrt(Math.Max(0, sumSquares / pixels.Length - mean * mean));
                return new FrameStats(mean, deviation, 100.0 * bright / pixels.Length, colors.Count);
            }

            public override string ToString() =>
                $"яркость {Mean:F1}, разброс {Deviation:F1}, светлых {BrightPercent:F1}%, оттенков {Colors}";
        }

        /// <summary>Материал URP с текстурой; нужен для моделей кита и персонажа.</summary>
        public static Material MakeTextureMaterial(string name, string texturePath)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                Debug.LogWarning($"[Loomfall] Текстура не найдена: {texturePath}");
            }

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetTexture("_BaseMap", texture);
            material.SetFloat("_Smoothness", 0.35f);

            EnsureFolder(MaterialsFolder);
            string path = $"{MaterialsFolder}/{name}.mat";
            AssetDatabase.CreateAsset(material, path);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        /// <summary>Создаёт папку в проекте вместе с родительскими.</summary>
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
            string leaf = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}