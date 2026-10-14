using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Loomfall.EditorTools
{
    /// <summary>Проверка механики Забвения: кадр с постобработкой и без неё при росте числа.</summary>
    public static class LoomfallOblivion
    {
        private const string ScenePath = "Assets/Scenes/LowerYardModels.unity";

        /// <summary>Рендерит двор при Забвении 0, 0,5 и 1 и печатает замеры обоих вариантов.</summary>
        public static void Compare()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var clock = Object.FindFirstObjectByType<Loomfall.OblivionClock>();
            Camera camera = Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                Debug.LogError("[Loomfall] На сцене нет камеры");
                EditorApplication.Exit(1);
                return;
            }

            Light[] lights = clock != null && clock.lights != null ? clock.lights : new Light[0];
            float[] basis = clock != null ? clock.baseIntensities : null;
            if (lights.Length == 0)
            {
                var found = new List<Light>();
                foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (light.type == LightType.Directional) found.Add(light);
                }

                lights = found.ToArray();
                basis = new float[lights.Length];
                for (int i = 0; i < lights.Length; i++) basis[i] = lights[i].intensity;
            }

            float baseFog = clock != null ? clock.baseFogDensity : 0.02f;
            float maxFog = clock != null ? clock.maxFogDensity : 0.07f;
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();

            float[] steps = { 0f, 0.5f, 1f };
            foreach (float value in steps)
            {
                Loomfall.OblivionClock.ApplyLook(
                    value,
                    lights,
                    basis,
                    camera,
                    clock != null ? clock.volume : null,
                    baseFog,
                    maxFog,
                    clock != null ? clock.baseExposure : 0.45f,
                    clock != null ? clock.darkExposure : -1.3f);

                float sum = 0f;
                foreach (Light light in lights) sum += light.intensity;

                data.renderPostProcessing = true;
                string path = "C:/Unity/shots/oblivion-" + Mathf.RoundToInt(value * 100f) + ".png";
                LoomfallCore.FrameStats withPost = LoomfallCore.RenderShot(camera, path, 1280, 720);

                data.renderPostProcessing = false;
                string flatPath = "C:/Unity/shots/oblivion-nopost-" + Mathf.RoundToInt(value * 100f) + ".png";
                LoomfallCore.FrameStats withoutPost = LoomfallCore.RenderShot(camera, flatPath, 1280, 720);
                data.renderPostProcessing = true;

                Debug.Log(
                    "[Loomfall] Забвение " + value.ToString("F1") +
                    ": сумма света " + sum.ToString("F2") +
                    ", туман " + RenderSettings.fogDensity.ToString("F3") +
                    " | с постобработкой: " + withPost +
                    " | без: " + withoutPost);
            }

            // Решающая проверка: свет 0, окружение 0, туман выключен.
            foreach (Light light in lights) light.intensity = 0f;
            RenderSettings.ambientSkyColor = Color.black;
            RenderSettings.ambientEquatorColor = Color.black;
            RenderSettings.ambientGroundColor = Color.black;
            RenderSettings.fog = false;
            data.renderPostProcessing = false;

            LoomfallCore.FrameStats black = LoomfallCore.RenderShot(camera, "C:/Unity/shots/black-test.png", 640, 360);
            Debug.Log("[Loomfall] Чёрный кадр (свет 0, окружение 0, без тумана): " + black);

            int shown = 0;
            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (shown >= 3) break;
                shown++;
                string shader = renderer.sharedMaterial != null ? renderer.sharedMaterial.shader.name : "нет материала";
                Debug.Log("[Loomfall] Поверхность: " + renderer.name + " -> " + shader);
            }
        }
    }
}